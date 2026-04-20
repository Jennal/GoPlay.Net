using System;

namespace GoPlay.Core.Attributes
{
    /// <summary>
    /// 限制 Processor 或路由方法的最大并发 in-flight 数。
    ///
    /// 挂在 class 上：覆盖 Server 构造时给的 defaultConcurrency，作为 Processor 总闸。
    /// 挂在 method（[Request]/[Notify]）上：方法级闸门，N 必须 ≤ Processor 总闸，
    /// 不标即不限流（仅受 Processor 总闸约束）。
    ///
    /// 校验：
    /// - N &gt;= 1（attribute 构造期校验）
    /// - method 级 N &lt;= processor 解析后的并发数（ProcessorRunner 启动期校验，
    ///   失败抛 InvalidOperationException 并指出具体方法）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class MaxConcurrencyAttribute : Attribute
    {
        public int Value { get; }

        public MaxConcurrencyAttribute(int value)
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"[MaxConcurrency({value})] 非法：必须 >= 1");
            }
            Value = value;
        }
    }
}
