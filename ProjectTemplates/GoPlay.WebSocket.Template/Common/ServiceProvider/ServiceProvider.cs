using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using StackExchange.Redis.MultiplexerPool;
using GoPlay.Common;
using GoPlayProj.Database;
using GoPlayProj.ServiceProviders;

namespace GoPlayProj;

public static class ServiceProvider
{
    private static readonly Lazy<IServiceProvider> _serviceProviderBuilder =
        new Lazy<IServiceProvider>(getServiceProvider, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// A lazy loaded thread-safe singleton
    /// </summary>
    public static IServiceProvider Instance { get; } = _serviceProviderBuilder.Value;

    public static T GetRequiredService<T>() 
        where T : notnull
    {
        return Instance.GetRequiredService<T>();
    }

    private static IServiceProvider getServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddOptions();

        //Configuration
        var cmd = Environment.CommandLine.Split(" ", StringSplitOptions.RemoveEmptyEntries);
        var basePath = GetConfigBasePath(cmd);
        Console.WriteLine($"Using `{basePath}/{RunArgs.AppConfigFile}` as Config");
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(RunArgs.AppConfigFile, optional: false, reloadOnChange: true)
            .Build();
        services.AddSingleton(_ => configuration);

        services.AddLogger();
        // services.AddFreeSql(configuration["MySql"]);
        services.Add2ndLvCache();
        services.AddGoPlayProjContextPool(configuration["MySql"]);
        services.AddRedisCaching(configuration["Redis:Host"], int.Parse(configuration["Redis:Port"]));
        services.AddRedis(configuration["Redis:Host"], int.Parse(configuration["Redis:Port"]), int.Parse(configuration["Redis:Pool"]));
        // services.AddDbModuleServiceProvider();

        return services.BuildServiceProvider();
    }

    private static string? GetConfigBasePath(string[] cmd)
    {
        if (cmd.Any(o => o.Contains("ReSharperTestRunner.dll")))
        {
            //Unit Test
            return Path.GetFullPath(".");
        }

        return Path.GetDirectoryName(cmd[0]);
    }
    
    public static ContextScope GetContextScope()
    {
        var serviceScope = GetRequiredService<IServiceScopeFactory>().CreateScope();
        var context = serviceScope.ServiceProvider.GetRequiredService<GoPlayProjContext>();
        return new ContextScope(serviceScope, context);
    }

    public static async Task<IDatabase> GetRedis()
    {
        var pool = GetRequiredService<IConnectionMultiplexerPool>();
        var redis = await pool.GetAsync();
        var configuration = GetRequiredService<IConfigurationRoot>();
        var db = int.Parse(configuration["Redis:Db"]);

        Console.WriteLine($"Reids => {redis.ConnectionIndex}");
        
        try
        {
            return redis.Connection.GetDatabase(db);
        }
        catch (RedisConnectionException)
        {
            await redis.ReconnectAsync();
            return redis.Connection.GetDatabase(db);
        }
    }
}