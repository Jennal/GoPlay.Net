using System.Net;

namespace GoPlay.Core.Transports.TCP
{
    public static class TcpClientExtension
    {
        public static string GetIPAddress(this System.Net.Sockets.TcpClient client)
        {
            var ep = client.Client.RemoteEndPoint as IPEndPoint;
            if (ep == null) return "unknown";

            return ep.Address.ToString();
        }
    }
}