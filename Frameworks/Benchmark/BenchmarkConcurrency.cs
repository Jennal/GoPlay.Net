using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.NetCoreServer;

namespace GoPlay.Benchmarks;

/// <summary>
/// 对比 Concurrency=1（模拟旧架构串行）与 Concurrency=64（新架构流水线）
/// 在不同业务延迟下的单位时间吞吐。
///
/// 切档不通过运行时静态字段，而是切 Processor class：
///   - Concurrency==1 → 注册 SlowSerialProcessor（不标 attribute，吃 Server 默认 1）
///   - Concurrency&gt;1 → 注册 SlowPipelineProcessor（[MaxConcurrency(64)]）
///
/// time/op 越低 → 流水线效果越好。
/// </summary>
[Config(typeof(Config))]
public class BenchmarkConcurrency
{
    private class Config : ManualConfig
    {
        public Config()
        {
            WithOptions(ConfigOptions.DisableOptimizationsValidator);
        }
    }

    [Params(0, 10, 50)]
    public int DelayMs;

    [Params(1, 64)]
    public int Concurrency;

    [Params(50)]
    public int BatchSize;

    private Server _server;
    private Client _client;
    private PbString _msg;

    [GlobalSetup]
    public async Task Setup()
    {
        var port = AllocFreePort();
        _server = new Server<NcServer>(defaultConcurrency: 1);

        if (Concurrency == 1)
            _server.Register(new SlowSerialProcessor());
        else
            _server.Register(new SlowPipelineProcessor());

        _ = _server.Start("127.0.0.1", port);

        _client = new Client<NcClient>();
        _client.RequestTimeout = TimeSpan.MaxValue;
        var ok = await _client.Connect("127.0.0.1", port, TimeSpan.FromSeconds(10));
        if (!ok) throw new Exception($"connect failed: port={port}");

        _msg = new PbString { Value = DelayMs.ToString() };
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        try { if (_client != null) await _client.DisconnectAsync(); } catch { /* ignore */ }
        try { _server?.Stop(); } catch { /* ignore */ }
    }

    [Benchmark]
    public async Task ConcurrentBatch()
    {
        var tasks = new Task[BatchSize];
        for (int i = 0; i < BatchSize; i++)
        {
            tasks[i] = _client.Request<PbString, PbString>("slow.slow", _msg);
        }
        await Task.WhenAll(tasks);
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
