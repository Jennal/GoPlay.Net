using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GoPlay.Analyzers.MaxConcurrency
{
    /// <summary>
    /// 针对 [MaxConcurrency] 的编译期校验：
    /// - MAXCONC001 (Warning)：N &lt;= 1 无意义（N &lt; 1 运行期会抛；N == 1 冗余）
    /// - MAXCONC002 (Error)  ：method 级 N 超过所在类（含继承链）的 class 级 N
    /// - MAXCONC003 (Error)  ：method 标了 [MaxConcurrency] 但既不是 [Request]/[Notify] 也不是 [ProcessorApi]
    ///
    /// 设计说明：
    /// - 类级 [MaxConcurrency] 通过走 BaseType 链找「最近一级」显式标注；找不到则不做 MAXCONC002 校验（运行期 fallback 到 Server.DefaultConcurrency，编译期无法得知）。
    /// - 诊断位置优先定位到 attribute 语法节点本身，减少 IDE 跳转成本。
    /// - MAXCONC003 判定：方法级 [MaxConcurrency] 必须挂在"可被外部调用"的方法上才有意义。
    ///   当前认定的合法身份集：[Request] / [Notify] / [ProcessorApi]。三者之一即可——
    ///     * [Request]/[Notify]：客户端请求路径，ProcessorRunner 依据 Route 建方法级 sem。
    ///     * [ProcessorApi]：跨 Processor 调用路径，ProcessorRunner 额外扫描这类方法建方法级 sem，
    ///       ProcessorRef.Request/Notify 投递闭包时带同样算法生成的 routeKey 命中限流。
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MaxConcurrencyAnalyzer : DiagnosticAnalyzer
    {
        private const string MaxConcurrencyAttributeMetadataName = "GoPlay.Core.Attributes.MaxConcurrencyAttribute";
        private const string RequestAttributeMetadataName        = "GoPlay.Core.Attributes.RequestAttribute";
        private const string NotifyAttributeMetadataName         = "GoPlay.Core.Attributes.NotifyAttribute";
        private const string ProcessorApiAttributeMetadataName   = "GoPlay.Core.Attributes.ProcessorApiAttribute";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                DiagnosticDescriptors.MeaninglessValue,
                DiagnosticDescriptors.MethodExceedsClass,
                DiagnosticDescriptors.NonRouteMethod);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationStart =>
            {
                var maxConcAttr = compilationStart.Compilation.GetTypeByMetadataName(MaxConcurrencyAttributeMetadataName);
                if (maxConcAttr is null)
                {
                    // 当前编译单元看不到 MaxConcurrencyAttribute，analyzer 不参与
                    return;
                }

                var requestAttr      = compilationStart.Compilation.GetTypeByMetadataName(RequestAttributeMetadataName);
                var notifyAttr       = compilationStart.Compilation.GetTypeByMetadataName(NotifyAttributeMetadataName);
                var processorApiAttr = compilationStart.Compilation.GetTypeByMetadataName(ProcessorApiAttributeMetadataName);

                compilationStart.RegisterSymbolAction(
                    ctx => AnalyzeNamedType(ctx, maxConcAttr),
                    SymbolKind.NamedType);

                compilationStart.RegisterSymbolAction(
                    ctx => AnalyzeMethod(ctx, maxConcAttr, requestAttr, notifyAttr, processorApiAttr),
                    SymbolKind.Method);
            });
        }

        private static void AnalyzeNamedType(
            SymbolAnalysisContext context,
            INamedTypeSymbol maxConcAttr)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            var attr = FindAttribute(type.GetAttributes(), maxConcAttr);
            if (attr is null) return;
            if (!TryGetValue(attr, out var value)) return;

            // 类级判定：
            // - N < 1：运行期 MaxConcurrencyAttribute 构造函数直接抛 ArgumentOutOfRangeException
            // - N == 1：**不**报冗余。Processor 的"默认"并发由 Server(int defaultConcurrency)
            //   构造时传入（见 ProcessorRunner.ResolveMaxConcurrency：attr 优先，无 attr 才用
            //   Server.DefaultConcurrency），并非硬编码 1。类级显式标 [MaxConcurrency(1)]
            //   表达的是"强制该 Processor 串行、不随 Server 默认并发漂移"的契约意图，
            //   无论 Server 默认值是多少都合法、且与"不标注"在 Server 默认 > 1 时语义**不同**，
            //   静态分析看不到 runtime 构造参数，不应在此处武断判冗余。
            if (value < 1)
            {
                ReportAtAttribute(context, attr, DiagnosticDescriptors.MeaninglessValue,
                    value, "N<1 会在运行期抛 ArgumentOutOfRangeException");
            }
        }

        private static void AnalyzeMethod(
            SymbolAnalysisContext context,
            INamedTypeSymbol maxConcAttr,
            INamedTypeSymbol? requestAttr,
            INamedTypeSymbol? notifyAttr,
            INamedTypeSymbol? processorApiAttr)
        {
            var method = (IMethodSymbol)context.Symbol;

            // 只关心用户显式定义的方法，过滤掉 getter/setter/operator/构造函数等合成成员
            if (method.MethodKind != MethodKind.Ordinary) return;

            var methodAttrs = method.GetAttributes();
            var mc = FindAttribute(methodAttrs, maxConcAttr);
            if (mc is null) return;

            if (!TryGetValue(mc, out var methodValue)) return;

            var containingType = method.ContainingType;
            var containingTypeName = containingType?.Name ?? "<global>";

            // 先把 class 级解析出来，后面 MAXCONC001 对 N==1 的判定需要它。
            // 没显式标注时按"未知"处理——运行期回退到 Server.DefaultConcurrency，编译期无法知道，
            // 此时对"方法级 N==1 是否冗余"采取保守策略：不报（避免误杀）。
            int classValue = 0;
            bool classExplicit = containingType != null
                && TryGetClassMaxConcurrency(containingType, maxConcAttr, out classValue);

            // MAXCONC001：方法级取值无意义的判定收窄
            // - N < 1：运行期必抛（attribute 构造函数直接 ArgumentOutOfRangeException）
            // - N == 1 且 class 级也 <= 1：方法级闸门被 Processor 总闸完全覆盖，冗余
            // - N == 1 且 class 级 > 1：不报。这是合法的"单人通道"配置——该方法的总 in-flight
            //   （跨客户端 [Request] 和 ProcessorRef 调用两条路径）被严格锁为 1，class 级仍然
            //   允许其他方法并发跑。这是方法级 [MaxConcurrency(1)] 唯一有意义的用途。
            if (methodValue < 1)
            {
                ReportAtAttribute(context, mc, DiagnosticDescriptors.MeaninglessValue,
                    methodValue, "N<1 会在运行期抛 ArgumentOutOfRangeException");
            }
            else if (methodValue == 1 && classExplicit && classValue <= 1)
            {
                ReportAtAttribute(context, mc, DiagnosticDescriptors.MeaninglessValue,
                    methodValue,
                    $"class 级 [MaxConcurrency({classValue})] 已经是串行，方法级再标 1 冗余，请移除");
            }

            // MAXCONC003: 方法必须是 [Request]/[Notify]/[ProcessorApi] 之一才允许标 [MaxConcurrency]
            // 注意：即使 MAXCONC001 / MAXCONC003 同时触发，也都要报出来，让用户一次性看到全部问题。
            var isLimitable = HasRouteAttribute(method, requestAttr)
                              || HasRouteAttribute(method, notifyAttr)
                              || HasRouteAttribute(method, processorApiAttr);
            if (!isLimitable)
            {
                ReportAtAttribute(
                    context,
                    mc,
                    DiagnosticDescriptors.NonRouteMethod,
                    containingTypeName,
                    method.Name);
                // 非路由方法根本用不到 class/method 对比，直接返回
                return;
            }

            // MAXCONC002: method 级 N 超过 class 级 N
            if (!classExplicit) return;

            if (methodValue > classValue)
            {
                ReportAtAttribute(
                    context,
                    mc,
                    DiagnosticDescriptors.MethodExceedsClass,
                    containingTypeName,
                    method.Name,
                    methodValue,
                    classValue);
            }
        }

        /// <summary>
        /// 沿 BaseType 链向上找「最近一级」显式标了 [MaxConcurrency] 的类，返回其 N 值。
        /// </summary>
        private static bool TryGetClassMaxConcurrency(
            INamedTypeSymbol type,
            INamedTypeSymbol maxConcAttr,
            out int value)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                var attr = FindAttribute(current.GetAttributes(), maxConcAttr);
                if (attr is null) continue;
                if (TryGetValue(attr, out value)) return true;
            }
            value = 0;
            return false;
        }

        private static bool HasRouteAttribute(IMethodSymbol method, INamedTypeSymbol? routeAttr)
        {
            if (routeAttr is null) return false;

            // attribute 在类型上声明为 Inherited = true，需要沿 OverriddenMethod 链找
            for (var current = method; current is not null; current = current.OverriddenMethod)
            {
                foreach (var attr in current.GetAttributes())
                {
                    if (IsOrInheritsFrom(attr.AttributeClass, routeAttr))
                        return true;
                }
            }
            return false;
        }

        private static AttributeData? FindAttribute(ImmutableArray<AttributeData> attrs, INamedTypeSymbol target)
        {
            foreach (var attr in attrs)
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, target))
                    return attr;
            }
            return null;
        }

        private static bool IsOrInheritsFrom(INamedTypeSymbol? type, INamedTypeSymbol target)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, target.OriginalDefinition))
                    return true;
            }
            return false;
        }

        private static bool TryGetValue(AttributeData attr, out int value)
        {
            value = 0;
            var args = attr.ConstructorArguments;
            if (args.Length < 1) return false;
            var arg = args[0];
            if (arg.Kind == TypedConstantKind.Error) return false;
            if (arg.Value is int i)
            {
                value = i;
                return true;
            }
            return false;
        }

        private static void ReportAtAttribute(
            SymbolAnalysisContext context,
            AttributeData attr,
            DiagnosticDescriptor descriptor,
            params object[] messageArgs)
        {
            // 优先报在 attribute 语法节点上；取不到语法时退化到 symbol 的第一个 location
            var syntaxRef = attr.ApplicationSyntaxReference;
            var location = syntaxRef is not null
                ? Location.Create(syntaxRef.SyntaxTree, syntaxRef.Span)
                : context.Symbol.Locations.FirstOrDefault() ?? Location.None;

            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
        }
    }
}
