using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using StackExchange.Redis.MultiplexerPool;

namespace GoPlayProj.ServiceProviders;

public static class RedisServiceProvider
{
    public static IServiceCollection AddRedis(this IServiceCollection services, string host, int port, int poolSize)
    {
        //加了Admin速度变超慢，没法用！
        var config = new ConfigurationOptions
        {
            EndPoints = {host, port.ToString()},
            AllowAdmin = true,
        };
        // var pool = ConnectionMultiplexerPoolFactory.Create(
        //     poolSize: poolSize,
        //     configurationOptions: config,
        //     connectionSelectionStrategy: ConnectionSelectionStrategy.RoundRobin);
        var pool = ConnectionMultiplexerPoolFactory.Create(
            poolSize: poolSize,
            configuration: $"{host}:{port}",
            connectionSelectionStrategy: ConnectionSelectionStrategy.RoundRobin);
        services.AddSingleton(pool);

        return services;
    }
}