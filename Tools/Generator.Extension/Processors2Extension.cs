using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Generator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp;

namespace GoPlay.Generators.Extension
{
    public static class Processors2Extension
    {
        private const string PROCESSOR_BASE = "ProcessorBase";
        private const string PROCESSOR = "Processor";
        private const string REQUEST = "Request";
        private const string NOTIFY = "Notify";
        private const string SERVER_TAG = "ServerTag";
        private const string HEADER = "Header";
        private const string PUSHES = "Pushes";
        private const string OVERRIDE = "override";
        private const string BEFORE_LOGIN = "BeforeLogin";

        private static string[] BASIC_NAMESPACES = GeneratorUtils.GetBasicConf("basic_ns_ext");
        private static string TPL_FRONTEND = GeneratorUtils.GetTpl("tpl_frontend");
        private static string TPL_BACKEND = GeneratorUtils.GetTpl("tpl_backend");
        
        private static List<TemplateData> _frontends;
        private static List<TemplateData> _backends;

        private static List<PushData> _frontPushes;
        private static List<PushData> _backPushes;

        private static List<string> _frontNs;
        private static List<string> _backNs;
        
        private static string _outFrontFile;
        private static string _outBackFile;
        private static string[] _baseClasses;
        private static string[] _ignoreTypes;
        private static string[] _ignoreMethods;
        
        public static async Task Generate(string slnFolder, string outputFrontendFile, string outputBackendFile, string baseClasses, string frontendTplPath="", string backendTplPath="", string frontendNs="", string backendNs="", string ignoreTypes="", string ignoreMethods="")
        {
            _outFrontFile = outputFrontendFile;
            _outBackFile = outputBackendFile;
            _baseClasses = baseClasses?.Split(",", StringSplitOptions.RemoveEmptyEntries);
            var frontendTpl = string.IsNullOrEmpty(frontendTplPath) ? TPL_FRONTEND : await File.ReadAllTextAsync(frontendTplPath);
            var backendTpl = string.IsNullOrEmpty(backendTplPath) ? TPL_BACKEND : await File.ReadAllTextAsync(backendTplPath);

            if (string.IsNullOrEmpty(_outFrontFile) &&
                string.IsNullOrEmpty(_outBackFile))
            {
                throw new Exception("FrontendFile or BackendFile must be set!");
            }
            
            _frontends = new List<TemplateData>();
            _backends = new List<TemplateData>();

            _frontPushes = new List<PushData>();
            _backPushes = new List<PushData>();

            _frontNs = CreateNs(frontendNs);
            _backNs = CreateNs(backendNs);
            _ignoreTypes = ignoreTypes.Split(",", StringSplitOptions.RemoveEmptyEntries);
            _ignoreMethods = ignoreMethods.Split(",", StringSplitOptions.RemoveEmptyEntries);
            
            var files = Directory.EnumerateFiles(slnFolder, "*.csproj", SearchOption.AllDirectories);
            foreach (var csprojPath in files)
            {
                if (csprojPath.Replace("\\", "/").Contains("/Frameworks/")) continue;
                await ResolveProject(csprojPath);
            }

            if(!string.IsNullOrEmpty(_outFrontFile)) Write(_outFrontFile, frontendTpl, _frontends, _frontPushes, _frontNs);
            if(!string.IsNullOrEmpty(_outBackFile)) Write(_outBackFile, backendTpl, _backends, _backPushes, _backNs);
        }

        private static List<string> CreateNs(string ns)
        {
            if (string.IsNullOrEmpty(ns)) return BASIC_NAMESPACES.ToList();
            
            var nsList = ns.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .Where(o => !string.IsNullOrEmpty(o));

            return BASIC_NAMESPACES.Union(nsList)
                                   .ToList();
        }

        private static void Write(string outFile, string tpl, List<TemplateData> data, List<PushData> pushes, List<string> ns)
        {
            Console.Write($"Writing: {outFile}...");
            {
                var content = GeneratorUtils.RenderTpl(tpl, new
                {
                    data = data.OrderBy(o => o.route).ToList(),
                    pushes = pushes.OrderBy(o => o.route).ToList(),
                    namespaces = ns.Distinct().OrderBy(o => o).ToList(),
                });
                File.WriteAllText(outFile, content);
            }
            Console.WriteLine("Done");
        }

