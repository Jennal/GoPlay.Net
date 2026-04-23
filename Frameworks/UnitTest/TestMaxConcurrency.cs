using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnitTest.Helpers;
using GoPlay;
using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports.TCP;

namespace UnitTest
{
    /// <summary>
    /// 验证 [MaxConcurrency(N)] 行为：
    /// 1. attribute 自身校验：N &lt; 1 抛 ArgumentOutOfRangeException
    /// 2. 启动期校验：方法级 N 大于 Processor 解析后并发上限抛 InvalidOperationException
    /// 3. Processor 解析顺序：class attribute 优先，否则用 Server.DefaultConcurrency
    /// 4. 运行期：方法级闸门生效；未标注方法吃满 Processor 预算
    /// </summary>
    public class TestMaxConcurrency
    {
        [Test]
        public void TestAttributeValidation()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MaxConcurrencyAttribute(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MaxConcurrencyAttribute(-1));
            Assert.DoesNotThrow(() => new MaxConcurrencyAttribute(1));
            Assert.DoesNotThrow(() => new MaxConcurrencyAttribute(8));
        }

        [Test]
        public void TestServerDefaultConcurrencyAutoSizing()
        {
            // 0 / 负值：按 Environment.ProcessorCount 自动推导，至少 1
            var expected = Math.Max(1, Environment.ProcessorCount);
            using (var s1 = new Server<TcpServer>(defaultConcurrency: 0))
            {
                Assert.AreEqual(expected, s1.DefaultConcurrency);
            }
            using (var s2 = new Server<TcpServer>(defaultConcurrency: -1))
            {
                Assert.AreEqual(expected, s2.DefaultConcurrency);
            }
            using (var s3 = new Server<TcpServer>(defaultConcurrency: 4))
            {
                Assert.AreEqual(4, s3.DefaultConcurrency);
            }
        }

        [Test]
        public void TestStartupValidationFailWhenMethodExceedsProcessor()
        {
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>();
            try
            {
                // ConcurrencyTooLargeProcessor 自带 [MaxConcurrency(4)]，方法 [MaxConcurrency(8)]
                server.Register(new ConcurrencyTooLargeProcessor());
                Assert.Throws<InvalidOperationException>(() => server.Start("127.0.0.1", port));
            }
            finally
            {
                try { server.Stop(); } catch { /* ignore */ }
            }
        }

        [Test]
        public void TestStartupValidationFailWhenServerDefaultBelowMethod()
        {
            // ServerDefaultDrivenProcessor 不标 attribute，server 默认 2，方法标 4 → 应抛错
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>(defaultConcurrency: 2);
            try
            {
                server.Register(new ServerDefaultDrivenProcessor());
                Assert.Throws<InvalidOperationException>(() => server.Start("127.0.0.1", port));
            }
            finally
            {
                try { server.Stop(); } catch { /* ignore */ }
            }
        }

        [Test]
        public async Task TestMethodLevelLimitTakesEffect()
        {
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>();
            Client<TcpClient> client = null;

            ConcurrencyProbeProcessor.Reset();

            try
            {
                server.Register(new ConcurrencyProbeProcessor());
                server.Start("127.0.0.1", port);

                client = new Client<TcpClient>();
                await client.Connect("127.0.0.1", port);

                // processor [MaxConcurrency(8)]，方法 [MaxConcurrency(2)]
                // 16 条并发期望同时在飞数 == 2
                var tasks = new List<Task>();
                for (int i = 0; i < 16; i++)
                {
                    tasks.Add(client.Request<PbString, PbString>("probe.limited",
                        new PbString { Value = $"v{i}" }));
                }
                await Task.WhenAll(tasks);

                Assert.LessOrEqual(ConcurrencyProbeProcessor.LimitedMaxObserved, 2,
                    $"limited 同时在飞数 {ConcurrencyProbeProcessor.LimitedMaxObserved} 超过了 [MaxConcurrency(2)]");
                Assert.GreaterOrEqual(ConcurrencyProbeProcessor.LimitedMaxObserved, 2,
                    $"limited 同时在飞数始终 {ConcurrencyProbeProcessor.LimitedMaxObserved}，限流闸看起来没拉开");
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                try { server.Stop(); } catch { /* ignore */ }
            }
        }

