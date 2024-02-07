using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage;

public class MethodCall
{
    public MethodInfo Method;

    public MethodCall(MethodInfo info)
    {
        Method = info;
    }
}

public class MethodCallTypeMapping : RelationalTypeMapping
{
    private const string DummyStoreType = "clrOnly";

    public MethodCallTypeMapping()
        : base(new RelationalTypeMappingParameters(new CoreTypeMappingParameters(typeof(MethodCall)), DummyStoreType))
    {
    }

    protected MethodCallTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new MethodCallTypeMapping(parameters);

    public override string GenerateSqlLiteral(object value)
        => throw new InvalidOperationException("This type mapping exists for code generation only.");

    public override Expression GenerateCodeLiteral(object value)
    {
        return value is MethodCall methodCall
            ? Expression.Call(methodCall.Method)
            : null;
    }
}