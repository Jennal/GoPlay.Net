#define DEBUG
#define PROFILER

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTest.Benchmarks;
using UnitTest.Helpers;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Debug;
using GoPlay.Core.Encodes;
using GoPlay.Core.Protocols;
using GoPlay.Core.Routers;
using GoPlay.Core.Transport.NetCoreServer;
using GoPlay.Core.Transports.TCP;

namespace UnitTest
{
    public class BenchmarkServer
    {
        private Server<TcpServer> _server;
        private int _port;

        [SetUp]
        public void Setup()
        {
            Profiler.Clear();
            _port = TestPort.GetFree();

            _server = new Server<TcpServer>();
            _server.OnError += (clientId, err) =>
            {
                Console.WriteLine($"SERVER_ERROR({clientId}): {err.Message}\n{err.StackTrace}");
            };

            _server.Register(new TestProcessor());
            _server.Start("127.0.0.1", _port);
        }

        [TearDown]
        public void TearDown()
        {
            try { _server?.Stop(); } catch { /* ignore */ }
        }

        [Test]
        public async Task BenchmarkRouter()
        {
            var count = 1000 * 10000;
            var timer = new System.Diagnostics.Stopwatch();
            var route = new Route(new TestProcessor(), typeof(TestProcessor).GetMethod("Echo")!, 0);
            var pack = new Package<PbString>
            {
                Header = new Header
                {
                    PackageInfo = new PackageInfo
                    {
                        EncodingType = EncodingType.Protobuf,
                    }
                },
                RawData = ProtobufEncoder.Instance.Encode(new PbString
                {
                    Value = "Hello"
                }),
            };
            timer.Start();
            for (var i = 0; i < count; i++)
            {
                var result = (await route.Invoke(pack)) as Package<PbString>;
                // Assert.AreEqual("Hello", result.Data.Value);
            }

            timer.Stop();

            var total = timer.ElapsedMilliseconds;
            var avg = (float)total / count;
            Console.WriteLine($"Total millisec: {total}");
            Console.WriteLine($"Average millisec: {avg}");
            Console.WriteLine();
            Console.WriteLine(Profiler.Statistics());
        }

        [Test]
        public async Task BenchmarkRequest()
        {
            var count = 10000 * 10;
            var timer = new System.Diagnostics.Stopwatch();

            var client = new Client<TcpClient>();
            try
            {
                await client.Connect("127.0.0.1", _port);
                client.RequestTimeout = TimeSpan.MaxValue;
                client.OnError += err => Console.WriteLine($"ERROR: {err.Message}\n{err.StackTrace}");

                timer.Start();
                for (var i = 0; i < count; i++)
                {
                    var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString
                    {
                        Value = $"Hello_{i}"
                    });

                    Console.WriteLine($"{i}, {status}, {result}");

                    Assert.AreEqual("", status.Message);
                    Assert.AreEqual(StatusCode.Success, status.Code);
                    Assert.AreEqual($"[Test] Server reply: Hello_{i}", result.Value);
                }

                timer.Stop();

                var total = timer.ElapsedMilliseconds;
                var avg = (float)total / count;
                Console.WriteLine($"Total millisec: {total}");
                Console.WriteLine($"Average millisec: {avg}");
                Console.WriteLine(Profiler.Statistics());
            }
            finally
            {
                try { await client.DisconnectAsync(); } catch { /* ignore */ }
            }
        }

