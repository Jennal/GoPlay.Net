using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using UnitTest.Processors;
using GoPlay.Services;
using GoPlay.Services.Core.Protocols;
using GoPlay.Services.Core.Transport.NetCoreServer;

namespace UnitTest.Benchmarks;

[Config(typeof(Config))]
[RPlotExporter]
public class RequestBenchmark
{
    private Server _server = null;
    private Client _client = null;

    private PbString _msg = new PbString
    {
        Value = "Hello"
    };
    private Task _srvTask;
    
    public RequestBenchmark()
    {
    }

    private class Config : ManualConfig
    {
        public Config()
        {
            WithOptions(ConfigOptions.DisableOptimizationsValidator);
        }
    }

    [GlobalSetup]
    public async Task Setup()
    {
        _server = new Server<NcServer>();
        _server.Register(new TestProcessor());
        _srvTask = _server.Start("127.0.0.1", 9012);

        _client = new Client<NcClient>();
        await _client.Connect("127.0.0.1", 9012);
        _client.RequestTimeout = TimeSpan.MaxValue;
    }
    
    [Benchmark(Baseline = true)]
    public async Task Echo()
    {
        var resp = await _client.Request<PbString, PbString>("test.echo", _msg);
    }

    // [Benchmark]
    // public void Login()
    // {
    //     
    // }

    public static void Run()
    {
        var summary = BenchmarkRunner.Run(typeof(RequestBenchmark));
        Console.WriteLine(summary);
    }
}