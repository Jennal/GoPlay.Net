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

    // 三档 payload 分档，用来摸清 body 分配占比对单请求 Allocated / RTT 的影响：
    //   Small  ~7 B     → 当前 Echo baseline，body byte[] 几乎可忽略
    //   Medium ~1 KB    → 典型 RPC 消息体量，body byte[] 成为次级分配源
    //   Large  ~10 KB   → 接近分块阈值（MAX_CHUNK_SIZE=63487）之前的单包热路径上限
    // 三档都走 zero-alloc 发包路径（Split 小消息分支），区别只在 ParseRaw 里
    // `body = data.Slice(...).ToArray()` 那次分配的绝对字节数。
    // 若 body ArrayPool 改造后 Medium/Large 两档 Allocated 明显下降、Small 持平，则说明
    // 收益是 payload-size-linear 的，值得做；反之则不值得。
    private PbString _msgSmall = new PbString { Value = "Hello" };
    private PbString _msgMedium = new PbString { Value = new string('x', 1024) };
    private PbString _msgLarge = new PbString { Value = new string('x', 10 * 1024) };
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
    public async Task EchoSmall()
    {
        var resp = await _client.Request<PbString, PbString>("test.echo", _msgSmall);
    }

    [Benchmark]
    public async Task EchoMedium()
    {
        var resp = await _client.Request<PbString, PbString>("test.echo", _msgMedium);
    }

    [Benchmark]
    public async Task EchoLarge()
    {
        var resp = await _client.Request<PbString, PbString>("test.echo", _msgLarge);
    }
}