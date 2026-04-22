using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GoPlay.Generators.ProcessorRef
{
    /// <summary>
    /// 扫描所有标了 <c>[GoPlay.Core.Attributes.ProcessorApi]</c> 的 Processor 方法，
    /// 为每个外层 <c>ProcessorBase</c> 派生类生成一个 <c>static</c> 扩展类，
    /// 里面对 <c>ProcessorRef&lt;TProcessor&gt;</c> 暴露同名同签名的扩展方法；
    /// 扩展方法内部把调用翻译成 <c>Request&lt;R&gt;</c> / <c>Request</c> / <c>Notify</c>。
    ///
    /// 业务调用链：
    /// <code>
    /// Server.GetProcessor&lt;Foo&gt;().Bar(x)   // 原型不变
    /// => FooRefExtensions.Bar(ProcessorRef&lt;Foo&gt; ref, x)   // 编译期解析到这个扩展
    /// => ref.Request(__p =&gt; __p.Bar(x))    // 运行期走 Runner.Post 串行执行
    /// </code>
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class ProcessorRefGenerator : IIncrementalGenerator
    {
        private const string ProcessorApiAttributeFullName = "GoPlay.Core.Attributes.ProcessorApiAttribute";
        private const string ProcessorBaseFullName = "GoPlay.Core.Processors.ProcessorBase";
        private const string GeneratedNamespace = "GoPlay.Generated.ProcessorRefs";

        /// <summary>
        /// <c>ProcessorRef&lt;T&gt;</c> 自身暴露的实例成员集合，用于 PROCREF004 诊断。
        /// </summary>
        private static readonly ImmutableHashSet<string> ProcessorRefReservedNames =
            ImmutableHashSet.Create(
                StringComparer.Ordinal,
                "Request", "Notify", "IsValid", "Equals", "GetHashCode", "ToString", "GetType");

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 以 attribute 名过滤候选方法：Roslyn 在索引层就做过滤，比自己扫 SyntaxNode 快得多
            var methods = context.SyntaxProvider.ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: ProcessorApiAttributeFullName,
                predicate: static (syntax, _) => syntax is MethodDeclarationSyntax,
                transform: static (ctx, ct) => ExtractMethodCandidate(ctx, ct));

            // 扔掉 null（Transform 返回 null 表示不是合法的 Processor.Xxx 组合）
            var validMethods = methods.Where(static m => m is not null)!.Select(static (m, _) => m!);

            var collected = validMethods.Collect();

            context.RegisterSourceOutput(collected, static (spc, allMethods) =>
            {
                if (allMethods.IsDefaultOrEmpty) return;

                // 先把所有诊断发出去（就算后续某组不生成代码，诊断也应该出来）
                foreach (var m in allMethods)
                {
                    foreach (var diag in m.Diagnostics) spc.ReportDiagnostic(diag);
                }

                // 按 Processor 类型分组，每组生成一个文件
                var groups = allMethods
                    .Where(m => !m.HasBlockingError)
                    .GroupBy(m => (m.ProcessorNamespace, m.ProcessorTypeName, m.ProcessorFullyQualifiedName));

                foreach (var group in groups)
                {
                    var (ns, typeName, fqn) = group.Key;
                    var source = Emitter.EmitExtensionClass(ns, typeName, fqn, group.ToList());
                    var hintName = $"{SanitizeFileName(ns)}_{typeName}RefExtensions.g.cs";
                    spc.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
                }
            });
        }

        private static MethodCandidate? ExtractMethodCandidate(
            GeneratorAttributeSyntaxContext ctx,
            System.Threading.CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (ctx.TargetSymbol is not IMethodSymbol methodSymbol) return null;
            if (methodSymbol.ContainingType is not INamedTypeSymbol containingType) return null;

            // 容器必须继承自 ProcessorBase（间接也算）
            if (!IsProcessorSubtype(containingType)) return null;

            // 抽象类不生成（不会被实例化）
            if (containingType.IsAbstract) return null;

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            bool hasBlockingError = false;

            var methodLocation = methodSymbol.Locations.FirstOrDefault() ?? Location.None;

            // 读 Fire 参数
            bool fire = false;
            foreach (var attr in methodSymbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() != ProcessorApiAttributeFullName) continue;
                foreach (var named in attr.NamedArguments)
                {
                    if (named.Key == "Fire" && named.Value.Value is bool b) fire = b;
                }
            }

            // PROCREF001: 必须 public、非 static
            if (methodSymbol.DeclaredAccessibility != Accessibility.Public || methodSymbol.IsStatic)
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.NonPublicOrStatic,
                    methodLocation,
                    methodSymbol.Name));
                hasBlockingError = true;
            }

            // PROCREF002: ref/out/in 参数
            foreach (var p in methodSymbol.Parameters)
            {
                if (p.RefKind == RefKind.None) continue;
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.HasByRefParameter,
                    methodLocation,
                    methodSymbol.Name, p.Name));
                hasBlockingError = true;
            }

            // 分析返回类型
            var returnKind = ClassifyReturnType(methodSymbol.ReturnType, out var returnResultType);

            // PROCREF003: Fire=true 但带返回值
            if (fire && returnKind is ReturnKind.TaskOfT or ReturnKind.ValueTaskOfT or ReturnKind.Other)
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.FireOnNonTaskReturn,
                    methodLocation,
                    methodSymbol.Name, methodSymbol.ReturnType.ToDisplayString()));
                hasBlockingError = true;
            }

            // 目前不支持方法级泛型：fn 闭包里的 T 无法从扩展方法推断到目标实例方法
            // （可以未来再加：在扩展方法上也加对应泛型参数）——这里暂拒，走 Notify/Request 的闭包仍然能显式类型
            if (methodSymbol.IsGenericMethod)
            {
                // 这条不单列诊断编号，直接跳过生成（不视为 blocking error——调用方仍可用 Unsafe 路径）
                hasBlockingError = true;
            }

            // PROCREF004: 和 ProcessorRef<T> 内置成员重名（warning，不阻止生成）
            if (ProcessorRefReservedNames.Contains(methodSymbol.Name))
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.NameConflict,
                    methodLocation,
                    methodSymbol.Name));
                // 不置 hasBlockingError：仍旧生成代码，但扩展会被实例方法屏蔽，业务要注意
            }

            // 参数列表
            var paramBuilder = ImmutableArray.CreateBuilder<ParameterInfo>(methodSymbol.Parameters.Length);
            foreach (var p in methodSymbol.Parameters)
            {
                paramBuilder.Add(new ParameterInfo(
                    Name: p.Name,
                    TypeDisplay: p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                        .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                            | SymbolDisplayMiscellaneousOptions.UseSpecialTypes
                            | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)),
                    IsParams: p.IsParams,
                    HasExplicitDefault: p.HasExplicitDefaultValue,
                    DefaultLiteral: p.HasExplicitDefaultValue ? FormatDefault(p) : null));
            }

            var containingNs = containingType.ContainingNamespace?.IsGlobalNamespace == true
                ? string.Empty
                : containingType.ContainingNamespace!.ToDisplayString();

            return new MethodCandidate(
                MethodName: methodSymbol.Name,
                ProcessorNamespace: containingNs,
                ProcessorTypeName: containingType.Name,
                ProcessorFullyQualifiedName: containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ReturnKind: returnKind,
                ReturnDisplay: methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                TaskResultDisplay: returnResultType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Fire: fire,
                Parameters: paramBuilder.ToImmutable(),
                Diagnostics: diagnostics.ToImmutable(),
                HasBlockingError: hasBlockingError);
        }

        private static bool IsProcessorSubtype(INamedTypeSymbol type)
        {
            for (var cur = type.BaseType; cur is not null; cur = cur.BaseType)
            {
                if (cur.ToDisplayString() == ProcessorBaseFullName) return true;
            }
            return false;
        }

        private static ReturnKind ClassifyReturnType(ITypeSymbol type, out ITypeSymbol? resultType)
        {
            resultType = null;
            if (type.SpecialType == SpecialType.System_Void) return ReturnKind.Void;

            if (type is INamedTypeSymbol named)
            {
                var fullName = named.ConstructedFrom?.ToDisplayString() ?? named.ToDisplayString();
                switch (fullName)
                {
                    case "System.Threading.Tasks.Task":
                        return ReturnKind.Task;
                    case "System.Threading.Tasks.Task<TResult>":
                        resultType = named.TypeArguments.Length > 0 ? named.TypeArguments[0] : null;
                        return ReturnKind.TaskOfT;
                    case "System.Threading.Tasks.ValueTask":
                        return ReturnKind.ValueTask;
                    case "System.Threading.Tasks.ValueTask<TResult>":
                        resultType = named.TypeArguments.Length > 0 ? named.TypeArguments[0] : null;
                        return ReturnKind.ValueTaskOfT;
                }
            }

            return ReturnKind.Other;
        }

        private static string? FormatDefault(IParameterSymbol p)
        {
            var v = p.ExplicitDefaultValue;
            if (v is null)
            {
                // null literal 或 default(T)：用 default 最省心，编译器会按声明类型推断
                return "default";
            }
            return v switch
            {
                string s => SymbolDisplay.FormatLiteral(s, quote: true),
                char c => SymbolDisplay.FormatLiteral(c, quote: true),
                bool b => b ? "true" : "false",
                _ => System.Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture) ?? "default",
            };
        }

        private static string SanitizeFileName(string ns)
        {
            if (string.IsNullOrEmpty(ns)) return "global";
            var sb = new StringBuilder(ns.Length);
            foreach (var c in ns)
            {
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            }
            return sb.ToString();
        }
    }

    internal enum ReturnKind
    {
        Void,
        Task,
        TaskOfT,
        ValueTask,
        ValueTaskOfT,
        Other,
    }

    internal sealed record ParameterInfo(
        string Name,
        string TypeDisplay,
        bool IsParams,
        bool HasExplicitDefault,
        string? DefaultLiteral);

    internal sealed record MethodCandidate(
        string MethodName,
        string ProcessorNamespace,
        string ProcessorTypeName,
        string ProcessorFullyQualifiedName,
        ReturnKind ReturnKind,
        string ReturnDisplay,
        string? TaskResultDisplay,
        bool Fire,
        ImmutableArray<ParameterInfo> Parameters,
        ImmutableArray<Diagnostic> Diagnostics,
        bool HasBlockingError);
}
