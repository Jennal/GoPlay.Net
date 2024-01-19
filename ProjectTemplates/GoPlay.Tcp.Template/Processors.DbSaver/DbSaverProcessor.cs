using GoPlay.Core.Attributes;
using GoPlay.Core.Protocols;
using GoPlay.Interfaces;

namespace GoPlayProj.Processors;

[ServerTag(Tag = ServerTag.BackEnd)]
[Processor("dbsaver")]
public class DbSaverProcessor : GoPlayProjProcessor, IUpdate, IStop
{
    public override string[] Pushes => null;

    public void OnUpdate()
    {
        SaveDbModules();
    }

    public void OnStop()
    {
        SaveDbModules();
        ClearCaches();
    }

    private void ClearCaches()
    {
        // if (!RunArgs.ClearCache) return;
        //
        // try
        // {
        //     var redisForData = ServiceProvider.GetRequiredService<IEasyCachingProviderFactory>()
        //         .GetCachingProvider(Consts.ProviderName.RedisForData);
        //     var redisForDb = ServiceProvider.GetRequiredService<IEasyCachingProviderFactory>()
        //         .GetCachingProvider(Consts.ProviderName.RedisForDb);
        //
        //     redisForData.Flush();
        //     redisForDb.Flush();
        // }
        // catch
        // {
        //     /* IGNORE EXCEPTION */
        // }
    }

    private void SaveDbModules()
    {
        // var provider = ServiceProvider.GetRequiredService<IDbModuleProvider>();
        // foreach (var module in provider.Modules)
        // {
        //     ContextScope scope = null;
        //     try
        //     {
        //         scope = GetDbContextScope();
        //         module.Save(scope.DbContext, this).Wait();
        //     }
        //     catch (Exception err)
        //     {
        //         Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
        //     }
        //     finally
        //     {
        //         scope?.Dispose();
        //     }
        // }
    }

    public override void Push<T>(string route, uint clientId, T data)
    {
        var id = Server.ToRouteId(route);
        var package = Package.Create(id, data, PackageType.Push, Server.EncodingType);
        package.Header.ClientId = clientId;
        Server.Send(package);
    }
}