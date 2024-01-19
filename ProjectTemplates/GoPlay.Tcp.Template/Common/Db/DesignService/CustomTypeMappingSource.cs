using Microsoft.EntityFrameworkCore.Storage;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Pomelo.EntityFrameworkCore.MySql.Storage.Internal;

public class CustomTypeMappingSource : MySqlTypeMappingSource
{
    public CustomTypeMappingSource(TypeMappingSourceDependencies dependencies, RelationalTypeMappingSourceDependencies relationalDependencies, IMySqlOptions options) : base(dependencies, relationalDependencies, options)
    {
    }

    protected override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
    {
        if (mappingInfo.ClrType == typeof(MethodCall))
        {
            return new MethodCallTypeMapping();
        }

        return base.FindMapping(mappingInfo);
    }
}