        private static async Task ResolveProject(string csprojPath)
        {
            // Console.WriteLine($"{csprojPath}");

            using MSBuildWorkspace mSBuildWorkspace = MSBuildWorkspace.Create();
            var project = await mSBuildWorkspace.OpenProjectAsync(csprojPath);
            var sourceFiles = Directory.GetFiles(Path.GetDirectoryName(csprojPath)!, "*.cs", SearchOption.AllDirectories)
                                                 .Where(o => !o.Replace("\\", "/").Contains("/obj/") &&
                                                                  !o.Replace("\\", "/").Contains("/bin/"))
                                                 .ToList();
            foreach (var file in sourceFiles)
            {
                project = project.AddDocument(file, await File.ReadAllTextAsync(file)).Project;
            }
            
            var compilation = await project.GetCompilationAsync();
            // var semanticModel = compilation!.GetSemanticModel(compilation.SyntaxTrees.FirstOrDefault()!);
            var symbolsWithNames = compilation!.GetSymbolsWithName((string name) => true, SymbolFilter.Type);
            var types = symbolsWithNames.OfType<INamedTypeSymbol>().ToArray();
            foreach (var type in types)
            {
                if (!CheckType(compilation, type)) continue;

                var isFrontEnd = ProcessorIsFrontEnd(type);
                var isBackEnd = ProcessorIsBackEnd(type);

                var methods = GetMethods(type);
                foreach (var method in methods)
                {
                    var data = GetTemplateData(type, method);
                    if (!CheckMethod(type, method)) continue;

                    var isMethodFrontEnd = MethodIsFrontEnd(method);
                    var isMethodBackEnd = MethodIsBackEnd(method);
                    
                    if (isFrontEnd && isMethodFrontEnd) _frontends.Add(data);
                    if (isBackEnd && isMethodBackEnd) _backends.Add(data);
                }

                var pushes = GetPushes(type).ToList();
                if (isFrontEnd) _frontPushes.AddRange(pushes);
                if (isBackEnd) _backPushes.AddRange(pushes);
            }
        }

        private static bool CheckMethod(INamedTypeSymbol type, MethodDeclarationSyntax method)
        {
            if (_ignoreMethods == null) return true;
            if (_ignoreMethods.Length <= 0) return true;

            var processorName = type.Name;
            var methodName = method.Identifier.ValueText;
            
            var methodFullName = $"{processorName}.{methodName}";
            return !_ignoreMethods.Contains(methodFullName);
        }

        private static bool CheckType(Compilation compilation, INamedTypeSymbol type)
        {
            if (type.Name == PROCESSOR_BASE) return false;
            if (_baseClasses?.Contains(type.Name) ?? false) return false;
            if (_ignoreTypes?.Contains(type.Name) ?? false) return false;

            var types = type.DeclaringSyntaxReferences
                .Select(o => o.GetSyntax() as ClassDeclarationSyntax)
                .Where(o => o != null)
                .Select(o => compilation.GetSemanticModel(o.SyntaxTree).GetDeclaredSymbol(o) as ITypeSymbol)
                .ToList();
            var baseTypes = types.SelectMany(o => o.AllInterfaces).Union(types.Select(o => o.BaseType)).Where(o => o != null).ToList();
            if (!baseTypes.Any(t => t.Name == PROCESSOR_BASE || (_baseClasses?.Contains(t.Name) ?? false))) return false;
            
            return true;
        }

        private static IEnumerable<PushData> GetPushes(INamedTypeSymbol type)
        {
            var pushes = type.DeclaringSyntaxReferences.Select(o => o.GetSyntax().DescendantNodes()
                .OfType<PropertyDeclarationSyntax>().FirstOrDefault(o =>
                    o.Identifier.ToString() == PUSHES && o.Modifiers.ToString().Contains(OVERRIDE)))
                .FirstOrDefault(o => o != null);
            if (pushes == null) yield break;

            var tokens = pushes.DescendantTokens().OfType<SyntaxToken>().Where(o => o.IsKind(SyntaxKind.StringLiteralToken));
            foreach (var token in tokens)
            {
                yield return new PushData
                {
                    name = GetPushPropName(token.ValueText),
                    route = token.ValueText,
                };
            }
        }

        private static string GetPushPropName(string val)
        {
            var arr = val.Split(".", StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var token in arr)
            {
                var item = $"{token.Substring(0, 1).ToUpper()}{token.Substring(1).ToLower()}";
                sb.Append(item);
            }

            return sb.ToString();
        }

        private static bool ProcessorIsBackEnd(INamedTypeSymbol type)
        {
            var attr = GetAttribute(type, SERVER_TAG);
            if (attr == null) return true;

            var attrArg = attr.DescendantNodes().OfType<AttributeArgumentSyntax>().FirstOrDefault();
            if (attrArg == null) return true;

            var exps = attrArg.Expression.ToString();
            if (exps.Contains("ServerTag.BackEnd")) return true;
            if (exps.Contains("ServerTag.All")) return true;

            return false;
        }

        private static bool ProcessorIsFrontEnd(INamedTypeSymbol type)
        {
            var attr = GetAttribute(type, SERVER_TAG);
            if (attr == null) return true;

            var attrArg = attr.DescendantNodes().OfType<AttributeArgumentSyntax>().FirstOrDefault();
            if (attrArg == null) return true;

            var exps = attrArg.Expression.ToString();
            if (exps.Contains("ServerTag.FrontEnd")) return true;
            if (exps.Contains("ServerTag.All")) return true;

            return false;
        }
        
