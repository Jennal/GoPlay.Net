using Microsoft.CodeAnalysis;

namespace GoPlay.Generators.ProcessorRef
{
    /// <summary>
    /// Diagnostics emitted by <see cref="ProcessorRefGenerator"/>.
    /// 编号段位：PROCREF001-099。
    /// </summary>
    internal static class DiagnosticDescriptors
    {
        private const string Category = "GoPlay.ProcessorRef";

        /// <summary>
        /// [ProcessorApi] 标在非 public 实例方法或 static 方法上。
        /// </summary>
        public static readonly DiagnosticDescriptor NonPublicOrStatic = new DiagnosticDescriptor(
            id: "PROCREF001",
            title: "[ProcessorApi] 方法必须是 public 实例方法",
            messageFormat: "方法 '{0}' 标了 [ProcessorApi] 但不是 public 实例方法；只有 public 非 static 方法可跨 Processor 暴露",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// 方法带有 ref/out/in 参数：跨 mailbox 投递引用没有语义，且闭包无法捕获按引用参数。
        /// </summary>
        public static readonly DiagnosticDescriptor HasByRefParameter = new DiagnosticDescriptor(
            id: "PROCREF002",
            title: "[ProcessorApi] 方法不允许 ref/out/in 参数",
            messageFormat: "方法 '{0}' 含 ref/out/in 参数 '{1}'，跨 Runner 投递无法保持引用语义；请改为按值传递或包装成 DTO",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// [ProcessorApi(Fire=true)] 但返回类型不是 Task / ValueTask / void。
        /// </summary>
        public static readonly DiagnosticDescriptor FireOnNonTaskReturn = new DiagnosticDescriptor(
            id: "PROCREF003",
            title: "[ProcessorApi(Fire=true)] 要求返回 void / Task / ValueTask",
            messageFormat: "方法 '{0}' 标了 Fire=true 但返回类型 '{1}' 非 void/Task/ValueTask；带返回值的方法不能 fire-and-forget",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// 方法名与 <c>ProcessorRef&lt;T&gt;</c> 的实例方法（Request / Notify / 等）冲突，
        /// 扩展方法会被实例方法屏蔽，业务调用方表面上能编译但行为不对。
        /// </summary>
        public static readonly DiagnosticDescriptor NameConflict = new DiagnosticDescriptor(
            id: "PROCREF004",
            title: "[ProcessorApi] 方法名与 ProcessorRef<T> 成员冲突",
            messageFormat: "方法 '{0}' 与 ProcessorRef<T> 内置成员同名，生成的扩展会被屏蔽；请重命名该方法",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
