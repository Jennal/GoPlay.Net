using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GoPlay;
using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Transport.Ws;
using NUnit.Framework;
using UnitTest.Helpers;

namespace UnitTest
{
    /// <summary>
    /// 用于追踪 OnClientConnected / OnClientDisconnected / OnBroadcast 回调执行情况的 Processor。
    /// 字段全部用 <see cref="Interlocked"/> / lock 做并发保护，因为这两个回调
    /// 当前在 transport IO 线程上同步触发，多 session 会并发进来。
    /// </summary>
    [Processor("tracker")]
    public class TrackerProcessor : ProcessorBase
    {
        public int ConnectCallCount;
        public int DisconnectCallCount;
        public int BroadcastCallCount;

        // 记录见过的 clientId，检查 Connect / Disconnect 是否 1:1 配对
        public readonly HashSet<uint> ConnectedClientIds = new HashSet<uint>();
        public readonly HashSet<uint> DisconnectedClientIds = new HashSet<uint>();
        private readonly object _lock = new object();

        // 可配置的"阻塞时长"，模拟 OnClientDisconnected 里同步 await Redis 的耗时场景，
        // 让我们能显式验证 Stop 会等到这段逻辑跑完才返回。
        public TimeSpan DisconnectBlockFor { get; set; } = TimeSpan.Zero;

        // 可配置 OnBroadcast 每条消息的执行耗时；用于验证 StopProcessors 的 graceful drain 能等这段工作跑完。
        public TimeSpan BroadcastDelayPer { get; set; } = TimeSpan.Zero;

        public override string[] Pushes => Array.Empty<string>();

        public override void OnClientConnected(uint clientId)
        {
            Interlocked.Increment(ref ConnectCallCount);
            lock (_lock) ConnectedClientIds.Add(clientId);
        }

        public override void OnClientDisconnected(uint clientId)
        {
            if (DisconnectBlockFor > TimeSpan.Zero)
            {
                Thread.Sleep(DisconnectBlockFor);
            }
            Interlocked.Increment(ref DisconnectCallCount);
            lock (_lock) DisconnectedClientIds.Add(clientId);
        }

        public override async Task OnBroadcast(uint clientId, int eventId, object data)
        {
            if (BroadcastDelayPer > TimeSpan.Zero)
            {
                await Task.Delay(BroadcastDelayPer).ConfigureAwait(false);
            }
            Interlocked.Increment(ref BroadcastCallCount);
        }
    }

    /// <summary>
    /// OnClientConnected / OnClientDisconnected 里主动抛异常，
    /// 验证"单个 Processor 抛异常不会阻断后续 Processor 的事件"。
    /// </summary>
    [Processor("throwing")]
    public class ThrowingProcessor : ProcessorBase
    {
        public int ConnectCallCount;
        public int DisconnectCallCount;

        public override string[] Pushes => Array.Empty<string>();

        public override void OnClientConnected(uint clientId)
        {
            Interlocked.Increment(ref ConnectCallCount);
            throw new InvalidOperationException("ThrowingProcessor.OnClientConnected intentional throw");
        }

        public override void OnClientDisconnected(uint clientId)
        {
            Interlocked.Increment(ref DisconnectCallCount);
            throw new InvalidOperationException("ThrowingProcessor.OnClientDisconnected intentional throw");
        }
    }

    /// <summary>
    /// 覆盖最近一次对 Server.Stop() 相关改动的验收：
    /// - Stop 会等所有已建立连接的 OnClientDisconnected 回调链跑完再返回
    /// - 单个 Processor 抛异常不会阻断其它 Processor 的事件
    /// - Stop 期间新连接会被立刻踢掉，不登记票据
    /// - Stop→Start 重启场景下票据表被正确清空
    /// - Stop 的 drain 超时保护生效（不会无限等）
    /// </summary>
    [TestFixture]
    public class TestStopDrain
    {
        private const string Host = "127.0.0.1";
        private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

