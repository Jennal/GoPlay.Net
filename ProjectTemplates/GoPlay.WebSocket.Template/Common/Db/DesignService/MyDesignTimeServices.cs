using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.DependencyInjection;

public class MyDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        services.AddSingleton<IProviderCodeGeneratorPlugin, CustomProviderCodeGeneratorPlugin>();
        services.AddSingleton<ICSharpHelper, CustomCSharpHelper>();
        services.AddEntityFrameworkMySqlJsonNewtonsoft();
        
        // Setup our own implementation based on the default one.
        services.AddSingleton<IScaffoldingModelFactory, CustomRelationalScaffoldingModelFactory>();

        // services.AddSingleton<IRelationalTypeMappingSource, CustomTypeMappingSource>();
        
        //Option Generator
        // services.AddSingleton<IProviderConfigurationCodeGenerator, ProviderConfigurationCodeGenerator>();
        
        // Setup our own implementation based on the default one.
        // services.AddSingleton<IRelationalTypeMappingSourcePlugin, CustomMySqlJsonNewtonsoftTypeMappingSourcePlugin>();

        // Add all default implementations.
        // services.AddEntityFrameworkMySqlJsonNewtonsoft();
    }
}

public class CustomRelationalScaffoldingModelFactory : RelationalScaffoldingModelFactory
{
    public CustomRelationalScaffoldingModelFactory(
        IOperationReporter reporter,
        ICandidateNamingService candidateNamingService,
        IPluralizer pluralizer,
        ICSharpUtilities cSharpUtilities,
        IScaffoldingTypeMapper scaffoldingTypeMapper,
        // LoggingDefinitions loggingDefinitions,
        IModelRuntimeInitializer modelRuntimeInitializer)
        : base(
            reporter,
            candidateNamingService,
            pluralizer,
            cSharpUtilities,
            scaffoldingTypeMapper,
            // loggingDefinitions,
            modelRuntimeInitializer)
    {
    }
    
    protected override TypeScaffoldingInfo? GetTypeScaffoldingInfo(DatabaseColumn column)
    {
        var typeScaffoldingInfo = base.GetTypeScaffoldingInfo(column);
        // Console.WriteLine($"table:{column.Table.Name} => column:{column.Name} : {column.StoreType}");
        
        // Use any logic you want, to determine the true target CLR type of the
        // property.
        //
        // For this sample code, we assume that the target CLR type has been
        // specified in the comment of the column of the database table,
        // e.g. like: System.Int32[]
        //Json
        if (typeScaffoldingInfo is not null &&
            (column.StoreType == "json") &&
            !string.IsNullOrEmpty(column.Comment))
        {
            var clrTypeName = column.Comment;
            var clrType = FindType(clrTypeName) ?? typeof(string);
            Console.WriteLine($"column:{column.Name}, Comment:{column.Comment}, clrType:{clrType}");

            // Regenerate the TypeScaffoldingInfo based on our new CLR type.
            typeScaffoldingInfo = new TypeScaffoldingInfo(
                clrType,
                typeScaffoldingInfo.IsInferred,
                typeScaffoldingInfo.ScaffoldUnicode,
                typeScaffoldingInfo.ScaffoldMaxLength,
                typeScaffoldingInfo.ScaffoldFixedLength,
                typeScaffoldingInfo.ScaffoldPrecision,
                typeScaffoldingInfo.ScaffoldScale);

            // Remove the comment, so that it does not popup in the generated
            // C# source file.
            column.Comment = null;
        }
        
        //enum
        if (typeScaffoldingInfo is not null &&
            (column.StoreType == "tinyint" || column.StoreType == "smallint") &&
            !string.IsNullOrEmpty(column.Comment))
        {
            var clrTypeName = column.Comment;
            var clrType = FindType(clrTypeName);
            if (clrType != null)
            {
                Console.WriteLine($"column:{column.Name}, Comment:{column.Comment}, clrType:{clrType}");
                // Regenerate the TypeScaffoldingInfo based on our new CLR type.
                typeScaffoldingInfo = new TypeScaffoldingInfo(
                    clrType,
                    typeScaffoldingInfo.IsInferred,
                    typeScaffoldingInfo.ScaffoldUnicode,
                    typeScaffoldingInfo.ScaffoldMaxLength,
                    typeScaffoldingInfo.ScaffoldFixedLength,
                    typeScaffoldingInfo.ScaffoldPrecision,
                    typeScaffoldingInfo.ScaffoldScale);

                // Remove the comment, so that it does not popup in the generated
                // C# source file.
                column.Comment = null;
            }
        }

        return typeScaffoldingInfo;
    }

    private Type? FindType(string typeName)
    {
        switch (typeName)
        {
            //Json Types
            case "Dictionary<int, int>":
                return typeof(Dictionary<int, int>);
            case "HashSet<string>":
                return typeof(HashSet<string>);
            case "int[]":
                return typeof(int[]);
            case "string[]":
                return typeof(string[]);
            
            //Enum Types
        }

        return null;
    }
}

// public class CustomMySqlJsonNewtonsoftTypeMappingSourcePlugin : MySqlJsonNewtonsoftTypeMappingSourcePlugin
// {
//     public CustomMySqlJsonNewtonsoftTypeMappingSourcePlugin(IMySqlOptions options)
//         : base(options)
//     {
//     }
//
//     public override RelationalTypeMapping FindMapping(in RelationalTypeMappingInfo mappingInfo)
//     {
//         if (string.Equals(mappingInfo.StoreTypeNameBase, "json", StringComparison.OrdinalIgnoreCase) &&
//             mappingInfo.ClrType is null)
//         {
//             var customMappingInfo = new RelationalTypeMappingInfo(
//                 typeof(int[]), // <-- your target CLR type
//                 mappingInfo.StoreTypeName,
//                 mappingInfo.StoreTypeNameBase,
//                 mappingInfo.IsKeyOrIndex,
//                 mappingInfo.IsUnicode,
//                 mappingInfo.Size,
//                 mappingInfo.IsRowVersion,
//                 mappingInfo.IsFixedLength,
//                 mappingInfo.Precision,
//                 mappingInfo.Scale);
//
//             return base.FindMapping(customMappingInfo);
//         }
//
//         return base.FindMapping(mappingInfo);
//     }
// }