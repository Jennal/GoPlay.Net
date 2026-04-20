using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.NetCoreServer;

namespace GoPlay.Benchmarks;

/// <summary>
/// 对比不同 Processor 并发度 × 业务延迟 × 客户端并发度 的吞吐/耗时差距。
///
/// 核心对比：
/// - Concurrency = 1      → 等价旧架构（ProcessorBase.PackageLoopFrame + Task.Wait 串行）
///                          实现：注册 SlowSerialProcessor（不标 attribute，吃 Server 默认 1）
/// - Concurrency = 64/128 → 新架构 ProcessorRunner + Channel + ExclusiveScheduler 的流水线能力
///                          实现：注册 SlowPipelineProcessor（[MaxConcurrency(64)]）
///
/// 业务延迟 D>0 时（await Task.Delay(D)），旧架构每条消息占用整个处理 "虚线程" D ms，
/// 单 processor 理论上限 = 1000/D req/s；新架构可达 Concurrency * 1000/D req/s。
/// </summary>
public static class ConcurrencyReport
{
    private record Scenario(int DelayMs, int Concurrency, int BatchSize, int Rounds);

    public static async Task Run()
    {
        Console.WriteLine();
        Console.WriteLine("================ ConcurrencyReport ================");
        Console.WriteLine("Scenario: 单 client, WhenAll 并发发 BatchSize 条请求, 重复 Rounds 轮");
        Console.WriteLine("Concurrency=1 注册 SlowSerialProcessor; Concurrency>1 注册 SlowPipelineProcessor");
        Console.WriteLine();

        var scenarios = new[]
        {
            // DelayMs   Concurrency   BatchSize   Rounds
            new Scenario(0,    1,   200, 5),   // 零业务延迟，主要看 Channel + 编译委托 vs BlockingCollection + 反射
            new Scenario(0,   64,   200, 5),
            new Scenario(10,   1,   100, 3),
            new Scenario(10,  64,   100, 3),
            new Scenario(50,   1,    50, 2),
            new Scenario(50,  64,    50, 2),
            new Scenario(100,  1,    50, 2),
            new Scenario(100, 64,    50, 2),
        };

        var results = new List<(Scenario s, double avgMs, double qps)>();

        foreach (var s in scenarios)
        {
            var r = await Measure(s).ConfigureAwait(false);
            results.Add((s, r.avgMs, r.qps));
        }

        Console.WriteLine();
        Console.WriteLine("| Delay(ms) | Concurrency | BatchSize | Wall(ms) | Throughput(req/s) | Speedup |");
        Console.WriteLine("|----------:|------------:|----------:|---------:|------------------:|--------:|");

        for (int i = 0; i < results.Count; i += 2)
        {
            var baseline = results[i];
            var optimized = results[i + 1];
            var speedup = baseline.avgMs / Math.Max(optimized.avgMs, 0.0001);
            Console.WriteLine($"| {baseline.s.DelayMs,9} | {baseline.s.Concurrency,11} | {baseline.s.BatchSize,9} | {baseline.avgMs,8:F2} | {baseline.qps,17:F0} | {"1.00x",7} |");
            Console.WriteLine($"| {optimized.s.DelayMs,9} | {optimized.s.Concurrency,11} | {optimized.s.BatchSize,9} | {optimized.avgMs,8:F2} | {optimized.qps,17:F0} | {speedup,6:F2}x |");
        }
        Console.WriteLine();

        await RunMethodConcurrencyDemo().ConfigureAwait(false);
    }

