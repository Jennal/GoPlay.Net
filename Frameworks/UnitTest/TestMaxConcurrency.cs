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
        public void TestServerDefaultConcurrencyValidation()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Server<TcpServer>(defaultConcurrency: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Server<TcpServer>(defaultConcurrency: -1));
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
}
