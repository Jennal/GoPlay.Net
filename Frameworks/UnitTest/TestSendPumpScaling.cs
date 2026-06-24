using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTest.Helpers;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.Ws;

namespace UnitTest
{
    /// <summary>
    /// 验证 L3：把"每连接一条 LongRunning 发送线程"换成固定 N 条 worker 的共享 <c>SessionSendPump</c>。
    ///
    /// <para>
    /// 注意：单测里 client 与 server 同进程，而 client 框架仍是 thread-per-client，会淹没线程数差异，
    /// 因此这里**不**断言 OS 线程数（那一面由线上 DIAG 的 osThr 验证）。本文件聚焦共享 pump 下的
    /// **功能正确性**：多连接并发不丢包/不饿死、单连接保序高吞吐、断开不泄漏 sender、Stop/Start 后可复用。
    /// </para>
    /// </summary>
    public class TestSendPumpScaling
    {
        // ---- 多连接并发：共享 pump 必须服务到每一个 client，不丢包、不饿死 ----

        [Test]
        public async Task TestPumpServesManyClientsConcurrently()
        {
            // 并发连接数取一个能体现"多连接共享 pump"的量级即可；线程数那一面由线上 osThr DIAG 验证。
            // RequestTimeout 给足，避免 CI/被压测占满的机器上 handshake 因机器饱和误判超时（与其它用例一致的做法）。
            const int clientCount = 30;
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            var clients = new List<Client<WsClient>>();
            try
            {
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);

                // 并发连接 + 并发各发一条带唯一标识的 echo，全部必须拿到正确回包。
                var tasks = Enumerable.Range(0, clientCount).Select(async i =>
                {
                    var client = new Client<WsClient> { RequestTimeout = TimeSpan.FromMinutes(2) };
                    lock (clients) clients.Add(client);

                    if (!await client.Connect("127.0.0.1", port))
                        throw new Exception($"client {i} connect failed");

                    var (status, result) = await client.Request<PbString, PbString>(
                        "test.echo", new PbString { Value = $"c{i}" });

                    Assert.AreEqual(StatusCode.Success, status.Code, $"client {i} status");
                    Assert.AreEqual($"[Test] Server reply: c{i}", result.Value, $"client {i} payload");
                }).ToArray();

                await Task.WhenAll(tasks);

                await WaitUntil(() => server.SenderCount == clientCount, 5000);
                Assert.AreEqual(clientCount, server.SenderCount, "every connected client should own exactly one sender");

                // 全部断开后，sender 必须回落到 0（pump 模型下也不该泄漏）。
                foreach (var c in clients)
                {
                    try { await c.DisconnectAsync(); } catch { /* ignore */ }
                }
                await WaitUntil(() => server.SenderCount == 0, 8000);
                Assert.AreEqual(0, server.SenderCount, "all senders should be reclaimed after disconnect");
            }
            finally
            {
                foreach (var c in clients)
                {
                    try { await c.DisconnectAsync(); } catch { /* ignore */ }
                }
                server.Stop();
            }
        }

        // ---- 单连接高吞吐：同 client 内严格保序，pump 反复 drain 同一条流不乱序/不丢 ----

        [Test]
        public async Task TestPumpPreservesPerClientOrderHighThroughput()
        {
            const int requestCount = 300;
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            Client<WsClient> client = null;
            try
            {
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);

                client = new Client<WsClient> { RequestTimeout = TimeSpan.FromMinutes(2) };
                Assert.IsTrue(await client.Connect("127.0.0.1", port));

                for (var i = 0; i < requestCount; i++)
                {
                    var (status, result) = await client.Request<PbString, PbString>(
                        "test.echo", new PbString { Value = $"seq_{i}" });

                    Assert.AreEqual(StatusCode.Success, status.Code, $"req {i} status");
                    Assert.AreEqual($"[Test] Server reply: seq_{i}", result.Value, $"req {i} payload (顺序/对应关系必须严格)");
                }
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }

        // ---- Stop 后 pump 关闭、Start 后重建：服务必须照常可用 ----

        [Test]
        public async Task TestPumpWorksAfterRestart()
        {
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            try
            {
                server.Register(new TestProcessor());

                server.Start("127.0.0.1", port);
                await EchoOnce(port, "before-restart");
                server.Stop();

                // 复用同一端口重启：pump 应被重建，发送链路恢复可用。
                server.Start("127.0.0.1", port);
                await EchoOnce(port, "after-restart");
            }
            finally
            {
                server.Stop();
            }
        }

        private static async Task EchoOnce(int port, string value)
        {
            var client = new Client<WsClient> { RequestTimeout = TimeSpan.FromMinutes(2) };
            try
            {
                Assert.IsTrue(await client.Connect("127.0.0.1", port), $"connect failed ({value})");
                var (status, result) = await client.Request<PbString, PbString>(
                    "test.echo", new PbString { Value = value });
                Assert.AreEqual(StatusCode.Success, status.Code);
                Assert.AreEqual($"[Test] Server reply: {value}", result.Value);
            }
            finally
            {
                try { await client.DisconnectAsync(); } catch { /* ignore */ }
            }
        }

        private static async Task WaitUntil(Func<bool> condition, int timeoutMs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (condition()) return;
                await Task.Delay(20).ConfigureAwait(false);
            }
        }
    }
}