        [Test]
        public async Task TestTcpMultiClientRequest()
        {
            var clientCount = 100;
            var requestCount = 100;

            var port = TestPort.GetFree();
            var server = new Server<TcpServer>();
            try
            {
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);

                var tasks = new List<Task>();
                for (int i = 0; i < clientCount; i++)
                {
                    var clientId = i;
                    var profilerKey = $"Request_{clientId}";
                    var t = Task.Run(async () =>
                    {
                        var client = new Client<TcpClient>();
                        client.RequestTimeout = TimeSpan.MaxValue;
                        await client.Connect("127.0.0.1", port);

                        try
                        {
                            for (var j = 0; j < requestCount; j++)
                            {
                                var id = clientId * j;
                                Profiler.Begin(profilerKey);
                                var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString
                                {
                                    Value = $"Hello_{id}"
                                });
                                Profiler.End(profilerKey);

                                Assert.AreEqual(StatusCode.Success, status.Code);
                                Assert.AreEqual($"[Test] Server reply: Hello_{id}", result.Value);
                            }
                        }
                        finally
                        {
                            try { await client.DisconnectAsync(); } catch { /* ignore */ }
                        }
                    });

                    tasks.Add(t);
                }

                await Task.WhenAll(tasks.ToArray());
                Console.WriteLine(Profiler.StatisPrefix("Request"));
            }
            finally
            {
                server.Stop();
            }
        }
        
        [Test]
        public async Task TestNetCoreMultiClientRequest()
        {
            var clientCount = 100;
            var requestCount = 1000;

            var port = TestPort.GetFree();
            var server = new Server<NcServer>();
            try
            {
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);
                await Task.Yield();

                Profiler.Begin("Total");
                var tasks = new List<Task>();
                for (int i = 0; i < clientCount; i++)
                {
                    var clientId = i;
                    var profilerKey = $"Request_{clientId}";
                    var t = Task.Run(async () =>
                    {
                        var client = new Client<NcClient>();
                        client.RequestTimeout = TimeSpan.MaxValue;
                        var ok = await client.Connect("127.0.0.1", port, TimeSpan.FromSeconds(30));
                        if (!ok) throw new Exception("Connection Failed");

                        try
                        {
                            for (var j = 0; j < requestCount; j++)
                            {
                                var id = clientId * j;
                                Profiler.Begin(profilerKey);
                                var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString
                                {
                                    Value = $"Hello_{id}"
                                });
                                Profiler.End(profilerKey);

                                Assert.AreEqual(StatusCode.Success, status.Code);
                                Assert.AreEqual($"[Test] Server reply: Hello_{id}", result.Value);
                            }
                        }
                        finally
                        {
                            try { await client.DisconnectAsync(); } catch { /* ignore */ }
                        }
                    });

                    tasks.Add(t);
                }

                await Task.WhenAll(tasks.ToArray());
                Profiler.End("Total");
                Console.WriteLine(Profiler.StatisPrefix("Request"));
                Console.WriteLine(Profiler.Statistics("Total"));
            }
            finally
            {
                server.Stop();
            }
        }

        [Test]
        public async Task TestAddListenerOnce()
        {
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>();
            Client<TcpClient> client = null;
            try
            {
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);

                client = new Client<TcpClient>();
                await client.Connect("127.0.0.1", port);

                var once = 0;
                var twice = 0;

                client.AddListenerOnce<PbString>("test.push", val =>
                {
                    once++;
                    Console.WriteLine($"ONCE: {val.Value}");
                });

                client.AddListener<PbString>("test.push", val =>
                {
                    twice++;
                    Console.WriteLine($"ALL: {val.Value}");
                });

                client.Notify("test.notify", new PbString
                {
                    Value = "hello"
                });

                await Task.Delay(TimeSpan.FromSeconds(1));

                Assert.AreEqual(1, once);
                Assert.AreEqual(2, twice);
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }

        [Test]
        public void TestProfilerStatus()
        {
            var status = new ProfileStatus
            {
                Key = "Test",
                RunCount = 100000000,
                TotalTime = TimeSpan.FromSeconds(22.123)
            };
            Console.WriteLine(status.ToString());
        }

        [Test]
        public void TestProcessorBase()
        {
            var test = new TestProcessor();
            // Console.WriteLine(test.GetRouteDict().Dump());
        }

        [Test]
        public void BenchmarkEcho()
        {
            RequestBenchmark.Run();
        }
    }
}
