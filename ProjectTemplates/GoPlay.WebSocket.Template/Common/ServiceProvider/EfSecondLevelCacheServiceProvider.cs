using EFCoreSecondLevelCacheInterceptor;
using Microsoft.Extensions.DependencyInjection;

namespace GoPlayProj.ServiceProviders;

public static class EfSecondLevelCacheServiceProvider
{
    public static IServiceCollection Add2ndLvCache(this IServiceCollection services)
    {
        services.AddEFSecondLevelCache(options => 
            options.UseEasyCachingCoreProvider(Consts.ProviderName.RedisForDb, isHybridCache: false)
                .DisableLogging(true)
                .UseCacheKeyPrefix("EF_"));
        return services;
    }
}