using Microsoft.CodeAnalysis;

namespace GoPlay.Analyzers.ProcessorIsolation
{
    internal static class DiagnosticDescriptors
    {
        private const string Category = "GoPlay.Processor";

        /// <summary>
        /// 跨 Processor 调用未标注 [ProcessorApi] 的方法，线程安全风险高。
        /// </summary>
        public static readonly DiagnosticDescriptor CrossProcessorUnmarkedMethod = new(
            id: "PROCREF010",
            title: "跨 Processor 调用未标注 [ProcessorApi] 的方法",
            messageFormat: "在 Processor '{0}' 里跨 Processor 调用 '{1}.{2}'，目标方法未标 [ProcessorApi]；若会修改状态请加 [ProcessorApi] 并改用 GetProcessor<{1}>().{2}(...)；若为纯读数据可在 .editorconfig 里把 PROCREF010 降级/关闭",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "跨 Processor 直接通过 GetProcessorUnsafe<X>() 调用 X 的实例方法会绕过目标 Processor 的 mailbox，可能引入跨线程竞争。");

        /// <summary>
        /// 方法已标 [ProcessorApi]，但调用方仍在走 GetProcessorUnsafe；建议改走 GetProcessor 走 mailbox。
        /// </summary>
        public static readonly DiagnosticDescriptor CrossProcessorUseGetProcessor = new(
            id: "PROCREF011",
            title: "已标 [ProcessorApi]，推荐改用 GetProcessor 走 mailbox",
            messageFormat: "'{0}.{1}' 已通过 [ProcessorApi] 暴露为跨 Processor 安全调用，此处请改用 Server.GetProcessor<{0}>().{1}(...) 走目标 Runner 的 mailbox",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "目标方法已提供 ProcessorRef 扩展，继续使用 GetProcessorUnsafe 会失去线程隔离保障。");

        /// <summary>
        /// 跨 Processor 直接访问字段/属性，无法通过 mailbox，线程不安全。
        /// </summary>
        public static readonly DiagnosticDescriptor CrossProcessorFieldOrPropertyAccess = new(
            id: "PROCREF012",
            title: "跨 Processor 直接访问字段/属性",
            messageFormat: "在 Processor '{0}' 里跨 Processor 访问 '{1}.{2}'（{3}）无法走 mailbox；请包成 [ProcessorApi] 的 getter/setter 方法再走 Server.GetProcessor<{1}>()",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "字段与属性访问不会进入目标 Processor 的 mailbox，存在跨线程数据竞争。");
    }
}