        [Test]
        public async Task TestUnlimitedMethodUsesProcessorBudget()
        {
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>();
            Client<TcpClient> client = null;

            ConcurrencyProbeProcessor.Reset();

            try
            {
                server.Register(new ConcurrencyProbeProcessor());
                server.Start("127.0.0.1", port);

                client = new Client<TcpClient>();
                await client.Connect("127.0.0.1", port);

                // free 方法未标注，期望吃满 processor [MaxConcurrency(8)]
                var tasks = new List<Task>();
                for (int i = 0; i < 16; i++)
                {
                    tasks.Add(client.Request<PbString, PbString>("probe.free",
                        new PbString { Value = $"v{i}" }));
                }
                await Task.WhenAll(tasks);

                Assert.LessOrEqual(ConcurrencyProbeProcessor.FreeMaxObserved, 8,
                    $"free 同时在飞数 {ConcurrencyProbeProcessor.FreeMaxObserved} 超过 [MaxConcurrency(8)]");
                Assert.Greater(ConcurrencyProbeProcessor.FreeMaxObserved, 2,
                    $"free 同时在飞数 {ConcurrencyProbeProcessor.FreeMaxObserved} 没用到 processor 预算");
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                try { server.Stop(); } catch { /* ignore */ }
            }
        }

        [Test]
        public async Task TestServerDefaultConcurrencyAppliesWhenNoAttribute()
        {
            // ServerDefaultDrivenProcessor 不标 attribute，server defaultConcurrency=8
            // 期望同时在飞数能达到 8
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>(defaultConcurrency: 8);
            Client<TcpClient> client = null;

            ServerDefaultDrivenProcessor.Reset();

            try
            {
                server.Register(new ServerDefaultDrivenProcessor());
                server.Start("127.0.0.1", port);

                client = new Client<TcpClient>();
                await client.Connect("127.0.0.1", port);

                var tasks = new List<Task>();
                for (int i = 0; i < 16; i++)
                {
                    tasks.Add(client.Request<PbString, PbString>("defaulted.work",
                        new PbString { Value = $"v{i}" }));
                }
                await Task.WhenAll(tasks);

                Assert.LessOrEqual(ServerDefaultDrivenProcessor.WorkMaxObserved, 8,
                    $"work 同时在飞数 {ServerDefaultDrivenProcessor.WorkMaxObserved} 超过 Server.DefaultConcurrency=8");
                Assert.Greater(ServerDefaultDrivenProcessor.WorkMaxObserved, 2,
                    $"work 同时在飞数 {ServerDefaultDrivenProcessor.WorkMaxObserved} 没用到 server 默认预算");
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                try { server.Stop(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// 验证"单人通道"语义：class 级 [MaxConcurrency(8)] + 方法级 [MaxConcurrency(1)]，
        /// 跨客户端 [Request] 和 ProcessorRef 两条路径合计 in-flight 严格为 1。
        /// 这同时也是 MAXCONC001 不再对该配置 warn 的正当性依据——配置是有意义的。
        /// </summary>
        [Test]
        public async Task TestMethodLevelOneIsSingleLane()
        {
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>();
            Client<TcpClient> client = null;

            SingleLaneTarget.Reset();

            try
            {
                server.Register(new SingleLaneTarget());
                server.Register(new SingleLaneDriver());
                server.Start("127.0.0.1", port);

                client = new Client<TcpClient>();
                await client.Connect("127.0.0.1", port);

                var tasks = new List<Task>();
                for (int i = 0; i < 6; i++)
                {
                    tasks.Add(client.Request<PbString, PbString>("lane_target.solo",
                        new PbString { Value = $"c{i}" }));
                }
                tasks.Add(client.Request<PbString, PbString>("lane_driver.fan_out",
                    new PbString { Value = "6" }));

                await Task.WhenAll(tasks);

                Assert.AreEqual(1, SingleLaneTarget.SoloMaxObserved,
                    $"Solo 同时在飞数 {SingleLaneTarget.SoloMaxObserved} != 1，单人通道语义失效");
                Assert.GreaterOrEqual(SingleLaneTarget.SoloTotalInvocations, 12,
                    $"Solo 总调用数 {SingleLaneTarget.SoloTotalInvocations} < 12，路径没跑全");
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                try { server.Stop(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// 验证：方法级 [MaxConcurrency(N)] 的限流对 ProcessorRef 跨 Processor 调用同样生效，
        /// 且与客户端 [Request] 路径共享同一把 semaphore。
        /// 场景：Target.Limited 同时是 [Request("limited_x")]+[MaxConcurrency(2)]；
        ///      客户端并发 8 条 limited_x，同时 Trigger 另一 Processor 派发 10 条跨调用——
        ///      两条路径合计 18 条 in-flight，方法级上限仍应为 2。
        /// </summary>
        [Test]
        public async Task TestMethodLevelLimitCoversProcessorRefPath()
        {
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>();
            Client<TcpClient> client = null;

            MethodLimitTarget.Reset();

            try
            {
                server.Register(new MethodLimitTarget());
                server.Register(new MethodLimitDriver());
                server.Start("127.0.0.1", port);

                client = new Client<TcpClient>();
                await client.Connect("127.0.0.1", port);

                var tasks = new List<Task>();
                // 客户端直连路径：8 条
                for (int i = 0; i < 8; i++)
                {
                    tasks.Add(client.Request<PbString, PbString>("limit_target.limited_x",
                        new PbString { Value = $"c{i}" }));
                }
                // Driver 内部再 fire 10 条跨 Processor 调用到 MethodLimitTarget.Limited
                tasks.Add(client.Request<PbString, PbString>("limit_driver.trigger",
                    new PbString { Value = "10" }));

                await Task.WhenAll(tasks);

                Assert.LessOrEqual(MethodLimitTarget.LimitedMaxObserved, 2,
                    $"Limited 同时在飞数 {MethodLimitTarget.LimitedMaxObserved} 超过 [MaxConcurrency(2)]，" +
                    "方法级 sem 没有覆盖 ProcessorRef 跨调用路径。");
                Assert.GreaterOrEqual(MethodLimitTarget.LimitedMaxObserved, 2,
                    $"Limited 同时在飞数始终 {MethodLimitTarget.LimitedMaxObserved}，限流闸看起来没拉开。");
                Assert.GreaterOrEqual(MethodLimitTarget.LimitedTotalInvocations, 18,
                    $"Limited 总调用数 {MethodLimitTarget.LimitedTotalInvocations} < 18，路径没跑全。");
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                try { server.Stop(); } catch { /* ignore */ }
            }
        }

        /// <summary>
        /// 验证：纯 [ProcessorApi] + [MaxConcurrency] 方法（不是 [Request]/[Notify]）
        /// 通过 ProcessorRef 跨 Processor 调用时同样受方法级 semaphore 约束。
        /// 这对应 Runner 侧扫描纯 ProcessorApi 方法并建方法级 sem 的新分支，
        /// 以及 Generator 侧 ComputeRouteKey 对纯 ProcessorApi fallback 到 method.Name 的策略。
        /// </summary>
        [Test]
        public async Task TestPureProcessorApiIsThrottled()
        {
            var port = TestPort.GetFree();
            var server = new Server<TcpServer>();
            Client<TcpClient> client = null;

            PureApiTarget.Reset();

            try
            {
                server.Register(new PureApiTarget());
                server.Register(new PureApiDriver());
                server.Start("127.0.0.1", port);

                client = new Client<TcpClient>();
                await client.Connect("127.0.0.1", port);

                // 客户端无法直接调 PureApiTarget.Work（它不是 [Request]），全部走 Driver 的跨调用
                var tasks = new List<Task>();
                for (int i = 0; i < 3; i++)
                {
                    tasks.Add(client.Request<PbString, PbString>("pure_driver.trigger",
                        new PbString { Value = "6" }));
                }

                await Task.WhenAll(tasks);

                Assert.LessOrEqual(PureApiTarget.WorkMaxObserved, 2,
                    $"Work 同时在飞数 {PureApiTarget.WorkMaxObserved} 超过 [MaxConcurrency(2)]，" +
                    "方法级 sem 没覆盖纯 [ProcessorApi] 路径。");
                Assert.GreaterOrEqual(PureApiTarget.WorkMaxObserved, 2,
                    $"Work 同时在飞数始终 {PureApiTarget.WorkMaxObserved}，限流闸看起来没拉开。");
                Assert.GreaterOrEqual(PureApiTarget.WorkTotalInvocations, 18,
                    $"Work 总调用数 {PureApiTarget.WorkTotalInvocations} < 18（3*6），路径没跑全。");
            }
            finally
            {
                try { if (client != null) await client.DisconnectAsync(); } catch { /* ignore */ }
                try { server.Stop(); } catch { /* ignore */ }
            }
        }
    }

    [Processor("toolarge")]
    [MaxConcurrency(4)]
    internal class ConcurrencyTooLargeProcessor : ProcessorBase
    {
        public override string[] Pushes => Array.Empty<string>();

        // 8 > class 上的 4，启动期应抛错
        [Request("oops")]
        [MaxConcurrency(8)]
        public PbString Oops(Header h, PbString s) => s;
    }

    [Processor("probe")]
    [MaxConcurrency(8)]
    internal class ConcurrencyProbeProcessor : ProcessorBase
    {
        public override string[] Pushes => Array.Empty<string>();

        public static int LimitedConcurrent;
        public static int LimitedMaxObserved;
        public static int FreeConcurrent;
        public static int FreeMaxObserved;

        public static void Reset()
        {
            LimitedConcurrent = 0;
            LimitedMaxObserved = 0;
            FreeConcurrent = 0;
            FreeMaxObserved = 0;
        }

        [Request("limited")]
        [MaxConcurrency(2)]
        public async Task<PbString> Limited(Header h, PbString s)
        {
            var c = Interlocked.Increment(ref LimitedConcurrent);
            ConcurrencyTestUtil.BumpMax(ref LimitedMaxObserved, c);
            await Task.Delay(80);
            Interlocked.Decrement(ref LimitedConcurrent);
            return new PbString { Value = s.Value };
        }

        [Request("free")]
        public async Task<PbString> Free(Header h, PbString s)
        {
            var c = Interlocked.Increment(ref FreeConcurrent);
            ConcurrencyTestUtil.BumpMax(ref FreeMaxObserved, c);
            await Task.Delay(80);
            Interlocked.Decrement(ref FreeConcurrent);
            return new PbString { Value = s.Value };
        }
    }

    /// <summary>
    /// 不标 class 级 [MaxConcurrency]，并发上限完全由 Server.DefaultConcurrency 决定。
    /// 同时也在方法上标 [MaxConcurrency(4)] 用于校验失败场景。
    /// </summary>
    [Processor("defaulted")]
    internal class ServerDefaultDrivenProcessor : ProcessorBase
    {
        public override string[] Pushes => Array.Empty<string>();

        public static int WorkConcurrent;
        public static int WorkMaxObserved;

        public static void Reset()
        {
            WorkConcurrent = 0;
            WorkMaxObserved = 0;
        }

        [Request("work")]
        public async Task<PbString> Work(Header h, PbString s)
        {
            var c = Interlocked.Increment(ref WorkConcurrent);
            ConcurrencyTestUtil.BumpMax(ref WorkMaxObserved, c);
            await Task.Delay(80);
            Interlocked.Decrement(ref WorkConcurrent);
            return new PbString { Value = s.Value };
        }

        // 方法级 4：在 server defaultConcurrency=2 时会触发启动校验失败
        [Request("over")]
        [MaxConcurrency(4)]
        public PbString Over(Header h, PbString s) => s;
    }

    internal static class ConcurrencyTestUtil
    {
        public static void BumpMax(ref int slot, int candidate)
        {
            int snapshot;
            do
            {
                snapshot = Volatile.Read(ref slot);
                if (candidate <= snapshot) return;
            } while (Interlocked.CompareExchange(ref slot, candidate, snapshot) != snapshot);
        }
    }

    /// <summary>
    /// "单人通道"测试目标：class 级 8，方法级 1，预期 <see cref="Solo"/> 任何时刻
    /// 只能有 1 个 in-flight（客户端直连 + 跨 Processor 调用合计）。
    /// </summary>
    [Processor("lane_target")]
    [MaxConcurrency(8)]
    internal class SingleLaneTarget : ProcessorBase
    {
        public override string[] Pushes => Array.Empty<string>();

        public static int SoloConcurrent;
        public static int SoloMaxObserved;
        public static int SoloTotalInvocations;

        public static void Reset()
        {
            SoloConcurrent = 0;
            SoloMaxObserved = 0;
            SoloTotalInvocations = 0;
        }

        [Request("solo")]
        [MaxConcurrency(1)]
        public async Task<PbString> Solo(Header h, PbString s)
        {
            Interlocked.Increment(ref SoloTotalInvocations);
            var c = Interlocked.Increment(ref SoloConcurrent);
            ConcurrencyTestUtil.BumpMax(ref SoloMaxObserved, c);
            await Task.Delay(30);
            Interlocked.Decrement(ref SoloConcurrent);
            return new PbString { Value = s.Value };
        }
    }

    [Processor("lane_driver")]
    [MaxConcurrency(8)]
    internal class SingleLaneDriver : ProcessorBase
    {
        public override string[] Pushes => Array.Empty<string>();

        [Request("fan_out")]
        public async Task<PbString> FanOut(Header h, PbString s)
        {
            if (!int.TryParse(s.Value, out var n)) n = 6;
            var targetRef = Server.GetProcessor<SingleLaneTarget>();

            var tasks = new List<Task<PbString>>(n);
            for (int i = 0; i < n; i++)
            {
                var idx = i;
                tasks.Add(targetRef.Request("lane_target.solo",
                    p => p.Solo(null, new PbString { Value = $"x{idx}" })));
            }

            await Task.WhenAll(tasks);
            return new PbString { Value = s.Value };
        }
    }

    /// <summary>
    /// 被限流的目标 Processor：<see cref="Limited"/> 同时是 [Request] 和跨 Processor 调用入口。
    /// 方法级 [MaxConcurrency(2)] 应同时约束两条路径。
    /// </summary>
    [Processor("limit_target")]
    [MaxConcurrency(8)]
    internal class MethodLimitTarget : ProcessorBase
    {
        public override string[] Pushes => Array.Empty<string>();

        public static int LimitedConcurrent;
        public static int LimitedMaxObserved;
        public static int LimitedTotalInvocations;

        public static void Reset()
        {
            LimitedConcurrent = 0;
            LimitedMaxObserved = 0;
            LimitedTotalInvocations = 0;
        }

        // 注意：[ProcessorApi] 是给 Source Generator 用的——本测试 UnitTest 项目没挂 generator，
        // 所以下面 Driver 里用 ProcessorRef<T>.Request(routeKey, fn) 手动发起调用来模拟
        // generator 会生成的代码，直接覆盖 Runner 侧按 routeKey 查方法级 sem 的行为。
        [Request("limited_x")]
        [MaxConcurrency(2)]
        public async Task<PbString> Limited(Header h, PbString s)
        {
            Interlocked.Increment(ref LimitedTotalInvocations);
            var c = Interlocked.Increment(ref LimitedConcurrent);
            ConcurrencyTestUtil.BumpMax(ref LimitedMaxObserved, c);
            await Task.Delay(60);
            Interlocked.Decrement(ref LimitedConcurrent);
            return new PbString { Value = s.Value };
        }
    }

    /// <summary>
    /// 驱动 Processor：收到客户端 trigger 后，向 <see cref="MethodLimitTarget"/> 派发 N 条
    /// ProcessorRef 跨调用。派发时带 routeKey "limit_target.limited_x"，和 Route.RouteString 一致。
    /// </summary>
    [Processor("limit_driver")]
    [MaxConcurrency(8)]
    internal class MethodLimitDriver : ProcessorBase
    {
        public override string[] Pushes => Array.Empty<string>();

        [Request("trigger")]
        public async Task<PbString> Trigger(Header h, PbString s)
        {
            if (!int.TryParse(s.Value, out var n)) n = 10;
            var targetRef = Server.GetProcessor<MethodLimitTarget>();

            var tasks = new List<Task<PbString>>(n);
            for (int i = 0; i < n; i++)
            {
                var idx = i;
                // routeKey 必须和 Route.RouteString 一致："{processorName}.{methodName}".ToLower()
                tasks.Add(targetRef.Request("limit_target.limited_x",
                    p => p.Limited(null, new PbString { Value = $"x{idx}" })));
            }

            await Task.WhenAll(tasks);
            return new PbString { Value = s.Value };
        }
    }

    /// <summary>
    /// 纯 [ProcessorApi] + [MaxConcurrency] 目标：<see cref="Work"/> 不是 [Request]/[Notify]，
    /// 只能从其他 Processor 通过 ProcessorRef 触达。方法级 sem 仍应按 routeKey 命中。
    /// </summary>
    [Processor("pure_target")]
    [MaxConcurrency(8)]
    internal class PureApiTarget : ProcessorBase
    {
        public override string[] Pushes => Array.Empty<string>();

        public static int WorkConcurrent;
        public static int WorkMaxObserved;
        public static int WorkTotalInvocations;

        public static void Reset()
        {
            WorkConcurrent = 0;
            WorkMaxObserved = 0;
            WorkTotalInvocations = 0;
        }

        [ProcessorApi]
        [MaxConcurrency(2)]
        public async Task<PbString> Work(PbString s)
        {
            Interlocked.Increment(ref WorkTotalInvocations);
            var c = Interlocked.Increment(ref WorkConcurrent);
            ConcurrencyTestUtil.BumpMax(ref WorkMaxObserved, c);
            await Task.Delay(40);
            Interlocked.Decrement(ref WorkConcurrent);
            return new PbString { Value = s.Value };
        }
    }

    /// <summary>
    /// 调用 <see cref="PureApiTarget.Work"/> 的发起方。UnitTest 项目未挂 generator，
    /// 这里手动以 <c>ProcessorRef.Request(routeKey, fn)</c> 模拟 generator 会生成的代码，
    /// routeKey 与 Runner 侧对纯 <c>[ProcessorApi]</c> 方法建表的 key 严格一致：
    /// <c>"{procName}.{methodName}".ToLower()</c>。
    /// </summary>
    [Processor("pure_driver")]
    [MaxConcurrency(8)]
    internal class PureApiDriver : ProcessorBase
    {
        public override string[] Pushes => Array.Empty<string>();

        [Request("trigger")]
        public async Task<PbString> Trigger(Header h, PbString s)
        {
            if (!int.TryParse(s.Value, out var n)) n = 6;
            var targetRef = Server.GetProcessor<PureApiTarget>();

            var tasks = new List<Task<PbString>>(n);
            for (int i = 0; i < n; i++)
            {
                var idx = i;
                tasks.Add(targetRef.Request("pure_target.work",
                    p => p.Work(new PbString { Value = $"x{idx}" })));
            }

            await Task.WhenAll(tasks);
            return new PbString { Value = s.Value };
        }
    }
}
