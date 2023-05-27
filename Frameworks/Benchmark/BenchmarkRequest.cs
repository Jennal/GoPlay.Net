using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using GoPlay.Services;
using GoPlay.Services.Core.Protocols;
using GoPlay.Services.Core.Transport.NetCoreServer;

namespace GoPlay.Benchmarks;

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