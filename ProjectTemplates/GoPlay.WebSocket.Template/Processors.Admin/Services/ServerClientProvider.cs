using Microsoft.Extensions.Configuration;
using GoPlay;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.Ws;

namespace GoPlayProj.Processors.Services;

public class ServerClientProvider
{
    private static Client<WsClient>? s_client;
    private IConfiguration Configuration;
    
    public ServerClientProvider(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    
    public async Task<Client<WsClient>?> GetClient(TimeSpan timeout)
    {
        if (s_client?.IsConnected ?? false) return s_client;
        
        var host = Configuration["GameServer:Host"]!;
        var port = Configuration.GetValue<int>("GameServer:Port");

        s_client = new Client<WsClient>
        {
            ServerTag = ServerTag.BackEnd,
            // RequestTimeout = TimeSpan.MaxValue,
        };
        s_client.RegisterFilter(new AdminSignFilter());
        s_client.OnError += err =>
        {
            Console.WriteLine($@"========== Client Error ==========
{err}
==================================
");
        };

        if (!await s_client.Connect(host, port, timeout))
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zz}] Connect Failed!");
            return null;
        }

        return s_client;
    }
}