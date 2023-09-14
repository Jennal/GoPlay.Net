using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Core.Transports;

namespace GoPlay.Core.Transport.Http
{
    public class HttpClient : TransportClientBase
    {
        private global::NetCoreServer.HttpClientEx m_client;
        private CancellationTokenSource m_cancelSource;
        private BlockingCollection<byte[]> m_responseChannel = new BlockingCollection<byte[]>(byte.MaxValue);

        public override void Connect(string host, int port, TimeSpan timeout)
        {
            m_cancelSource = new CancellationTokenSource();
            
            var addresses = Dns.GetHostAddresses(host);
            foreach (var address in addresses)
            {
                if (address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (m_client != null) m_client.Dispose();
                
                try
                {
                    m_client = new global::NetCoreServer.HttpClientEx(address, port);

                    m_client.ConnectAsync();
                    var startTime = DateTime.UtcNow;
                    while (!m_client.IsConnected)
                    {
                        Thread.Yield();
                        var ts = DateTime.UtcNow.Subtract(startTime);
                        if (ts > timeout) throw new Exception("Connect timeout!");
                    }

                    return;
                }
                catch
                {
                    continue;
                }
            }

            throw new Exception("host can't be reached!");
        }

        public override void Disconnect()
        {
            if (m_client == null) return;
            
            m_cancelSource.Cancel();
            m_cancelSource.Dispose();
            m_cancelSource = null;
            
            m_client.Dispose();
            m_client = null;
        }

        public override ValueTask<byte[]> Recv(CancellationTokenSource cancelSource)
        {
            return new ValueTask<byte[]>(m_responseChannel.Take(m_cancelSource.Token));
        }

        public override async ValueTask Send(byte[] data, CancellationTokenSource cancelSource)
        {
            var text = Convert.ToBase64String(data);
            var response = await m_client.SendPostRequest(HttpConsts.POST_URL, text);
            m_responseChannel.Add(Convert.FromBase64String(response.Body));
        }

        public override void Dispose()
        {
            m_cancelSource?.Dispose();
            m_client?.Dispose();
        }
    }
}