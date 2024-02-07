using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using GoPlayProj.Database;

namespace GoPlayProj.ServiceProviders;

public static class DbContextServiceProvider
{
    public static IServiceCollection AddGoPlayProjContextPool(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextPool<GoPlayProjContext>((serviceProvider, optionsBuilder) =>
            optionsBuilder
                .UseMySql(
                    connectionString,
                    ServerVersion.Parse("8.0.29-mysql"), 
                    options => options.UseNewtonsoftJson()
                        .EnableRetryOnFailure())
                .EnableSensitiveDataLogging()
                .LogTo(GoPlayProjContext.Log, GoPlayProjContext.LogFilter, DbContextLoggerOptions.DefaultWithLocalTime)
                .AddInterceptors(serviceProvider.GetRequiredService<SecondLevelCacheInterceptor>()));
        
        return services;
    }
}