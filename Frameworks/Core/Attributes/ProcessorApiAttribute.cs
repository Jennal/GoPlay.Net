using System;

namespace GoPlay.Core.Attributes
{
    /// <summary>
    /// 声明该方法可以被其他 Processor 通过 <c>Server.GetProcessor&lt;T&gt;().XxxMethod(...)</c> 跨 Runner 调用。
    ///
    /// 运行时：调用会被 <c>ProcessorRef&lt;T&gt;</c> 转投递到目标 Processor 的 Runner 邮箱串行执行，
    /// 调用方和目标方各自的 Runner 互不阻塞，天然消除跨 Processor 的数据竞争。
    ///
    /// 编译期：Source Generator 根据此标签为每个方法生成对应的 <c>ProcessorRef&lt;T&gt;</c> 扩展方法，
    /// 业务代码几乎不需要修改——<c>GetProcessor</c> 返回的 Ref 会直接暴露同名同签名的扩展。
    ///
    /// 使用约束：
    /// - 必须标在 Processor 的 public 实例方法上（非 static、非 private/internal）。
    /// - 参数不可包含 <c>ref</c> / <c>out</c> / <c>in</c>（跨 mailbox 传引用无意义）。
    /// - 推荐返回 <c>Task</c> 或 <c>Task&lt;T&gt;</c>；若标 <c>Fire=true</c>，则强制走 Notify 语义（fire-and-forget），
    ///   要求返回 <c>Task</c> 或 <c>void</c>。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ProcessorApiAttribute : Attribute
    {
        /// <summary>
        /// 强制走 Notify（fire-and-forget）。默认为 false：
        /// 若方法返回 <c>Task&lt;T&gt;</c> 或 <c>Task</c>，生成 <c>Request</c> 扩展（可 await 结果 / 异常）；
        /// 若方法返回 <c>void</c>，生成 <c>Notify</c> 扩展。
        ///
        /// 设为 true 时，即使返回 <c>Task</c> 也不等待完成，异常就地 <c>OnErrorEvent</c> 上报。
        /// </summary>
        public bool Fire { get; set; }
    }
}
