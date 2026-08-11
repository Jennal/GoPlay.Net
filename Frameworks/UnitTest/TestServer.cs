#define DEBUG
#define PROFILER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        /// <summary>
        /// 多连接同时发送超大包：NcServer 在不同 IOCP 线程上并发进入 <c>ResolveChunk</c>。
        /// 旧实现用全局共享 Dictionary 会在跨连接并发写时损坏或串包；
        /// 现挂在 per-session <c>ClientLifetime.ChunkCache</c> 上应稳定，且各连接回包不得串扰。
        /// </summary>
        [Test]
        public async Task TestConcurrentOverflowPackage()
        {
            const int clientCount = 24;
            const int roundsPerClient = 3;

            var port = TestPort.GetFree();
            var server = new Server<NcServer>();
            var serverErrors = new ConcurrentBag<(uint ClientId, Exception Error)>();
            var failures = new ConcurrentBag<string>();
            server.OnError += (u, exception) =>
            {
                serverErrors.Add((u, exception));
                Console.WriteLine($"Server.OnError[{u}]: {exception}");
            };

            static string MakePayload(int clientIndex)
            {
                // 独特前缀：一旦跨 session 串包，断言立刻暴露。
                var marker = $"C{clientIndex:D4}|";
                var sb = new StringBuilder(marker, (int)Consts.Package.MAX_CHUNK_SIZE + 256);
                var pad = (char)('A' + (clientIndex % 26));
                while (sb.Length < Consts.Package.MAX_CHUNK_SIZE + 100)
                {
                    sb.Append(pad);
                }
                return sb.ToString();
            }

            try
            {
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);

                var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                var connected = 0;
                var tasks = new List<Task>(clientCount);

                for (var i = 0; i < clientCount; i++)
                {
                    var clientIndex = i;
                    tasks.Add(Task.Run(async () =>
                    {
                        var client = new Client<NcClient>();
                        client.RequestTimeout = TimeSpan.FromSeconds(30);
                        client.OnError += exception =>
                        {
                            failures.Add($"client[{clientIndex}] OnError: {exception}");
                        };

                        try
                        {
                            Assert.IsTrue(await client.Connect("127.0.0.1", port),
                                $"client[{clientIndex}] connect failed");

                            if (Interlocked.Increment(ref connected) == clientCount)
                            {
                                startGate.TrySetResult(true);
                            }

                            // 对齐起点，尽量让各连接的分包重组重叠在同一时间窗。
                            await startGate.Task;

                            var payload = MakePayload(clientIndex);
                            var expected = $"[Test] Server reply: {payload}";
                            for (var round = 0; round < roundsPerClient; round++)
                            {
                                var (status, result) = await client.Request<PbString, PbString>(
                                    "test.echo", new PbString { Value = payload });

                                if (status.Code != StatusCode.Success)
                                {
                                    failures.Add(
                                        $"client[{clientIndex}] round={round}: status={status.Code}, msg={status.Message}");
                                    continue;
                                }

                                if (result?.Value != expected)
                                {
                                    var preview = result?.Value == null
                                        ? "<null>"
                                        : result.Value.Length <= 64
                                            ? result.Value
                                            : result.Value.Substring(0, 64) + "...";
                                    failures.Add(
                                        $"client[{clientIndex}] round={round}: reply mismatch, got={preview}");
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            failures.Add($"client[{clientIndex}] exception: {err}");
                        }
                        finally
                        {
                            try { await client.DisconnectAsync(); } catch { /* ignore */ }
                        }
                    }));
                }

                // 防止个别连接卡死导致 startGate 永不放行。
                var allStarted = await Task.WhenAny(startGate.Task, Task.Delay(TimeSpan.FromSeconds(10)));
                if (allStarted != startGate.Task)
                {
                    startGate.TrySetResult(true);
                    Assert.Fail($"only {connected}/{clientCount} clients connected before start gate timeout");
                }

                await Task.WhenAll(tasks);

                Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
                Assert.IsEmpty(serverErrors,
                    string.Join(Environment.NewLine, serverErrors.Select(e => $"[{e.ClientId}] {e.Error}")));
            }
            finally
            {
                server.Stop();
            }
        }
    }
}
