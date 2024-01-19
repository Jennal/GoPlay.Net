using Microsoft.Extensions.DependencyInjection;

namespace GoPlayProj.ServiceProviders;

public static class LoggerServiceProvider
{
    public static IServiceCollection AddLogger(this IServiceCollection services)
    {
        services.AddLogging(cfg => { /* REMOVE LOG */ });
        // services.AddLogging(cfg => cfg.AddConsole().AddDebug());
        return services;
    }
}