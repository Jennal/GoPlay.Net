using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.Extensions.Logging;

public class CustomProviderCodeGeneratorPlugin : IProviderCodeGeneratorPlugin
{
    private static readonly MethodInfo EnableSensitiveDataLoggingMethodInfo = typeof(DbContextOptionsBuilder).GetRequiredRuntimeMethod(
        nameof(DbContextOptionsBuilder.EnableSensitiveDataLogging),
        typeof(bool));

    private static readonly MethodInfo UseNewtonJsonMethodInfo = typeof(MySqlJsonNewtonsoftDbContextOptionsBuilderExtensions).GetRequiredRuntimeMethod(
        nameof(MySqlJsonNewtonsoftDbContextOptionsBuilderExtensions.UseNewtonsoftJson),
        typeof(MySqlDbContextOptionsBuilder),
        typeof(MySqlCommonJsonChangeTrackingOptions));
    
    private static readonly MethodInfo LogToMethodInfo = typeof(DbContextOptionsBuilder).GetRequiredRuntimeMethod(
        nameof(DbContextOptionsBuilder.LogTo),
        typeof(Action<string>),
        typeof(Func<EventId, LogLevel, bool>),
        typeof(DbContextLoggerOptions?));

    public MethodCallCodeFragment GenerateProviderOptions()
        => new MethodCallCodeFragment(UseNewtonJsonMethodInfo);

    public MethodCallCodeFragment GenerateContextOptions()
        => new MethodCallCodeFragment(EnableSensitiveDataLoggingMethodInfo)
            .Chain(GenerateLogToMethodCallCodeFragment());

    private MethodCallCodeFragment GenerateLogToMethodCallCodeFragment()
        => new MethodCallCodeFragment(
            LogToMethodInfo,
            new CSharpCodeGenerationExpressionString("Log"),
            new CSharpCodeGenerationExpressionString("LogFilter"),
            new CSharpCodeGenerationExpressionString("Microsoft.EntityFrameworkCore.Diagnostics.DbContextLoggerOptions.DefaultWithLocalTime"));
}