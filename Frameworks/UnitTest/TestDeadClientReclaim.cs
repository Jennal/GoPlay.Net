#define DEBUG

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTest.Helpers;
using UnitTest.Processors;
using GoPlay;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.Ws;
using GoPlay.Core.Transport.Wss;
using GoPlay.Core.Transport.NetCoreServer;

namespace UnitTest
{
    /// <summary>
    /// 验证"快速回收已断开客户端"四个优先级：
    /// - 存活闸门 IsClientAlive 生命周期
    /// - P0 发送路径不复活僵尸 sender
    /// - P1 死客户端排队包在出队点被丢弃
    /// - P2 每客户端取消令牌断开即取消
    /// - P3 三传输层 TCP KeepAlive 默认值 + 开启后连断收发回归
    /// </summary>
    public class TestDeadClientReclaim
    {
        // ---- 存活闸门生命周期 ----

        [Test]
        public async Task TestIsClientAliveLifecycle()
        {
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            Client<WsClient> client = null;
            try
            {
                server.Register(new TestProcessor());

                uint connectedId = 0;
                var connected = NewTcs();
                var disconnected = NewTcs();
                server.OnClientConnected += id => { connectedId = id; connected.TrySetResult(true); };
                server.OnClientDisconnected += id => { if (id == connectedId) disconnected.TrySetResult(true); };

                server.Start("127.0.0.1", port);

                client = new Client<WsClient>();
                client.RequestTimeout = TimeSpan.MaxValue;
                Assert.IsTrue(await client.Connect("127.0.0.1", port));

                await WaitAsync(connected.Task, 5000);
                Assert.IsTrue(server.IsClientAlive(connectedId), "connected client should be alive");

                await client.DisconnectAsync();
                await WaitAsync(disconnected.Task, 5000);

                // OnDisconnected 事件触发后 transport 才移除 session，给一点点时间。
                await WaitUntil(() => !server.IsClientAlive(connectedId), 3000);
                Assert.IsFalse(server.IsClientAlive(connectedId), "disconnected client should be dead");
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }

        // ---- P0：发送路径不复活 sender ----

        [Test]
        public async Task TestNoZombieSenderAfterDisconnect()
        {
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            Client<WsClient> client = null;
            try
            {
                server.Register(new TestProcessor());

                uint connectedId = 0;
                var connected = NewTcs();
                var disconnected = NewTcs();
                server.OnClientConnected += id => { connectedId = id; connected.TrySetResult(true); };
                server.OnClientDisconnected += id => { if (id == connectedId) disconnected.TrySetResult(true); };

                server.Start("127.0.0.1", port);

                client = new Client<WsClient>();
                client.RequestTimeout = TimeSpan.MaxValue;
                Assert.IsTrue(await client.Connect("127.0.0.1", port));
                await WaitAsync(connected.Task, 5000);

                await WaitUntil(() => server.SenderCount >= 1, 3000);
                Assert.GreaterOrEqual(server.SenderCount, 1, "connected client should own a sender");

                await client.DisconnectAsync();
                await WaitAsync(disconnected.Task, 5000);
                await WaitUntil(() => server.SenderCount == 0, 5000);
                Assert.AreEqual(0, server.SenderCount, "sender should be reclaimed after disconnect");

                // 关键断言：给已断开客户端发包，绝不复活 sender。
                var ghost = Package.Create(0, new PbString { Value = "ghost" }, PackageType.Response, server.EncodingType);
                ghost.Header.ClientId = connectedId;
                server.Send(ghost);

                await Task.Delay(300);
                Assert.AreEqual(0, server.SenderCount, "Send to a dead client must not resurrect a sender");
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }

        // ---- P1：死客户端排队包被丢弃 ----

        [Test]
        public async Task TestDeadClientQueuedPacketsDropped()
        {
            GateProcessor.Reset();
            var port = TestPort.GetFree();
            var server = new Server<WsServer>(); // 默认并发=1，保证后续请求排队
            Client<WsClient> client = null;
            try
            {
                server.Register(new GateProcessor());

                uint connectedId = 0;
                var connected = NewTcs();
                var disconnected = NewTcs();
                server.OnClientConnected += id => { connectedId = id; connected.TrySetResult(true); };
                server.OnClientDisconnected += id => { if (id == connectedId) disconnected.TrySetResult(true); };

                server.Start("127.0.0.1", port);

                client = new Client<WsClient>();
                client.RequestTimeout = TimeSpan.MaxValue;
                Assert.IsTrue(await client.Connect("127.0.0.1", port));
                await WaitAsync(connected.Task, 5000);

                // req1：进入 handler 并卡在门闩，占住 concurrency=1 的 runner。
                Ignore(client.Request<PbString, PbString>("gate.block", new PbString { Value = "1" }));
                await WaitAsync(GateProcessor.Started.Task, 5000);
                Assert.AreEqual(1, GateProcessor.ExecCount);

                // req2/req3：排进 gate 处理器邮箱（runner 正卡在 req1）。
                Ignore(client.Request<PbString, PbString>("gate.block", new PbString { Value = "2" }));
                Ignore(client.Request<PbString, PbString>("gate.block", new PbString { Value = "3" }));
                await WaitUntil(() => GetQueueCount(server, "gate") >= 2, 5000);
                Assert.GreaterOrEqual(GetQueueCount(server, "gate"), 2, "two packets should be queued");

                // 断开客户端，等到存活闸门翻成 false。
                await client.DisconnectAsync();
                await WaitAsync(disconnected.Task, 5000);
                await WaitUntil(() => !server.IsClientAlive(connectedId), 3000);

                // 释放门闩：req1 跑完（回包被丢），runner 继续 drain req2/req3（应在出队点被判活丢弃）。
                GateProcessor.Release();
                await Task.Delay(600);

                Assert.AreEqual(1, GateProcessor.ExecCount,
                    "queued packets of a disconnected client must be dropped at dequeue, not executed");
            }
            finally
            {
                GateProcessor.Release();
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }

        // ---- P2：每客户端取消令牌断开即取消 ----

        [Test]
        public async Task TestPerClientTokenCancelledOnDisconnect()
        {
            GateProcessor.Reset();
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            Client<WsClient> client = null;
            try
            {
                server.Register(new GateProcessor());

                uint connectedId = 0;
                var connected = NewTcs();
                var disconnected = NewTcs();
                server.OnClientConnected += id => { connectedId = id; connected.TrySetResult(true); };
                server.OnClientDisconnected += id => { if (id == connectedId) disconnected.TrySetResult(true); };

                server.Start("127.0.0.1", port);

                client = new Client<WsClient>();
                client.RequestTimeout = TimeSpan.MaxValue;
                Assert.IsTrue(await client.Connect("127.0.0.1", port));
                await WaitAsync(connected.Task, 5000);

                Ignore(client.Request<PbString, PbString>("gate.block", new PbString { Value = "x" }));
                await WaitAsync(GateProcessor.Started.Task, 5000);

                var token = GateProcessor.CapturedToken;
                Assert.IsTrue(token.CanBeCanceled, "ambient token should be a real cancellable token");
                Assert.IsFalse(token.IsCancellationRequested, "token should not be cancelled while connected");

                await client.DisconnectAsync();
                await WaitAsync(disconnected.Task, 5000);

                await WaitUntil(() => token.IsCancellationRequested, 5000);
                Assert.IsTrue(token.IsCancellationRequested, "per-client token must be cancelled on disconnect");
            }
            finally
            {
                GateProcessor.Release();
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }

        // ---- P3：三传输层 KeepAlive 默认值 ----

        [Test]
        public void TestKeepAliveDefaults()
        {
            var ws = new WsServer();
            Assert.IsTrue(ws.KeepAliveEnabled);
            Assert.AreEqual(5, ws.KeepAliveTimeSeconds);
            Assert.AreEqual(2, ws.KeepAliveIntervalSeconds);
            Assert.AreEqual(3, ws.KeepAliveRetryCount);

            var wss = new WssServer();
            Assert.IsTrue(wss.KeepAliveEnabled);
            Assert.AreEqual(5, wss.KeepAliveTimeSeconds);
            Assert.AreEqual(2, wss.KeepAliveIntervalSeconds);
            Assert.AreEqual(3, wss.KeepAliveRetryCount);

            var nc = new NcServer();
            Assert.IsTrue(nc.KeepAliveEnabled);
            Assert.AreEqual(5, nc.KeepAliveTimeSeconds);
            Assert.AreEqual(2, nc.KeepAliveIntervalSeconds);
            Assert.AreEqual(3, nc.KeepAliveRetryCount);
        }

        // ---- P3：开启 KeepAlive 后连断收发回归（免证书的 Ws / Nc）----

        [Test]
        public async Task TestKeepAliveConnectRegressionWs()
        {
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            Client<WsClient> client = null;
            try
            {
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);

                client = new Client<WsClient>();
                client.RequestTimeout = TimeSpan.MaxValue;
                Assert.IsTrue(await client.Connect("127.0.0.1", port));

                var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString { Value = "ka" });
                Assert.AreEqual(StatusCode.Success, status.Code);
                Assert.AreEqual("[Test] Server reply: ka", result.Value);
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }

        [Test]
        public async Task TestKeepAliveConnectRegressionNc()
        {
            var port = TestPort.GetFree();
            var server = new Server<NcServer>();
            Client<NcClient> client = null;
            try
            {
                server.Register(new TestProcessor());
                server.Start("127.0.0.1", port);

                client = new Client<NcClient>();
                client.RequestTimeout = TimeSpan.MaxValue;
                Assert.IsTrue(await client.Connect("127.0.0.1", port));

                var (status, result) = await client.Request<PbString, PbString>("test.echo", new PbString { Value = "ka" });
                Assert.AreEqual(StatusCode.Success, status.Code);
                Assert.AreEqual("[Test] Server reply: ka", result.Value);
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }

        // ---- helpers ----

        private static TaskCompletionSource<bool> NewTcs()
            => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        private static async Task WaitAsync(Task task, int timeoutMs)
        {
            var done = await Task.WhenAny(task, Task.Delay(timeoutMs)).ConfigureAwait(false);
            if (done != task) Assert.Fail("timeout waiting for task");
            await task.ConfigureAwait(false);
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

        private static int GetQueueCount(Server server, string name)
        {
            foreach (var s in server.GetProcessorQueueStatus())
            {
                if (s.Name == name) return s.PackageQueueCount;
            }
            return 0;
        }

        private static void Ignore(Task t) => t.ContinueWith(x => { _ = x.Exception; }, TaskScheduler.Default);
    }
}
