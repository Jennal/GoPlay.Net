using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using GoPlay;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.NetCoreServer;

namespace GoPlay.Benchmarks;

// 常驻 MemoryDiagnoser：回归红线里有 "Echo Allocated / op ≤ 16 KB" 这一条，
// 只有开着才能持续验证。代价是 BDN Request 跑时长 +40–50%（约 15s → 22s），
// 完全可接受。Concurrency/Route 因信噪比差或时间成本高，不加。
[MemoryDiagnoser]
public class BenchmarkRequest
{
    private Server _server = null;
    private Client _client = null;

    private PbString _msg = new PbString
    {
        Value = "Hello"
    };
    private Task _srvTask;
    
    [GlobalSetup]
    public async Task Setup()
    {
        _server = new Server<NcServer>();
        _server.Register(new TestProcessor());
        _srvTask = _server.Start("127.0.0.1", 8888);

        _client = new Client<NcClient>();
        await _client.Connect("127.0.0.1", 8888);
        _client.RequestTimeout = TimeSpan.MaxValue;
    }

    [GlobalCleanup]
    public async Task TearDown()
    {
        await _client.DisconnectAsync();
        _server.Stop();
    }
    
    [Benchmark(Baseline = true)]
    public async Task Echo()
    {
        var resp = await _client.Request<PbString, PbString>("test.echo", _msg);
    }
}