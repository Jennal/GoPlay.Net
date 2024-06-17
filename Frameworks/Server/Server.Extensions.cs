using GoPlay.Core.Interfaces;
using GoPlay.Core.Transports;

namespace GoPlay;

public static class Server_Extensions
{
    public static void AddStaticContent<T>(this Server<T> server, string path, string prefix = "/", string filter = "*.*",
        TimeSpan? timeout = null)
        where T : TransportServerBase, IAddStaticContent, new()
    {
        server.Transport.AddStaticContent(path, prefix, filter, timeout);
    }
    
    public static string GetClientBrowser<T>(this Server<T> server, uint clientId)
        where T : TransportServerBase, IGetClientBrowser, new()
    {
        return server.Transport.GetClientBrowser(clientId);
    }
    
    public static Dictionary<string, string> GetHttpHeaders<T>(this Server<T> server, uint clientId)
        where T : TransportServerBase, IGetHttpHeader, new()
    {
        return server.Transport.GetHttpHeaders(clientId);
    }
    
    public static string GetHttpHeader<T>(this Server<T> server, uint clientId, string key)
        where T : TransportServerBase, IGetHttpHeader, new()
    {
        return server.Transport.GetHttpHeader(clientId, key);
    }
    
    public static bool HasHttpHeader<T>(this Server<T> server, uint clientId, string key)
        where T : TransportServerBase, IGetHttpHeader, new()
    {
        return server.Transport.HasHttpHeader(clientId, key);
    }
}