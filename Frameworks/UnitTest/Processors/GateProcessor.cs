using System;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;

namespace UnitTest.Processors;

/// <summary>
/// 测试专用门闩处理器：用于确定性验证死客户端排队包丢弃(P1)与每客户端取消令牌(P2)。
/// - <see cref="ExecCount"/>：handler 真正进入执行的次数。被闸门在出队点丢弃的包不会进 handler，计数不增。
/// - <see cref="Gate"/>：handler await 的门闩，由测试 <c>TrySetResult</c> 释放。
/// - <see cref="Started"/>：handler 进入并捕获 token 后置位，供测试等待"已进入"。
/// - <see cref="CapturedToken"/>：handler 进入时抓取的 ambient <c>Server.CurrentClientToken</c>。
/// 每个用例开始前调用 <see cref="Reset"/>。
/// </summary>
[Processor("gate")]
class GateProcessor : ProcessorBase
{
    public static int ExecCount;
    public static TaskCompletionSource<bool> Gate = NewTcs();
    public static TaskCompletionSource<bool> Started = NewTcs();
    public static CancellationToken CapturedToken;

    public override string[] Pushes => Array.Empty<string>();

    private static TaskCompletionSource<bool> NewTcs()
        => new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

    public static void Reset()
    {
        Interlocked.Exchange(ref ExecCount, 0);
        Gate = NewTcs();
        Started = NewTcs();
        CapturedToken = default;
    }

    public static void Release() => Gate.TrySetResult(true);

    [Request("block")]
    public async Task<PbString> Block(Header header, PbString str)
    {
        Interlocked.Increment(ref ExecCount);
        // 用类型限定访问静态 ambient token（实例属性 Server 会遮蔽类型名 Server）。
        CapturedToken = GoPlay.Server.CurrentClientToken;
        Started.TrySetResult(true);

        await Gate.Task.ConfigureAwait(false);

        return new PbString
        {
            Value = $"blocked: {str.Value}"
        };
    }
}
