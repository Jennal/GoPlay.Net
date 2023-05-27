using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Services.Core.Transports;
using TcpClient = NetCoreServer.TcpClient;

namespace GoPlay.Services.Core.Transport.NetCoreServer
{
    class PackClient : TcpClient
    {
        public BlockingCollection<byte[]> RecvChannel = new BlockingCollection<byte[]>(byte.MaxValue);
        public SocketError LastError;

        private CancellationToken m_token;
        private byte[] m_buffer;
        
        public PackClient(IPAddress address, int port, CancellationToken token) : base(address, port)
        {
            m_token = token;
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
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
                        RecvChannel.Add(packData, m_token);
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

        protected override void Dispose(bool disposingManagedResources)
        {
            base.Dispose(disposingManagedResources);
            RecvChannel.Dispose();
        }
    }
    
    public class NcClient : TransportClientBase
    {
        private PackClient m_client;
        private CancellationTokenSource m_cancelSource;
        
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
                    m_client = new PackClient(address, port, m_cancelSource.Token);

                    m_client.ConnectAsync();
                    var startTime = DateTime.UtcNow;
                    while (!m_client.IsConnected)
                    {
                        Thread.Yield();
                        var ts = DateTime.UtcNow.Subtract(startTime);
                        if (ts > timeout)
                        {
                            Console.WriteLine($"Connect timeout: {ts}");
                            throw new Exception("Connect timeout!");
                        }
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
            if (m_cancelSource == null) return;
            
            m_cancelSource.Cancel();
            
            m_client.Dispose();
            m_client = null;
        }

        public override ValueTask<byte[]> Recv(CancellationTokenSource cancelSource)
        {
            return new ValueTask<byte[]>(m_client.RecvChannel.Take(cancelSource.Token));
        }

        public override ValueTask Send(byte[] data, CancellationTokenSource cancelSource)
        {
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)data.Length);
                    bw.Write(data);
                    
                    m_client.SendAsync(ms.ToArray());
                }
            }
            
            return new ValueTask();
        }

        public override void Dispose()
        {
            m_client?.Dispose();
            m_cancelSource?.Dispose();
        }
    }
}