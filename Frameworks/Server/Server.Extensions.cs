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
}