    /// <summary>
    /// 方法级 [MaxConcurrency(N)] 演示：
    /// 同一 processor（[MaxConcurrency(64)]）下，对比两条路由：
    /// - limited.slow  : 不标注，吃满 processor 预算，理论 QPS ≈ 64 * 1000/D
    /// - limited.slow8 : 标 [MaxConcurrency(8)]，被方法级闸门压住，理论 QPS ≈ 8 * 1000/D
    /// 期望 ratio(slow / slow8) ≈ 8。
    /// </summary>
    private static async Task RunMethodConcurrencyDemo()
    {
        Console.WriteLine("================ Method-level [MaxConcurrency] ================");
        Console.WriteLine("MethodLimitedProcessor [MaxConcurrency(64)]; Slow8 标 [MaxConcurrency(8)]");
        Console.WriteLine();

        var delays = new[] { 10, 50, 100 };
        const int batch = 64;
        const int rounds = 3;

        var port = AllocFreePort();
        var server = new Server<NcServer>();
        server.Register(new MethodLimitedProcessor());
        _ = server.Start("127.0.0.1", port);

        var client = new Client<NcClient>();
        client.RequestTimeout = TimeSpan.MaxValue;
        var ok = await client.Connect("127.0.0.1", port, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        if (!ok) throw new Exception($"connect failed: port={port}");

        try
        {
            await SendBatch(client, "limited.slow", 0, 16).ConfigureAwait(false);

            Console.WriteLine("| Delay(ms) | Route          | Batch | Wall(ms) | Throughput(req/s) | ConcurrencyCap |");
            Console.WriteLine("|----------:|:---------------|------:|---------:|------------------:|---------------:|");

            foreach (var d in delays)
            {
                var slow = await MeasureRoute(client, "limited.slow", d, batch, rounds).ConfigureAwait(false);
                var slow8 = await MeasureRoute(client, "limited.slow8", d, batch, rounds).ConfigureAwait(false);
                Console.WriteLine($"| {d,9} | limited.slow   | {batch,5} | {slow.avgMs,8:F2} | {slow.qps,17:F0} | {"64 (proc)",14} |");
                Console.WriteLine($"| {d,9} | limited.slow8  | {batch,5} | {slow8.avgMs,8:F2} | {slow8.qps,17:F0} | {"8 (method)",14} |");
            }
            Console.WriteLine();
        }
        finally
        {
            try { await client.DisconnectAsync().ConfigureAwait(false); } catch { /* ignore */ }
            try { server.Stop(); } catch { /* ignore */ }
        }
    }

    private static async Task<(double avgMs, double qps)> MeasureRoute(Client client, string route, int delayMs, int batchSize, int rounds)
    {
        var elapsedMs = new List<double>(rounds);
        for (int r = 0; r < rounds; r++)
        {
            var sw = Stopwatch.StartNew();
            await SendBatch(client, route, delayMs, batchSize).ConfigureAwait(false);
            sw.Stop();
            elapsedMs.Add(sw.Elapsed.TotalMilliseconds);
        }
        var avg = elapsedMs.Average();
        return (avg, batchSize * 1000.0 / avg);
    }

    private static async Task SendBatch(Client client, string route, int delayMs, int batchSize)
    {
        var msg = new PbString { Value = delayMs.ToString() };
        var tasks = new Task[batchSize];
        for (int i = 0; i < batchSize; i++)
        {
            tasks[i] = client.Request<PbString, PbString>(route, msg);
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task<(double avgMs, double qps)> Measure(Scenario s)
    {
        var port = AllocFreePort();
        // Server defaultConcurrency=1: SlowSerialProcessor 不标 attribute 时正好走串行；
        // SlowPipelineProcessor 自带 [MaxConcurrency(64)] 会覆盖默认值。
        var server = new Server<NcServer>(defaultConcurrency: 1);
        if (s.Concurrency == 1)
            server.Register(new SlowSerialProcessor());
        else
            server.Register(new SlowPipelineProcessor());

        _ = server.Start("127.0.0.1", port);

        var client = new Client<NcClient>();
        client.RequestTimeout = TimeSpan.MaxValue;
        var ok = await client.Connect("127.0.0.1", port, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        if (!ok) throw new Exception($"connect failed: port={port}");

        try
        {
            // 预热
            await SendBatch(client, "slow.slow", s.DelayMs, Math.Min(16, s.BatchSize)).ConfigureAwait(false);

            var elapsedMs = new List<double>(s.Rounds);
            for (int r = 0; r < s.Rounds; r++)
            {
                var sw = Stopwatch.StartNew();
                await SendBatch(client, "slow.slow", s.DelayMs, s.BatchSize).ConfigureAwait(false);
                sw.Stop();
                elapsedMs.Add(sw.Elapsed.TotalMilliseconds);
            }

            var avgMs = elapsedMs.Average();
            var qps = s.BatchSize * 1000.0 / avgMs;

            Console.WriteLine(
                $"  delay={s.DelayMs,3}ms concurrency={s.Concurrency,3} batch={s.BatchSize,3} rounds={s.Rounds} " +
                $"=> avg wall={avgMs,8:F2}ms, qps={qps,8:F0}");

            return (avgMs, qps);
        }
        finally
        {
            try { await client.DisconnectAsync().ConfigureAwait(false); } catch { /* ignore */ }
            try { server.Stop(); } catch { /* ignore */ }
        }
    }

    private static int AllocFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
