using Microsoft.CodeAnalysis;

namespace GoPlay.Analyzers.MaxConcurrency
{
    internal static class DiagnosticDescriptors
    {
        private const string Category = "GoPlay.MaxConcurrency";

        /// <summary>
        /// [MaxConcurrency(N)] 的 N &lt;= 1 无意义：
        /// - N &lt; 1 运行期会直接抛 ArgumentOutOfRangeException；
        /// - N == 1 等价于"单并发/完全串行"，方法级本就被 Processor 总闸所约束，属于冗余。
        /// </summary>
        public static readonly DiagnosticDescriptor MeaninglessValue = new(
            id: "MAXCONC001",
            title: "[MaxConcurrency] 的值 <= 1 无意义",
            messageFormat: "[MaxConcurrency({0})] 无意义：N 必须 >= 2；N=1 等同于单并发（已被 Processor 总闸约束），N<1 会在运行期抛 ArgumentOutOfRangeException",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "MaxConcurrency 的合理取值是 >= 2；取 <= 1 的场景请直接去掉该标注或收敛到 Processor 总闸。");

        /// <summary>
        /// method 级 [MaxConcurrency(N)] 的 N 大于 class 级 [MaxConcurrency(M)] 的 M：
        /// ProcessorRunner 启动期会抛 InvalidOperationException，必须提前在编译期报错。
        /// </summary>
        public static readonly DiagnosticDescriptor MethodExceedsClass = new(
            id: "MAXCONC002",
            title: "方法级 [MaxConcurrency] 超过所在类的上限",
            messageFormat: "方法 '{0}.{1}' 标 [MaxConcurrency({2})] 超过类 '{0}' 的 [MaxConcurrency({3})]；方法级 N 必须 <= 类级 N",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "方法级并发上限必须收敛到 Processor 总闸之内，否则 ProcessorRunner 启动期会直接抛异常。");

        /// <summary>
        /// [MaxConcurrency] 只在 [Request]/[Notify] 路由方法上生效。
        /// 挂在普通方法上 attribute 完全不会被路由层消费，必然是误用。
        /// </summary>
        public static readonly DiagnosticDescriptor NonRouteMethod = new(
            id: "MAXCONC003",
            title: "[MaxConcurrency] 只允许标在 [Request]/[Notify] 方法上",
            messageFormat: "方法 '{0}.{1}' 标了 [MaxConcurrency] 但不是 [Request]/[Notify] 路由方法，attribute 不会生效",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "MaxConcurrency 是路由方法级闸门，只作用于 [Request]/[Notify] 注册的处理器方法；标在其他方法上属于误用。");
    }
}
