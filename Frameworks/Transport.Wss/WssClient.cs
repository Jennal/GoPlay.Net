using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Core.Transports;
using NetCoreServer;

namespace GoPlay.Core.Transport.Wss
{
    class PackClient : NetCoreServer.WssClient
    {
        private SocketError LastError;
        
        private WssClient m_client;
        private CancellationToken m_token;
        private byte[] m_buffer;
        private string m_host;
        private int m_port;
        private bool m_isUpgraded;
        private bool m_wsConnected;
        
        public bool IsUpgraded => m_isUpgraded;
        public bool WsConnected => m_wsConnected;
        
        public PackClient(WssClient client, SslContext context, string host, IPAddress address, int port, CancellationToken token) : base(context, address, port)
        {
            m_client = client;
            m_host = host;
            m_port = port;
            m_token = token;
            m_isUpgraded = false;
            m_wsConnected = false;
        }

        public override void OnWsConnecting(HttpRequest request)
        {
            request.SetBegin("GET", "/");
            request.SetHeader("Host", m_host);
            request.SetHeader("Origin", $"https://{m_host}:{m_port}");
            request.SetHeader("Upgrade", "websocket");
            request.SetHeader("Connection", "Upgrade");
            request.SetHeader("Sec-WebSocket-Key", Convert.ToBase64String(WsNonce));
            request.SetHeader("Sec-WebSocket-Protocol", "GoPlay.Net");
            request.SetHeader("Sec-WebSocket-Version", "13");
            request.SetBody();

            m_isUpgraded = true;
        }

        public override void OnWsConnected(HttpResponse response)
        {
            // Console.WriteLine($"WebSocket client connected a new session with Id {Id}");
            m_wsConnected = true;
            m_client.InvokeOnConnected();
        }

        public override void OnWsDisconnected()
        {
            // Console.WriteLine($"WebSocket client disconnected a session with Id {Id}");
            m_wsConnected = false;
            m_client.InvokeOnDisconnected();
        }
        
        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            var data = new ReadOnlySpan<byte>(buffer, (int)offset, (int)size);
            if (m_buffer == null)
            {
                m_buffer = data.ToArray();
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    ms.Write(m_buffer);
                    ms.Write(data);
                    m_buffer = ms.ToArray();
                }
            }

            using (var ms = new MemoryStream(m_buffer))
            {
                using (var br = new BinaryReader(ms))
                {
                    while (ms.Position < ms.Length)
                    {
                        if (ms.Length - ms.Position < sizeof(ushort)) break;

                        var lastPos = ms.Position;
                        var len = br.ReadUInt16();
                        if (ms.Length - ms.Position < len)
                        {
                            ms.Seek(lastPos, SeekOrigin.Begin);
                            break;
                        }

                        var packData = br.ReadBytes(len);
                        m_client.OnRecv(packData);
                    }

                    data = new ReadOnlySpan<byte>(m_buffer, (int)ms.Position, (int)(ms.Length - ms.Position));
                    m_buffer = data.ToArray();

                }
            }
        }

        protected override void OnError(SocketError error)
        {
            LastError = error;
        }
    }
    
    public class WssClient : TransportClientBase
    {
        private PackClient m_client;
        private CancellationTokenSource m_cancelSource;
        private BlockingCollection<byte[]> m_responseChannel = new BlockingCollection<byte[]>(byte.MaxValue);

        protected virtual string KeyPath => "client.pfx";
        protected virtual string KeyPass => "qwerty";

        public override bool IsConnected => m_client?.IsConnected ?? false;

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
                    var context = new SslContext(SslProtocols.Tls12, new X509Certificate2(KeyPath, KeyPass), (sender, certificate, chain, sslPolicyErrors) => true);
                    m_client = new PackClient(this, context, host, address, port, m_cancelSource.Token);

                    m_client.ConnectAsync();
                    var startTime = DateTime.UtcNow;
                    while (!m_client.IsConnected || !m_client.IsUpgraded || !m_client.WsConnected)
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

        internal void OnRecv(byte[] data)
        {
            m_responseChannel.Add(data, m_cancelSource.Token);
        }
        
        public override ValueTask<byte[]> Recv(CancellationTokenSource cancelSource)
        {
            return new ValueTask<byte[]>(m_responseChannel.Take(m_cancelSource.Token));
        }

        public override ValueTask Send(byte[] data, CancellationTokenSource cancelSource)
        {
            if (cancelSource.IsCancellationRequested) return new ValueTask();
            
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)data.Length);
                    bw.Write(data);
                    
                    m_client.SendBinaryAsync(ms.ToArray());
                }
            }
            
            return new ValueTask();
        }

        public override void Dispose()
        {
            m_cancelSource?.Dispose();
            m_client?.Dispose();
        }
        
        internal new void InvokeOnConnected()
        {
            base.InvokeOnConnected();
        }

        internal new void InvokeOnDisconnected()
        {
            base.InvokeOnDisconnected();
        }
    }
}