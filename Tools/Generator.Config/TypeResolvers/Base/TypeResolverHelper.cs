namespace GoPlay.Generators.Config;

public static class TypeResolverHelper
{
    public static bool IsConcreteResolver(Type t)
    {
        if (t is not { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }) return false;
        if (t == typeof(TypeResolverBase) || t == typeof(TypeResolverBase<>)) return false;
        return t.InheritsFrom(typeof(TypeResolverBase));
    }

    public static List<TypeResolverBase> CreateAll()
    {
        var list = new List<TypeResolverBase>();
        var types = ReflectionHelper.GetTypesInAllLoadedAssemblies(IsConcreteResolver);
        foreach (var type in types)
        {
            try
            {
                if (Activator.CreateInstance(type) is TypeResolverBase resolver)
                    list.Add(resolver);
            }
            catch (Exception ex)
            {
                ExporterUtils.Warning($"无法实例化 TypeResolver：{type.FullName} — {ex.Message}");
            }
        }

        return list;
    }
}
