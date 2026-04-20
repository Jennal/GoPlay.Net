#define DEBUG
#define PROFILER

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTest.Helpers;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Debug;
using GoPlay.Core.Encodes;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.Http;
using GoPlay.Core.Transport.Wss;

namespace UnitTest
{
    public class TestWssServer
    {
        private Server<WssServer> _server;
        private Client<WssClient> _client;
        private int _port;
        
        [SetUp]
        public async Task Setup()
        {
            Profiler.Clear();
            _port = TestPort.GetFree();
            
            _server = new Server<WssServer>();
            _server.Register(new TestProcessor());
            _server.Start("127.0.0.1", _port);
            
            _client = new Client<WssClient>();
            _client.RequestTimeout = TimeSpan.MaxValue;
            _client.OnError += Console.WriteLine;
            if (!await _client.Connect("127.0.0.1", _port, TimeSpan.MaxValue))
            {
                throw new Exception("connect failed!");
            }
        }

        [TearDown]
        public async Task TearDown()
        {
            try { if (_client != null) await _client.DisconnectAsync(); } catch { /* ignore */ }
            try { _server?.Stop(); } catch { /* ignore */ }
        }

        [Test]
        public async Task TestClientConnectError()
        {
            var insideOnError = false;
            var client = new Client<WssClient>();
            client.OnError += err =>
            {
                insideOnError = true;
            };
            var result = await client.Connect("localhost", 9999);
            Assert.AreEqual(false, result);
            Assert.AreEqual(true, insideOnError);
        }
        
        [Test]
        public async Task TestRequest()
        {
            var (status, result) = await _client.Request<PbString, PbString>("test.err", new PbString
            {
                Value = "hello"
            });
            Assert.AreEqual(StatusCode.Success, status.Code);
            Assert.AreEqual("Server reply: hello", result.Value);
            
            (status, result) = await _client.Request<PbString, PbString>("test.err", new PbString
            {
                Value = "hello1"
            });
            Assert.AreEqual(status.Code, StatusCode.Error);
            Assert.AreEqual(status.Message, "SYSTEM_ERR");
            Assert.AreEqual(null, result);
            
            (status, result) = await _client.Request<PbString, PbString>("test.err", new PbString
            {
                Value = "hello2"
            });
            Assert.AreEqual(StatusCode.Success, status.Code);
            Assert.AreEqual("Server reply: hello2", result.Value);
        }
        
        [Test]
        public async Task BenchmarkRequest()
        {
            var count = 1000;
            var timer = new System.Diagnostics.Stopwatch();
            
            var client = new Client<WssClient>();
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
        public async Task BenchmarkMultiClientRequest()
        {
            var clientCount = 100;
            var requestCount = 100;

            var port = TestPort.GetFree();
            var server = new Server<WssServer>();
            try
            {
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);

                var failedCount = 0;
                var tasks = new List<Task>();
                for (int i = 0; i < clientCount; i++)
                {
                    var clientId = i;
                    var profilerKey = $"Request_{clientId}";
                    await Task.Delay(1);
                    var t = Task.Run(async () =>
                    {
                        var client = new Client<WssClient>();
                        client.RequestTimeout = TimeSpan.MaxValue;
                        client.OnError += err =>
                        {
                            Console.WriteLine($"Client[{clientId}] Error: {err}");
                        };
                        var ok = await client.Connect("127.0.0.1", port);
                        if (!ok)
                        {
                            failedCount++;
                            Console.WriteLine($"Connect[{clientId}] Failed...");
                            return;
                        }

                        try
                        {
                            for (var j = 0; j < requestCount; j++)
                            {
                                if (client.Status != Client.ClientStatus.Connected)
                                {
                                    Console.WriteLine($"Client[{clientId}][{j}] is not connected!");
                                    break;
                                }

                                var id = clientId * j;
                                Profiler.Begin(profilerKey);
                                var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString
                                {
                                    Value = $"Hello_{id}"
                                });
                                Profiler.End(profilerKey);

                                Assert.AreEqual(status.Code, StatusCode.Success);
                                Assert.AreEqual(result.Value, $"[Test] Server reply: Hello_{id}");
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
                Console.WriteLine($"Failed Count: {failedCount}");
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
            var server = new Server<WssServer>();
            Client<WssClient> client = null;
            try
            {
                server.Register(new TestProcessor());
                server.OnError += (clientId, err) =>
                {
                    Console.WriteLine($"Server.OnError: {err}");
                };
                server.Start("127.0.0.1", port);

                client = new Client<WssClient>();
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
    }
}
