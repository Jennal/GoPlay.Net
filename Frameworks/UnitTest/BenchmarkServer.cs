#define DEBUG
#define PROFILER

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTest.Benchmarks;
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
        private Server<TcpServer> _server = null;
        private Client<TcpClient> _client = null;

        [SetUp]
        public async Task Setup()
        {
            Profiler.Clear();

            if (_server != null) return;

            _server = new Server<TcpServer>();
            _server.OnError += (clientId, err) =>
            {
                Console.WriteLine($"SERVER_ERROR({clientId}): {err.Message}\n{err.StackTrace}");
            };

            _server.Register(new TestProcessor());
            _server.Start("127.0.0.1", 8686);
        }

        [Test]
        public async Task BenchmarkRouter()
        {
            var count = 1000 * 10000;
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
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
            var count = 10000 * 10; //1000 * 10000;
            var timer = new System.Diagnostics.Stopwatch();

            var client = new Client<TcpClient>();
            await client.Connect("127.0.0.1", 8686);
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

            await client.DisconnectAsync();
        }

        [Test]
        public async Task TestTcpMultiClientRequest()
        {
            var clientCount = 100;
            var requestCount = 100;

            var encoder = ProtobufEncoder.Instance;
            var server = new Server<TcpServer>();
            server.Register(new TestProcessor());
            var task = server.Start("127.0.0.1", 5557);

            var tasks = new List<Task>();
            for (int i = 0; i < clientCount; i++)
            {
                var clientId = i;
                var profilerKey = $"Request_{clientId}";
                var t = Task.Run(async () =>
                {
                    var client = new Client<TcpClient>();
                    client.RequestTimeout = TimeSpan.MaxValue;
                    client.Connect("127.0.0.1", 5557).Wait();

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
                });

                tasks.Add(t);
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine(Profiler.StatisPrefix("Request"));
        }
        
        [Test]
        public async Task TestNetCoreMultiClientRequest()
        {
            var clientCount = 100;
            var requestCount = 1000;

            var encoder = ProtobufEncoder.Instance;
            var server = new Server<NcServer>();
            server.Register(new TestProcessor());
            var task = server.Start("127.0.0.1", 5557);
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
                    var ok = await client.Connect("127.0.0.1", 5557);
                    if (!ok) throw new Exception("Connection Failed");

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
                });

                tasks.Add(t);
            }

            Task.WaitAll(tasks.ToArray());
            Profiler.End("Total");
            Console.WriteLine(Profiler.StatisPrefix("Request"));
            Console.WriteLine(Profiler.Statistics("Total"));
        }

        [Test]
        public async Task TestAddListenerOnce()
        {
            var server = new Server<TcpServer>();
            server.Register(new TestProcessor());
            var task = server.Start("127.0.0.1", 5556);

            var client = new Client<TcpClient>();
            await client.Connect("127.0.0.1", 5556);

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