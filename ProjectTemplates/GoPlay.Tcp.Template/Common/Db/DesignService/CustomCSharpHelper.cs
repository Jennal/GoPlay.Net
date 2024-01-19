using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Storage;

public class CSharpCodeGenerationExpressionString
{
    public string ExpressionString { get; }

    public CSharpCodeGenerationExpressionString(string expressionString)
        => ExpressionString = expressionString;
}

public class CustomCSharpHelper : CSharpHelper
{
    public CustomCSharpHelper(ITypeMappingSource typeMappingSource)
        : base(typeMappingSource)
    {
    }

    public override string UnknownLiteral(object value)
        => value is CSharpCodeGenerationExpressionString codeGenerationExpressionString
            ? codeGenerationExpressionString.ExpressionString
            : base.UnknownLiteral(value);
}