using System.Reflection;
using System.Runtime.Loader;

namespace GoPlay.Generators.Config;

/// <summary>
/// 将用户扩展 DLL 装入默认 AssemblyLoadContext，使现有的
/// <see cref="TypeResolverBase"/> 扫描能够发现自定义类型转换。
/// </summary>
public static class ExtensionAssemblyLoader
{
    private static bool _hooked;
    private static readonly List<AssemblyDependencyResolver> _resolvers = new();
    private static readonly List<string> _probeDirs = new();

    /// <summary>
    /// 加载扩展 DLL。支持逗号或分号分隔的多个路径。
    /// 失败时返回 false（已输出错误信息）。
    /// </summary>
    public static bool Load(string dllPaths)
    {
        if (string.IsNullOrWhiteSpace(dllPaths)) return true;

        EnsureResolvingHook();

        var paths = dllPaths.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in paths)
        {
            if (!LoadOne(raw.Trim())) return false;
        }

        return true;
    }

    private static bool LoadOne(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            ExporterUtils.Error($"扩展 DLL 不存在：{fullPath}");
            return false;
        }

        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !_probeDirs.Contains(dir))
        {
            _probeDirs.Add(dir);
            _resolvers.Add(new AssemblyDependencyResolver(fullPath));
        }

        Assembly asm;
        try
        {
            asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
        }
        catch (FileLoadException)
        {
            asm = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(a =>
                string.Equals(a.Location, fullPath, StringComparison.OrdinalIgnoreCase));
            if (asm == null) throw;
        }

        ExporterUtils.Info($"已加载扩展 DLL：{fullPath}");

        var resolverTypes = GetTypesSafe(asm)
            .Where(TypeResolverHelper.IsConcreteResolver)
            .ToList();

        if (resolverTypes.Count == 0)
        {
            ExporterUtils.Warning(
                "扩展 DLL 中未发现 TypeResolverBase 子类。请确认：1) 已继承 TypeResolverBase / TypeResolverBase<T>；2) 引用的是与 goplay 同一套 Generator.Config（不要把 Generator.Config.dll 复制到扩展目录旁，否则类型身份会分裂）。");
            return true;
        }

        foreach (var t in resolverTypes)
        {
            ExporterUtils.Info($"  + TypeResolver: {t.FullName}");
        }

        return true;
    }

    private static void EnsureResolvingHook()
    {
        if (_hooked) return;
        _hooked = true;
        AssemblyLoadContext.Default.Resolving += OnResolving;
    }

    /// <summary>
    /// 优先复用已加载程序集（按简单名对齐），避免用户输出目录里的
    /// Generator.Config.dll / EPPlus.dll 造成 TypeResolverBase 类型身份分裂。
    /// </summary>
    private static Assembly? OnResolving(AssemblyLoadContext ctx, AssemblyName name)
    {
        var loaded = ctx.Assemblies.FirstOrDefault(a =>
            string.Equals(a.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase));
        if (loaded != null) return loaded;

        foreach (var resolver in _resolvers)
        {
            var resolved = resolver.ResolveAssemblyToPath(name);
            if (resolved != null) return ctx.LoadFromAssemblyPath(resolved);
        }

        foreach (var dir in _probeDirs)
        {
            var candidate = Path.Combine(dir, name.Name + ".dll");
            if (File.Exists(candidate))
                return ctx.LoadFromAssemblyPath(Path.GetFullPath(candidate));
        }

        return null;
    }

    private static IEnumerable<Type> GetTypesSafe(Assembly asm)
    {
        try
        {
            return asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var loaderException in ex.LoaderExceptions)
            {
                if (loaderException != null)
                    ExporterUtils.Warning($"加载类型失败：{loaderException.Message}");
            }

            return ex.Types.Where(t => t != null)!;
        }
    }
}
