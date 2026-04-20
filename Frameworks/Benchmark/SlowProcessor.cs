using System;
using System.Threading.Tasks;
using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;

namespace GoPlay.Benchmarks;

/// <summary>
/// 用于吞吐对比的慢路由 processor 集合。
///
/// 设计理念：每个不同并发度对应一个独立 class，benchmark 切场景时切 class，
/// 不依赖任何运行时静态字段——这样 [MaxConcurrency(N)] 才是真正的"启动期不可变"配置。
///
/// - SlowSerialProcessor      ：不标 attribute，走 Server.DefaultConcurrency（benchmark 默认 1）
/// - SlowPipelineProcessor    ：[MaxConcurrency(64)]，演示流水线
/// - MethodLimitedProcessor   ：[MaxConcurrency(64)] + 方法级 [MaxConcurrency(8)]，演示方法级闸门
/// </summary>
public abstract class SlowProcessorBase : ProcessorBase
{
    public override string[] Pushes => Array.Empty<string>();

    [Request("echo")]
    public PbString Echo(Header header, PbString str) => new() { Value = str.Value };

    [Request("slow")]
    public async Task<PbString> Slow(Header header, PbString str)
    {
        if (int.TryParse(str.Value, out var ms) && ms > 0)
        {
            await Task.Delay(ms).ConfigureAwait(false);
        }
        return new PbString { Value = str.Value };
    }
}

/// <summary>
/// 不标 [MaxConcurrency]：依赖 Server.DefaultConcurrency。
/// Benchmark 里 new Server&lt;...&gt;(defaultConcurrency: 1) 即对应旧串行架构基线。
/// </summary>
[Processor("slow")]
public class SlowSerialProcessor : SlowProcessorBase
{
}

/// <summary>
/// 显式 [MaxConcurrency(64)]：覆盖 Server 默认，演示流水线能力。
/// </summary>
[Processor("slow")]
[MaxConcurrency(64)]
public class SlowPipelineProcessor : SlowProcessorBase
{
}

/// <summary>
/// Method 级 [MaxConcurrency] 演示：
/// Processor 64 路并发，但 slow8 路由被压在同时在飞数 ≤ 8。
/// </summary>
[Processor("limited")]
[MaxConcurrency(64)]
public class MethodLimitedProcessor : ProcessorBase
{
    public override string[] Pushes => Array.Empty<string>();

    [Request("slow")]
    public async Task<PbString> Slow(Header header, PbString str)
    {
        if (int.TryParse(str.Value, out var ms) && ms > 0)
        {
            await Task.Delay(ms).ConfigureAwait(false);
        }
        return new PbString { Value = str.Value };
    }

    [Request("slow8")]
    [MaxConcurrency(8)]
    public async Task<PbString> Slow8(Header header, PbString str)
    {
        if (int.TryParse(str.Value, out var ms) && ms > 0)
        {
            await Task.Delay(ms).ConfigureAwait(false);
        }
        return new PbString { Value = str.Value };
    }
}
