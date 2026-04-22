using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace GoPlay.Analyzers.ProcessorIsolation
{
    /// <summary>
    /// 检测当前 Processor 子类中跨 Processor 的 GetProcessorUnsafe&lt;X&gt;().Xxx 调用：
    /// - PROCREF010：Xxx 是方法、未标 [ProcessorApi]
    /// - PROCREF011：Xxx 是方法、已标 [ProcessorApi]（推荐改用 GetProcessor）
    /// - PROCREF012：Xxx 是属性或字段访问
    /// 当前 class != X 才算跨 Processor；否则默认 skip。
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ProcessorIsolationAnalyzer : DiagnosticAnalyzer
    {
        // ProcessorBase 的 FQN
        private const string ProcessorBaseMetadataName = "GoPlay.Core.Processors.ProcessorBase";
        // [ProcessorApi] 特性的 FQN
        private const string ProcessorApiAttributeMetadataName = "GoPlay.Core.Attributes.ProcessorApiAttribute";
        // 逃生舱方法名（Server.GetProcessorUnsafe / ServiceProvider.GetProcessorUnsafe 都匹配）
        private const string GetProcessorUnsafeMethodName = "GetProcessorUnsafe";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(
                DiagnosticDescriptors.CrossProcessorUnmarkedMethod,
                DiagnosticDescriptors.CrossProcessorUseGetProcessor,
                DiagnosticDescriptors.CrossProcessorFieldOrPropertyAccess);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationStart =>
            {
                var processorBaseSymbol = compilationStart.Compilation.GetTypeByMetadataName(ProcessorBaseMetadataName);
                if (processorBaseSymbol is null)
                {
                    // 当前编译单元看不到 GoPlay.Core.Processors.ProcessorBase，analyzer 不参与
                    return;
                }

                var processorApiAttrSymbol = compilationStart.Compilation.GetTypeByMetadataName(ProcessorApiAttributeMetadataName);

                compilationStart.RegisterOperationAction(
                    ctx => AnalyzeInvocation(ctx, processorBaseSymbol, processorApiAttrSymbol),
                    OperationKind.Invocation);

                compilationStart.RegisterOperationAction(
                    ctx => AnalyzeMemberReference(ctx, processorBaseSymbol, isProperty: true),
                    OperationKind.PropertyReference);

                compilationStart.RegisterOperationAction(
                    ctx => AnalyzeMemberReference(ctx, processorBaseSymbol, isProperty: false),
                    OperationKind.FieldReference);
            });
        }

        private static void AnalyzeInvocation(
            OperationAnalysisContext context,
            INamedTypeSymbol processorBaseSymbol,
            INamedTypeSymbol? processorApiAttrSymbol)
        {
            var invocation = (IInvocationOperation)context.Operation;

            // 本次分析的是 M 调用，且要求 receiver 是另一个 Invocation（即 GetProcessorUnsafe<X>()）
            if (invocation.Instance is not IInvocationOperation receiverInvocation)
                return;

            var receiverMethod = receiverInvocation.TargetMethod;
            if (receiverMethod.Name != GetProcessorUnsafeMethodName || receiverMethod.TypeArguments.Length != 1)
                return;

            // 目标 Processor 类型（来自 <X>）
            if (receiverMethod.TypeArguments[0] is not INamedTypeSymbol targetProcessorType)
                return;

            // 确认调用发生在 Processor 子类中，且跨 Processor
            if (!IsCrossProcessorContext(context, processorBaseSymbol, targetProcessorType, out var currentProcessorType))
                return;

            var targetMethod = invocation.TargetMethod;
            var hasProcessorApi = processorApiAttrSymbol is not null
                && targetMethod.GetAttributes()
                    .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, processorApiAttrSymbol));

            var location = invocation.Syntax.GetLocation();

            if (hasProcessorApi)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.CrossProcessorUseGetProcessor,
                    location,
                    targetProcessorType.Name,
                    targetMethod.Name));
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.CrossProcessorUnmarkedMethod,
                    location,
                    currentProcessorType.Name,
                    targetProcessorType.Name,
                    targetMethod.Name));
            }
        }

        private static void AnalyzeMemberReference(
            OperationAnalysisContext context,
            INamedTypeSymbol processorBaseSymbol,
            bool isProperty)
        {
            var memberRef = (IMemberReferenceOperation)context.Operation;

            if (memberRef.Instance is not IInvocationOperation receiverInvocation)
                return;

            var receiverMethod = receiverInvocation.TargetMethod;
            if (receiverMethod.Name != GetProcessorUnsafeMethodName || receiverMethod.TypeArguments.Length != 1)
                return;

            if (receiverMethod.TypeArguments[0] is not INamedTypeSymbol targetProcessorType)
                return;

            if (!IsCrossProcessorContext(context, processorBaseSymbol, targetProcessorType, out var currentProcessorType))
                return;

            var memberKindLabel = isProperty ? "属性" : "字段";

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.CrossProcessorFieldOrPropertyAccess,
                memberRef.Syntax.GetLocation(),
                currentProcessorType.Name,
                targetProcessorType.Name,
                memberRef.Member.Name,
                memberKindLabel));
        }

        /// <summary>
        /// 检查当前 Operation 所在的类型是否继承自 ProcessorBase，且与目标 Processor 不同。
        /// </summary>
        private static bool IsCrossProcessorContext(
            OperationAnalysisContext context,
            INamedTypeSymbol processorBaseSymbol,
            INamedTypeSymbol targetProcessorType,
            out INamedTypeSymbol currentProcessorType)
        {
            currentProcessorType = null!;

            var containingSymbol = context.ContainingSymbol;
            var containingType = containingSymbol?.ContainingType;
            if (containingType is null)
                return false;

            if (!InheritsFrom(containingType, processorBaseSymbol))
                return false;

            // 确认目标类型也是 Processor 子类；否则不是「跨 Processor」而可能是普通泛型用法
            if (!InheritsFrom(targetProcessorType, processorBaseSymbol))
                return false;

            // 同一 Processor 内部直取不算跨 Processor；穿透 partial（同一 INamedTypeSymbol 实例）
            if (SymbolEqualityComparer.Default.Equals(containingType.OriginalDefinition, targetProcessorType.OriginalDefinition))
                return false;

            currentProcessorType = containingType;
            return true;
        }

        private static bool InheritsFrom(INamedTypeSymbol? type, INamedTypeSymbol target)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, target))
                    return true;
            }
            return false;
        }
    }
}
