using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Core.Transports;
using TcpClient = NetCoreServer.TcpClient;

namespace GoPlay.Core.Transport.NetCoreServer
{
    class PackClient : TcpClient
    {
        private NcClient m_client;
        
        public BlockingCollection<byte[]> RecvChannel = new BlockingCollection<byte[]>(byte.MaxValue);
        public SocketError LastError;

        private CancellationToken m_token;

        private byte[] m_stash;
        private int m_stashLen;
        private const int InitialStashCapacity = 4096;
        
        public PackClient(NcClient client, IPAddress address, int port, CancellationToken token) : base(address, port)
        {
            m_client = client;
            m_token = token;
        }

        protected override void OnConnected()
        {
            m_client.InvokeOnConnected();
        }

        protected override void OnDisconnected()
        {
            if (m_stash != null)
            {
                ArrayPool<byte>.Shared.Return(m_stash);
                m_stash = null;
                m_stashLen = 0;
            }
            m_client.InvokeOnDisconnected();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            AppendToStash(buffer, (int)offset, (int)size);
            DrainStash();
        }

        private void AppendToStash(byte[] buffer, int offset, int size)
        {
            if (size <= 0) return;
            if (m_stash == null)
            {
                m_stash = ArrayPool<byte>.Shared.Rent(Math.Max(size, InitialStashCapacity));
                m_stashLen = 0;
            }
            if (m_stashLen + size > m_stash.Length)
            {
                var newCap = m_stash.Length;
                while (newCap < m_stashLen + size) newCap *= 2;
                var newBuf = ArrayPool<byte>.Shared.Rent(newCap);
                if (m_stashLen > 0) System.Buffer.BlockCopy(m_stash, 0, newBuf, 0, m_stashLen);
                ArrayPool<byte>.Shared.Return(m_stash);
                m_stash = newBuf;
            }
            System.Buffer.BlockCopy(buffer, offset, m_stash, m_stashLen, size);
            m_stashLen += size;
        }

        private void DrainStash()
        {
            var pos = 0;
            while (m_stashLen - pos >= sizeof(ushort))
            {
                var len = BinaryPrimitives.ReadUInt16LittleEndian(
                    new ReadOnlySpan<byte>(m_stash, pos, sizeof(ushort)));
                if (m_stashLen - pos - sizeof(ushort) < len) break;

                var packData = new byte[len];
                System.Buffer.BlockCopy(m_stash, pos + sizeof(ushort), packData, 0, len);
                pos += sizeof(ushort) + len;

                // socket 完成回调线程上执行，未处理异常会让进程 failfast（net8 下尤其明显）。
                // Disconnect 期间 token 被 cancel / channel 被 dispose，丢弃未消费数据即可。
                try
                {
                    if (m_token.IsCancellationRequested) return;
                    RecvChannel.Add(packData, m_token);
                }
                catch (OperationCanceledException) { return; }
                catch (ObjectDisposedException) { return; }
                catch (InvalidOperationException) { return; }
            }

            if (pos == 0) return;
            var remaining = m_stashLen - pos;
            if (remaining > 0)
            {
                System.Buffer.BlockCopy(m_stash, pos, m_stash, 0, remaining);
            }
            m_stashLen = remaining;
        }

        protected override void OnError(SocketError error)
        {
            LastError = error;
        }

        protected override void Dispose(bool disposingManagedResources)
        {
            base.Dispose(disposingManagedResources);
            if (m_stash != null)
            {
                ArrayPool<byte>.Shared.Return(m_stash);
                m_stash = null;
                m_stashLen = 0;
            }
            RecvChannel.Dispose();
        }
    }
    
    public class NcClient : TransportClientBase
    {
        private PackClient m_client;
        private CancellationTokenSource m_cancelSource;

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
                    m_client = new PackClient(this, address, port, m_cancelSource.Token);

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
            if (cancelSource.IsCancellationRequested) return new ValueTask();
            
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