        private static bool MethodIsBackEnd(MethodDeclarationSyntax method)
        {
            var attr = GetAttribute(method, SERVER_TAG);
            if (attr == null) return true;

            var attrArg = attr.DescendantNodes().OfType<AttributeArgumentSyntax>().FirstOrDefault();
            if (attrArg == null) return true;

            var exps = attrArg.Expression.ToString();
            if (exps.Contains("ServerTag.BackEnd")) return true;
            if (exps.Contains("ServerTag.All")) return true;

            return false;
        }

        private static bool MethodIsFrontEnd(MethodDeclarationSyntax method)
        {
            var attr = GetAttribute(method, SERVER_TAG);
            if (attr == null) return true;

            var attrArg = attr.DescendantNodes().OfType<AttributeArgumentSyntax>().FirstOrDefault();
            if (attrArg == null) return true;

            var exps = attrArg.Expression.ToString();
            if (exps.Contains("ServerTag.FrontEnd")) return true;
            if (exps.Contains("ServerTag.All")) return true;

            return false;
        }

        private static TemplateData GetTemplateData(INamedTypeSymbol processor, MethodDeclarationSyntax method)
        {
            var data = new TemplateData();

            var param = GetParameter(method.ParameterList);

            var processorName = processor.Name;
            if (processorName.StartsWith(PROCESSOR)) processorName = processorName.Substring(PROCESSOR.Length);
            if (processorName.EndsWith(PROCESSOR)) processorName = processorName.Substring(0, processorName.Length - PROCESSOR.Length);
            
            //fill route
            data.route = $"{GetProcessorName(processor)}.{GetMethodName(method)}";

            data.method = $"{processorName}_{method.Identifier.ValueText}";
            data.returnType = GetReturnType(method.ReturnType.ToString());
            data.paramType = param?.Type?.ToString();
            data.isNeedLogin = GetIsNeedLogin(method);
            
            return data;
        }

        private static bool GetIsNeedLogin(MethodDeclarationSyntax method)
        {
            return method.DescendantNodes().OfType<AttributeSyntax>().All(o => o.Name.ToString() != BEFORE_LOGIN);
        }

        private static string GetReturnType(string type)
        {
            if (!type.StartsWith("Task<") || !type.EndsWith(">")) return type;
            return type.Substring(5, type.Length - 6);
        }

        private static ParameterSyntax GetParameter(ParameterListSyntax pl)
        {
            foreach (var parameter in pl.Parameters)
            {
                if (parameter.Type == null) continue;
                if (parameter.Type.ToString() == HEADER) continue;

                return parameter;
            }
            
            return null;
        }

        private static string GetMethodName(MethodDeclarationSyntax method)
        {
            var attr = method.DescendantNodes().OfType<AttributeSyntax>().FirstOrDefault(o => o.Name.ToString() == REQUEST || o.Name.ToString() == NOTIFY);
            if (attr == null) return method.Identifier.ValueText.ToLower();

            var token = attr.DescendantTokens()
                .FirstOrDefault(o => o.IsKind(SyntaxKind.StringLiteralToken));
            if (token == default) return method.Identifier.ValueText.ToLower();

            return token.ValueText.ToLower();
        }

        private static IEnumerable<MethodDeclarationSyntax> GetMethods(INamedTypeSymbol type)
        {
            var methods = type.DeclaringSyntaxReferences.SelectMany(o => o.GetSyntax().DescendantNodes().OfType<MethodDeclarationSyntax>());
            if (methods == null) yield break;
            foreach (var method in methods)
            {
                var attrs = method.DescendantNodes().OfType<AttributeSyntax>();
                if (attrs.Any(o => o.Name.ToString() == REQUEST || o.Name.ToString() == NOTIFY))
                {
                    yield return method;
                }
            }
        }

        private static string GetProcessorName(INamedTypeSymbol type)
        {
            var attr = GetAttribute(type, PROCESSOR);
            if (attr == null) return type.Name.ToLower();

            var token = attr.DescendantTokens()
                .FirstOrDefault(o => o.IsKind(SyntaxKind.StringLiteralToken));
            if (token == default) return type.Name.ToLower();

            return token.ValueText.ToLower();
        }

        private static AttributeSyntax GetAttribute(INamedTypeSymbol type, string attrName)
        {
            var attrs = type.DeclaringSyntaxReferences.Select(o =>
                                                            o.GetSyntax()
                                                             .DescendantNodes()
                                                             .OfType<AttributeSyntax>()
                                                             .FirstOrDefault(o => o.Name.ToString() == attrName));
            return attrs.FirstOrDefault(o => o != null);
        }
        
        private static AttributeSyntax GetAttribute(MethodDeclarationSyntax method, string attrName)
        {
            var attr = method
                    .DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .FirstOrDefault(o => o.Name.ToString() == attrName);
            return attr;
        }
    }
}