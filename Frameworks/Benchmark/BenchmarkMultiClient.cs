using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using GoPlay;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.NetCoreServer;

namespace GoPlay.Benchmarks;

/// <summary>
/// 多客户端多请求 steady-state 吞吐基线：<see cref="ClientCount"/> 个长连接并发，
/// 每连接内串行打 <see cref="RequestsPerClient"/> 条 Echo。
///
/// <para>
/// <b>与现有 benchmark 的定位差异</b>：
/// <list type="bullet">
/// <item><see cref="BenchmarkRequest"/>：1 client × 1 串行 RTT（测 Client↔Server 单连接往返时延 + 单 RTT 分配）。</item>
/// <item><see cref="BenchmarkConcurrency"/>：1 client × N 条 <c>WhenAll</c> 请求（测单连接 async pipeline + Processor
///   <c>[MaxConcurrency]</c> 调度）。</item>
/// <item><b>本 benchmark</b>：N clients × M 串行（每 client 内部不并发，所以单测隔离出的是
///   <b>多连接</b>并发时 Server 端 session fanout / SessionSender 吞吐的能力，避开单连接流水线这个维度）。</item>
/// </list>
/// </para>
///
/// <para>
/// <b>为什么 client 内部串行</b>：若每 client 再 <c>WhenAll</c>，就会把"单连接并发"和"多连接并发"两条正交维度
/// 混在一起，测得的数只能反映最大那条的能力，另一条被遮蔽。保持 client 内部串行 + client 间并发，
/// 得到的 QPS 主要受限于 server 多连接 fanout 能力（accept 注册竞态 / [MaxConcurrency] 全局预算 /
/// per-session <c>SessionSender.Channel</c> 吞吐），与 BenchmarkConcurrency 形成清晰互补。
/// </para>
///
/// <para>
/// <b>Setup 策略</b>：所有 client 在 <see cref="Setup"/> 里 <c>Task.Run + WhenAll</c> 建连，
/// iteration 只跑请求，**cold-start connect 成本不进 measurement**。这与 <see cref="BenchmarkRequest"/>
/// 一致，也是 BDN 惯例。Cold-start 多连接接入的场景由
/// <c>UnitTest.TestNetCoreServer.BenchmarkMultiClientRequest</c> 单测覆盖。
/// </para>
/// </summary>
[Config(typeof(Config))]
[MemoryDiagnoser]
public class BenchmarkMultiClient
{
    private class Config : ManualConfig
    {
        public Config()
        {
            // 和 BenchmarkConcurrency 保持一致：多连接场景 iteration 间方差相对大（TCP TIME_WAIT、
            // IOCP thread pool 扩容），不强行做 steady-state detection。
            WithOptions(ConfigOptions.DisableOptimizationsValidator);
        }
    }

    /// <summary>
    /// 并发 client（=TCP 长连接）数量。
    /// 选值依据：
    /// - 10：低负载，验证框架在普通 RPC 客户端数下的基线；
    /// - 100：和 <c>TestNetCoreServer.BenchmarkMultiClientRequest</c> 对齐，既是稳定性回归锚点，
    ///   也是"缺陷 4（server session 注册竞态）"是否复活的探针。
    /// </summary>
    [Params(10, 100)]
    public int ClientCount;

    /// <summary>
    /// 每个 client 内**串行**发的请求数。10 / 100 两档：
    /// - 10：per-iteration 总请求 = ClientCount × 10，iteration 短（几十 ms），适合压 latency；
    /// - 100：per-iteration 总请求 = ClientCount × 100，iteration 较长（几百 ms 到秒级），
    ///   摊平 one-off 抖动，适合读稳态 QPS 和 Allocated/op。
    /// </summary>
    [Params(10, 100)]
    public int RequestsPerClient;

    private Server _server = null!;
    private Client[] _clients = null!;
    private PbString _msg = null!;
    private int _port;

    [GlobalSetup]
    public async Task Setup()
    {
        _port = AllocFreePort();
        _server = new Server<NcServer>();
        _server.Register(new TestProcessor());
        _ = _server.Start("127.0.0.1", _port);

        // 并行建连：和 BenchmarkMultiClientRequest 单测同款压法，保证 server 端 accept fanout 能顶住。
        // 缺陷 4 修复（NcServer PackServer.OnConnecting 里注册 session）后这里 100 连接稳定 < 200ms。
        _clients = new Client[ClientCount];
        var connectTasks = new Task[ClientCount];
        for (int i = 0; i < ClientCount; i++)
        {
            var idx = i;
            connectTasks[i] = Task.Run(async () =>
            {
                var c = new Client<NcClient>();
                // 业务 request 不设死超时：BDN warmup 可能跑几十秒，Request 不应 time out。
                c.RequestTimeout = TimeSpan.MaxValue;
                var ok = await c.Connect("127.0.0.1", _port, TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                if (!ok)
                    throw new Exception($"BenchmarkMultiClient Setup: connect failed for client {idx} on port {_port}");
                _clients[idx] = c;
            });
        }
        await Task.WhenAll(connectTasks).ConfigureAwait(false);

        _msg = new PbString { Value = "Hello" };

        // 预热一轮：各 client 先打一条 Echo，吃掉 JIT tiered compilation 的 Tier0 路径 + ArrayBufferWriter
        // 首次扩容，避免 benchmark 第一 iteration 明显偏高。不计入测量，BDN 本身也会 warmup，但预热
        // 一下能让 warmup 阶段的 Allocated 曲线更平。
        var warm = new Task[ClientCount];
        for (int i = 0; i < ClientCount; i++)
        {
            var c = _clients[i];
            var msg = _msg;
            warm[i] = Task.Run(async () => { await c.Request<PbString, PbString>("test.echo", msg).ConfigureAwait(false); });
        }
        await Task.WhenAll(warm).ConfigureAwait(false);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        if (_clients != null)
        {
            foreach (var c in _clients)
            {
                if (c == null) continue;
                try { await c.DisconnectAsync().ConfigureAwait(false); } catch { /* ignore */ }
            }
        }
        try { _server?.Stop(); } catch { /* ignore */ }
    }

    /// <summary>
    /// per-iteration：每个 client 内部串行打 <see cref="RequestsPerClient"/> 条 Echo；
    /// <see cref="ClientCount"/> 个 client 之间 <c>Task.WhenAll</c> 并发。
    ///
    /// wall-time 接近 <c>RequestsPerClient × SingleRTT</c>（几乎独立于 ClientCount，只要 server fanout 不饱和）。
    /// QPS = <c>ClientCount × RequestsPerClient / wall-time</c>，用来对比框架多连接扩展效率。
    /// </summary>
    [Benchmark]
    public async Task MultiClientSerialRequest()
    {
        var tasks = new Task[ClientCount];
        for (int i = 0; i < ClientCount; i++)
        {
            var c = _clients[i];
            var n = RequestsPerClient;
            var msg = _msg;
            tasks[i] = Task.Run(async () =>
            {
                for (int j = 0; j < n; j++)
                {
                    await c.Request<PbString, PbString>("test.echo", msg).ConfigureAwait(false);
                }
            });
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static int AllocFreePort()
    {
        // 与 BenchmarkConcurrency / ConcurrencyReport 同款实现：绑 0 → OS 分一个 ephemeral port 再关，
        // 避免 benchmark 并行跑时端口撞车（BenchmarkRequest 用的是固定 8888）。
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
