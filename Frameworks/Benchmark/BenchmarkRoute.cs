using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using GoPlay.Services.Core.Encodes;
using GoPlay.Services.Core.Protocols;
using GoPlay.Services.Core.Routers;
using GoPlay.Services.Core.Utils;

namespace GoPlay.Benchmarks;

public class BenchmarkRoute
{
    private TestProcessor _testProcessor;
    private Route route;
    private Package<PbString> pack = new Package<PbString>
    {
        Header = new Header
        {
            PackageInfo = new PackageInfo
            {
                EncodingType = EncodingType.Protobuf,
            }
        },
        RawData = ProtobufEncoder.Instance.Encode(new PbString
        {
            Value = "Hello"
        }),
    };

    private PbString val = new PbString
    {
        Value = "Hello"
    };

    private MethodInfo methodInfo;
    private ExpressionUtil.CustomDelegate method;

    public BenchmarkRoute()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
        _testProcessor = new TestProcessor();

        route = new Route(_testProcessor, typeof(TestProcessor).GetMethod("Echo"), 0);
        method = ExpressionUtil.CreateMethod(_testProcessor, typeof(TestProcessor).GetMethod("Echo"));
        methodInfo = typeof(TestProcessor).GetMethod("Echo");
    }
    
    [Benchmark]
    public void CreateRoute()
    {
        var route = new Route(new TestProcessor(), typeof(TestProcessor).GetMethod("Echo")!, 0);
    }
    
    [Benchmark(Baseline=true)]
    public async Task InvokeRoute()
    {
        var result = (await route.Invoke(pack)) as Package<PbString>;
        // var result = methodInfo.Invoke(_testProcessor, new object[] { pack.Header, val });
    }
    
    // [Benchmark]
    // public void CreateExpression()
    // {
    //     var method = ExpressionUtil.CreateMethod(new TestProcessor(), typeof(TestProcessor).GetMethod("Echo"));
    // }
    
    // [Benchmark]
    // public void InvokeExpression()
    // {
    //     var result = method(pack.Header, val);
    // }
}