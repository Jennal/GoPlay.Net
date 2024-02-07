using EasyCaching.Serialization.SystemTextJson.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace GoPlayProj.ServiceProviders;

public static class CachingServiceProvider
{
    public static IServiceCollection AddRedisCaching(this IServiceCollection services, string host, int port)
    {
        services.AddEasyCaching(option =>
        {
            option.WithSystemTextJson(Consts.ProviderName.SystemTextJson);
            
            option.UseInMemory(Consts.ProviderName.MemoryForData);
            
            option.UseRedis(config =>
            {
                config.DBConfig.Database = 14;
                config.DBConfig.AllowAdmin = true;
                config.DBConfig.SyncTimeout = 10000;
                config.DBConfig.AsyncTimeout = 10000;
                config.DBConfig.Endpoints.Add(new EasyCaching.Core.Configurations.ServerEndPoint(host, port));
                config.SerializerName = Consts.ProviderName.SystemTextJson; //需要 WithSystemTextJson 提供的名字一致
            }, Consts.ProviderName.RedisForData);

            option.UseHybrid(config =>
            {
                config.TopicName = "GoPlayProj-data";
                config.EnableLogging = false;

                // specify the local cache provider name after v0.5.4
                config.LocalCacheProviderName = Consts.ProviderName.MemoryForData;
                // specify the distributed cache provider name after v0.5.4
                config.DistributedCacheProviderName = Consts.ProviderName.RedisForData;
            }, Consts.ProviderName.HybridForData)
            .WithRedisBus(busConf => 
            {
                busConf.Endpoints.Add(new EasyCaching.Core.Configurations.ServerEndPoint(host, port));
                busConf.Database = 13;
                busConf.AllowAdmin = true;

                // do not forget to set the SerializerName for the bus here !!
                busConf.SerializerName = Consts.ProviderName.SystemTextJson; //需要 WithSystemTextJson 提供的名字一致
            });
            
            option.UseRedis(config =>
            {
                config.DBConfig.Database = 15;
                config.DBConfig.AllowAdmin = true;
                config.DBConfig.SyncTimeout = 10000;
                config.DBConfig.AsyncTimeout = 10000;
                config.DBConfig.Endpoints.Add(new EasyCaching.Core.Configurations.ServerEndPoint(host, port));
                config.SerializerName = Consts.ProviderName.Json; //需要和后面 WithJson 提供的名字一致
            }, Consts.ProviderName.RedisForDb)
            .WithJson(Consts.ProviderName.Json);
        });

        return services;
    }
}