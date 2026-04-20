#define DEBUG
#define PROFILER

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTest.Helpers;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core;
using GoPlay.Core.Debug;
using GoPlay.Core.Encodes;
using GoPlay.Core.Protocols;
using GoPlay.Core.Routers;
using GoPlay.Core.Transport.NetCoreServer;
using GoPlay.Core.Transports.TCP;

namespace UnitTest
{
    public class TestServer
    {
        [SetUp]
        public void Setup()
        {
            Profiler.Clear();
        }

        [Test]
        public async Task TestClientConnectError()
        {
            var insideOnError = false;
            var client = new Client<TcpClient>();
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
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>();
            Client<TcpClient> client = null;
            try
            {
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);

                client = new Client<TcpClient>();
                await client.Connect("127.0.0.1", port);

                var (status, result) = await client.Request<PbString, PbString>("test.err", new PbString
                {
                    Value = "hello"
                });
                Assert.AreEqual(StatusCode.Success, status.Code);
                Assert.AreEqual("Server reply: hello", result.Value);

                (status, result) = await client.Request<PbString, PbString>("test.err", new PbString
                {
                    Value = "hello1"
                });
                Assert.AreEqual(status.Code, StatusCode.Error);
                Assert.AreEqual(status.Message, "SYSTEM_ERR");
                Assert.AreEqual(null, result);

                (status, result) = await client.Request<PbString, PbString>("test.err", new PbString
                {
                    Value = "hello2"
                });
                Assert.AreEqual(StatusCode.Success, status.Code);
                Assert.AreEqual("Server reply: hello2", result.Value);
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }
        
        [Test]
        public async Task TestMultiClientRequest()
        {
            var clientCount = 10;
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
                    var t = Task.Run(async () =>
                    {
                        var client = new Client<TcpClient>();
                        await client.Connect("127.0.0.1", port);

                        try
                        {
                            for (var j = 0; j < requestCount; j++)
                            {
                                var id = clientId * j;
                                var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString
                                {
                                    Value = $"Hello_{id}"
                                });

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
        public async Task TestOverflowPackage()
        {
            var port = TestPort.GetFree();
            var server = new Server<NcServer>();
            Client<NcClient> client = null;
            server.OnError += (u, exception) =>
            {
                Console.WriteLine($"Server.OnError[{u}]: {exception}");
            };
            try
            {
                var sb = new StringBuilder();
                for (int i = 0; i < Consts.Package.MAX_CHUNK_SIZE + 100; i++)
                {
                    sb.Append((byte)(i % byte.MaxValue));
                }

                var str = sb.ToString();
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);

                client = new Client<NcClient>();
                client.OnError += (exception) =>
                {
                    Console.WriteLine($"Client.OnError: {exception}");
                };
                client.RequestTimeout = TimeSpan.MaxValue;
                await client.Connect("127.0.0.1", port);

                var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString
                {
                    Value = str
                });
                Console.WriteLine($"{status.Code}, {status.Message}");
                Assert.AreEqual(StatusCode.Success, status.Code);
                Assert.AreEqual($"[Test] Server reply: {str}", result.Value);
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }
    }
}
