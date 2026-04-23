using Microsoft.CodeAnalysis;

namespace GoPlay.Analyzers.MaxConcurrency
{
    internal static class DiagnosticDescriptors
    {
        private const string Category = "GoPlay.MaxConcurrency";

        /// <summary>
        /// [MaxConcurrency(N)] 的 N 取值无意义。语义矩阵：
        /// <list type="bullet">
        /// <item><description>任意位置 N &lt; 1：运行期 <c>MaxConcurrencyAttribute</c> 构造函数会抛
        /// <c>ArgumentOutOfRangeException</c>，编译期提前 warn。</description></item>
        /// <item><description>类级 N == 1：<b>不</b>报。Processor 的默认并发由 <c>Server(int defaultConcurrency)</c>
        /// 构造时传入（可能是 1、8、基于 <c>Environment.ProcessorCount</c> 自动推导等任意值），
        /// 类级显式 <c>[MaxConcurrency(1)]</c> 表达"强制该 Processor 串行"的契约意图，
        /// 静态分析看不到 runtime 构造参数，无权武断判冗余。</description></item>
        /// <item><description>方法级 N == 1 <b>且</b> 所在类 class 级显式标了 N &lt;= 1：
        /// 方法级闸门被 Processor 总闸完全覆盖，冗余。</description></item>
        /// <item><description>方法级 N == 1 <b>且</b> 所在类 class 级 N &gt; 1（或未显式标注）：
        /// <b>不</b>报——这是合法的"单人通道"配置，表示该方法总 in-flight 严格为 1
        /// （跨 [Request]/[Notify] 与 ProcessorRef 路径共享同一把方法级 semaphore）。</description></item>
        /// </list>
        /// messageFormat 第二个占位符是具体原因短语，由 analyzer 根据场景分支传入。
        /// </summary>
        public static readonly DiagnosticDescriptor MeaninglessValue = new(
            id: "MAXCONC001",
            title: "[MaxConcurrency] 的取值无意义",
            messageFormat: "[MaxConcurrency({0})] 无意义：{1}",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "MaxConcurrency 的合理取值：N >= 1（N<1 运行期抛异常）；方法级 N 必须 <= 类级 N。" +
                         "方法级标 1 仅在 class 级显式 > 1 时才有意义（收窄为严格单人通道）；" +
                         "class 级显式 <= 1 时方法级再标 1 属于冗余。类级标 1 总是合法（显式串行契约）。");

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
        /// [MaxConcurrency] 只对"可被外部调用"的方法生效：
        /// <list type="bullet">
        /// <item><description>[Request] / [Notify]：客户端请求路径</description></item>
        /// <item><description>[ProcessorApi]：跨 Processor 调用路径（ProcessorRef）</description></item>
        /// </list>
        /// 挂在以上三者之外的普通方法上，attribute 不会被任何路径消费，属于误用。
        /// </summary>
        public static readonly DiagnosticDescriptor NonRouteMethod = new(
            id: "MAXCONC003",
            title: "[MaxConcurrency] 只允许标在 [Request]/[Notify]/[ProcessorApi] 方法上",
            messageFormat: "方法 '{0}.{1}' 标了 [MaxConcurrency] 但既不是 [Request]/[Notify]，也不是 [ProcessorApi]，attribute 不会生效",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "MaxConcurrency 作用于可被外部调用的方法：客户端路径的 [Request]/[Notify] 和跨 Processor 路径的 [ProcessorApi]；标在其他方法上属于误用。");
    }
}