        private static async Task<Client<WsClient>> ConnectClient(int port)
        {
            var client = new Client<WsClient>();
            client.OnError += err => Console.WriteLine($"[client] {err.Message}");
            var ok = await client.Connect(Host, port, ConnectTimeout);
            if (!ok) throw new Exception($"client connect to {Host}:{port} failed");
            return client;
        }

        /// <summary>
        /// 核心用例：N 个 client 建连后调 <c>server.Stop()</c>，
        /// Stop 返回时必须保证每个 clientId 的 <c>OnClientDisconnected</c> 都已执行完毕。
        /// </summary>
        [Test]
        public async Task Stop_waits_for_all_pending_disconnect_callbacks()
        {
            const int N = 5;

            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            var tracker = new TrackerProcessor();
            server.Register(tracker);
            server.Start(Host, port);

            var clients = new List<Client<WsClient>>();
            try
            {
                for (var i = 0; i < N; i++)
                {
                    clients.Add(await ConnectClient(port));
                }

                // 等 server 侧 Connect 回调全部到齐
                SpinWait.SpinUntil(() => tracker.ConnectCallCount >= N, WaitTimeout);
                Assert.AreEqual(N, tracker.ConnectCallCount, "所有 Connect 回调应该在 Stop 前都已触发");

                // 调用 Stop —— 这是本次用例的核心 assertion
                server.Stop();

                // Stop 返回时，每个已登记的 clientId 都应当 Disconnect 过
                Assert.AreEqual(N, tracker.DisconnectCallCount,
                    "Stop 返回时 DisconnectCallCount 必须等于 ConnectCallCount");

                // 每个 Connect 过的 clientId 都能在 Disconnected 集合里找到
                lock (tracker)
                {
                    CollectionAssert.AreEquivalent(tracker.ConnectedClientIds, tracker.DisconnectedClientIds,
                        "Connect 和 Disconnect 的 clientId 集合必须 1:1 对应");
                }
            }
            finally
            {
                foreach (var c in clients)
                {
                    try { await c.DisconnectAsync(); } catch { /* ignore */ }
                }
            }
        }

