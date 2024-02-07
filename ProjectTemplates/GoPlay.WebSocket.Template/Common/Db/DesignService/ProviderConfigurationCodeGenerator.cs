using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using GoPlayProj.Database;

public class ProviderConfigurationCodeGenerator : MySqlCodeGenerator 
{
    private static readonly MethodInfo _enableSensitiveDataLoggingMethodInfo = typeof(DbContextOptionsBuilder).GetRequiredRuntimeMethod(
        nameof(DbContextOptionsBuilder.EnableSensitiveDataLogging),
        typeof(bool));
    
    private static readonly MethodInfo _useNewtonJsonMethodInfo = typeof(MySqlJsonNewtonsoftDbContextOptionsBuilderExtensions).GetRequiredRuntimeMethod(
        nameof(MySqlJsonNewtonsoftDbContextOptionsBuilderExtensions.UseNewtonsoftJson),
        typeof(MySqlDbContextOptionsBuilder),
        typeof(MySqlCommonJsonChangeTrackingOptions));
    
    private static readonly MethodInfo _logToMethodInfo = typeof(DbContextOptionsBuilder).GetRequiredRuntimeMethod(
        nameof(DbContextOptionsBuilder.LogTo),
        typeof(Action<string>),
        typeof(Func<EventId, LogLevel, bool>),
        typeof(DbContextLoggerOptions?));
    
    private static readonly MethodInfo _logMethodInfo = typeof(GoPlayProjContext).GetRequiredRuntimeMethod(
        nameof(GoPlayProjContext.Log),
        typeof(string));
    
    private static readonly MethodInfo _logFilterMethodInfo = typeof(GoPlayProjContext).GetRequiredRuntimeMethod(
        nameof(GoPlayProjContext.LogFilter),
        typeof(EventId),
        typeof(LogLevel));

    private readonly ProviderCodeGeneratorDependencies _dependencies;
    private readonly IMySqlOptions _options;
    
    public ProviderConfigurationCodeGenerator(ProviderCodeGeneratorDependencies dependencies, IMySqlOptions options) : base(dependencies, options)
    {
        _dependencies = dependencies;
        _options = options;
    }
    
    public override MethodCallCodeFragment GenerateUseProvider(string connectionString, MethodCallCodeFragment? providerOptions)
    {
        if (providerOptions == null)
        {
            providerOptions = new MethodCallCodeFragment(_useNewtonJsonMethodInfo);
        }
        else
        {
            providerOptions = providerOptions.Chain(new MethodCallCodeFragment(_useNewtonJsonMethodInfo));
        }
        var fragment = base.GenerateUseProvider(connectionString, providerOptions);
        fragment = fragment.Chain(_enableSensitiveDataLoggingMethodInfo);
        // fragment = fragment.Chain(_logToMethodInfo, 
        //     new MethodCall(_logMethodInfo),
        //     new MethodCall(_logFilterMethodInfo),
        //     DbContextLoggerOptions.DefaultWithLocalTime);

        return fragment;
    }
}

public static class TypeExtensions
{
    public static MethodInfo GetRequiredRuntimeMethod(this Type type, string name, params Type[] parameters)
        => type.GetTypeInfo().GetRuntimeMethod(name, parameters)
           ?? throw new InvalidOperationException($"Could not find method '{name}' on type '{type}'");
}