        /// <summary>
        /// 如果某个 Processor 的 Disconnect 回调执行需要一段时间（模拟同步 await Redis），
        /// <c>Stop()</c> 必须至少等满这段时间。
        /// </summary>
        [Test]
        public async Task Stop_waits_for_slow_disconnect_callback()
        {
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            var tracker = new TrackerProcessor
            {
                // 明显可测量的阻塞，但远低于 StopDrainTimeout(10s)
                DisconnectBlockFor = TimeSpan.FromMilliseconds(500),
            };
            server.Register(tracker);
            server.Start(Host, port);

            var client = await ConnectClient(port);
            try
            {
                SpinWait.SpinUntil(() => tracker.ConnectCallCount >= 1, WaitTimeout);

                var sw = Stopwatch.StartNew();
                server.Stop();
                sw.Stop();

                Assert.AreEqual(1, tracker.DisconnectCallCount);
                Assert.GreaterOrEqual(sw.ElapsedMilliseconds, 400,
                    $"Stop 应至少等 ~500ms 让 Disconnect 回调跑完，实际 {sw.ElapsedMilliseconds}ms");
            }
            finally
            {
                try { await client.DisconnectAsync(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// 前置 Processor 在 OnClientConnected / OnClientDisconnected 里抛异常，
        /// 后置 Processor 仍必须收到事件，且异常应通过 <c>Server.OnError</c> 上报。
        /// </summary>
        [Test]
        public async Task Processor_exception_does_not_break_subsequent_processors()
        {
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();

            var throwing = new ThrowingProcessor();
            var tracker = new TrackerProcessor();

            var errorCount = 0;
            server.OnError += (_, err) =>
            {
                if (err is InvalidOperationException) Interlocked.Increment(ref errorCount);
            };
            server.Register(throwing);   // 必须先注册抛异常的
            server.Register(tracker);    // 再注册 tracker：验证 tracker 不被前者吞事件
            server.Start(Host, port);

            var client = await ConnectClient(port);
            try
            {
                SpinWait.SpinUntil(() => tracker.ConnectCallCount >= 1, WaitTimeout);

                Assert.AreEqual(1, throwing.ConnectCallCount, "throwing 确实被调到了");
                Assert.AreEqual(1, tracker.ConnectCallCount,
                    "前置 Processor 抛异常不得阻断后续 Processor 的 OnClientConnected");

                // 断开 client，由 client-initiated disconnect 触发 server 侧 OnClientDisconnected
                await client.DisconnectAsync();

                SpinWait.SpinUntil(() => tracker.DisconnectCallCount >= 1, WaitTimeout);

                Assert.AreEqual(1, throwing.DisconnectCallCount);
                Assert.AreEqual(1, tracker.DisconnectCallCount,
                    "前置 Processor 抛异常不得阻断后续 Processor 的 OnClientDisconnected");

                // Connect + Disconnect 各抛 1 次
                Assert.GreaterOrEqual(errorCount, 2,
                    $"Server.OnError 应捕捉到至少 2 次 InvalidOperationException，实际 {errorCount}");
            }
            finally
            {
                try { await client.DisconnectAsync(); } catch { /* ignore */ }
                server.Stop();
            }
        }

        /// <summary>
        /// Stop 是幂等的：重复调用不抛异常。
        /// </summary>
        [Test]
        public void Stop_is_idempotent()
        {
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            server.Register(new TrackerProcessor());
            server.Start(Host, port);

            Assert.DoesNotThrow(() => server.Stop());
            Assert.DoesNotThrow(() => server.Stop(), "第二次 Stop 必须是 no-op");
            Assert.DoesNotThrow(() => server.Stop(), "多次 Stop 都不能抛");
        }

        /// <summary>
        /// Stop 之后重新 Start，原来的票据表和停机标志必须清掉，
        /// 新一轮生命周期下 Connect / Disconnect 计数从 0 开始正常累计。
        /// </summary>
        [Test]
        public async Task Server_can_be_restarted_after_stop()
        {
            var port1 = TestPort.GetFree();
            var server = new Server<WsServer>();
            var tracker = new TrackerProcessor();
            server.Register(tracker);
            server.Start(Host, port1);

            var client1 = await ConnectClient(port1);
            SpinWait.SpinUntil(() => tracker.ConnectCallCount >= 1, WaitTimeout);
            try { await client1.DisconnectAsync(); } catch { /* ignore */ }
            server.Stop();

            Assert.AreEqual(1, tracker.ConnectCallCount);
            Assert.AreEqual(1, tracker.DisconnectCallCount);

            // 第二轮生命周期
            var port2 = TestPort.GetFree();
            server.Start(Host, port2);
            var client2 = await ConnectClient(port2);
            try
            {
                SpinWait.SpinUntil(() => tracker.ConnectCallCount >= 2, WaitTimeout);
                Assert.AreEqual(2, tracker.ConnectCallCount, "重启后新的 Connect 仍要被回调");

                server.Stop();

                Assert.AreEqual(2, tracker.DisconnectCallCount,
                    "重启后 Stop 仍要能 drain 第二轮的连接");
            }
            finally
            {
                try { await client2.DisconnectAsync(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// StopProcessors 的 graceful drain：Stop 之前往 <c>BroadcastQueue</c> 塞 N 条广播，
        /// Stop 返回时这 N 条都应该被 <c>OnBroadcast</c> 消费完（每条还带 10ms 延迟模拟真实 handler）。
        /// 这验证了 <c>ProcessorRunner</c> 在 writer 关闭后仍然继续跑 <c>DoPeriodicWorkAsync</c>
        /// 直到 <c>_broadcastQueue</c> 排空。
        /// </summary>
        [Test]
        public void Stop_drains_pending_broadcast_queue()
        {
            const int N = 50;
            const int delayPerMs = 10;

            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            var tracker = new TrackerProcessor
            {
                BroadcastDelayPer = TimeSpan.FromMilliseconds(delayPerMs),
            };
            server.Register(tracker);
            server.Start(Host, port);

            // 预装 N 条广播（不需要 client；Broadcast 直接入 Runner 内队列）。
            // 由于 Runner 主循环此刻正在 idle/busy 跑 DoPeriodicWork，部分广播可能在 Stop 前已被消费，
            // 我们关心的是"最终 == N"，不是"Stop 前还剩多少"。
            for (var i = 0; i < N; i++)
            {
                server.Broadcast(clientId: 0, eventId: 1, data: i);
            }

            var sw = Stopwatch.StartNew();
            server.Stop();
            sw.Stop();

            Assert.AreEqual(N, tracker.BroadcastCallCount,
                "Stop 返回时 OnBroadcast 必须已经被调用 N 次（graceful drain 不得丢 broadcast）");

            // 粗略下限验证：N 条每条 10ms，主循环 batch=64 一轮吃完，大致 N*delayPerMs 量级。
            // 不做上限 assert，避免 CI flaky；下限也给了一半宽容度。
            Assert.GreaterOrEqual(sw.ElapsedMilliseconds, (N * delayPerMs) / 2,
                $"Stop 应至少等一半的广播工作时长跑完，实际 {sw.ElapsedMilliseconds}ms");
        }

        /// <summary>
        /// 如果 OnBroadcast 每条阻塞得太久，导致 graceful 窗口吃不完：
        /// StopProcessors 的超时兜底应触发，不至于让 Stop 永远卡死。
        /// </summary>
        [Test]
        public void Stop_enforces_graceful_timeout_when_broadcast_too_slow()
        {
            const int N = 200;
            // 每条 200ms × 200 条 = 40 秒，远超 StopProcessors 默认 5s
            const int delayPerMs = 200;

            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            var tracker = new TrackerProcessor
            {
                BroadcastDelayPer = TimeSpan.FromMilliseconds(delayPerMs),
            };
            server.Register(tracker);
            server.Start(Host, port);

            for (var i = 0; i < N; i++)
            {
                server.Broadcast(clientId: 0, eventId: 1, data: i);
            }

            var sw = Stopwatch.StartNew();
            server.Stop();
            sw.Stop();

            // 默认 graceful window = 5s，StopProcessors 再给 +1s 缓冲；
            // 加上上下文切换，整个 Stop 不应显著超过 ~7s。给 12s 作为上限防 CI flaky。
            Assert.Less(sw.ElapsedMilliseconds, 12_000,
                $"Stop 必须在 graceful 超时兜底内返回，实际 {sw.ElapsedMilliseconds}ms");

            // 大量广播一定没跑完——不 assert 具体数字，只验证"不是全跑完的"。
            Assert.Less(tracker.BroadcastCallCount, N,
                "超时场景下 BroadcastCallCount 应小于 N（Runner 被硬 Cancel 打断）");
        }

        /// <summary>
        /// <c>server.Dispose()</c> 会链式调 Stop，重复调用应安全。
        /// </summary>
        [Test]
        public async Task Dispose_after_stop_is_safe()
        {
            var port = TestPort.GetFree();
            var server = new Server<WsServer>();
            var tracker = new TrackerProcessor();
            server.Register(tracker);
            server.Start(Host, port);

            var client = await ConnectClient(port);
            try
            {
                SpinWait.SpinUntil(() => tracker.ConnectCallCount >= 1, WaitTimeout);
            }
            finally
            {
                try { await client.DisconnectAsync(); } catch { /* ignore */ }
            }

            server.Stop();
            Assert.DoesNotThrow(() => server.Dispose(), "Stop 之后再 Dispose 不得抛");
            Assert.AreEqual(1, tracker.DisconnectCallCount);
        }
    }
}
