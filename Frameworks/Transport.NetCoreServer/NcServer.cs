using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetCoreServer;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports;

namespace GoPlay.Core.Transport.NetCoreServer
{
    class PackSession : TcpSession
    {
        // ArrayPool-backed stash（同 WsPackSession），避免每次收包 ToArray。
        private byte[] m_stash;
        private int m_stashLen;
        private const int InitialStashCapacity = 4096;

        public uint ClientId { get; }
        public string ClientIP;
        public PackServer PackServer => Server as PackServer;

        public PackSession(TcpServer server, uint clientId) : base(server)
        {
            ClientId = clientId;
        }

        protected override void OnConnected()
        {
            var ep = Socket.RemoteEndPoint as IPEndPoint;
            if (ep == null)
            {
                ClientIP = "unknown";
            }
            else
            {
                ClientIP = ep.Address.ToString();    
            }
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

                PackServer.OnRecv(this, packData);
            }

            if (pos == 0) return;
            var remaining = m_stashLen - pos;
            if (remaining > 0)
            {
                System.Buffer.BlockCopy(m_stash, pos, m_stash, 0, remaining);
            }
            m_stashLen = remaining;
        }

        protected override void OnDisconnected()
        {
            if (m_stash != null)
            {
                ArrayPool<byte>.Shared.Return(m_stash);
                m_stash = null;
                m_stashLen = 0;
            }
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Session[{ClientId}] caught an error with code {error}");
        }
    }

    class PackServer : TcpServer
    {
        private NcServer m_ncServer;
        private IdLoopGenerator m_idGen = new IdLoopGenerator(uint.MaxValue);
        private ConcurrentDictionary<uint, PackSession> m_sessions = new ConcurrentDictionary<uint, PackSession>();
        // private BlockingCollection<(uint, byte[])> m_readChannel = new BlockingCollection<(uint, byte[])>(ushort.MaxValue);

        public PackServer(NcServer server, IPAddress address, int port) : base(address, port)
        {
            m_ncServer = server;
        }

        protected override TcpSession CreateSession() { return new PackSession(this, m_idGen.Next()); }

        protected override void OnConnected(TcpSession session)
        {
            if (session is PackSession packSession == false) return;
            
            m_sessions[packSession.ClientId] = packSession;
            m_ncServer.OnConnected(packSession.ClientId);
        }

        protected override void OnDisconnected(TcpSession session)
        {
            if (session is PackSession packSession == false) return;

            m_ncServer.OnDisconnected(packSession.ClientId);
            m_sessions.TryRemove(packSession.ClientId, out _);
        }

        internal void OnRecv(PackSession session, byte[] data)
        {
            m_ncServer.InvokeOnDataReceived(session.ClientId, data);
            // m_readChannel.Add((session.ClientId, data), m_ncServer.CancellationToken);
        }

        // public (uint, byte[]) Recv()
        // {
        //     return m_readChannel.Take(m_ncServer.CancellationToken);
        // }

        public void Send(uint clientId, byte[] data)
        {
            if (!GetSession(clientId, out var session)) return;

            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write((ushort)data.Length);
                    bw.Write(data);
                    
                    session.SendAsync(ms.ToArray());
                }
            }
        }

        /// <summary>
        /// 零拷贝批量发送：<paramref name="framedBytes"/> 已经是 wire bytes（含外层长度前缀），
        /// 直接 forward 到 session.SendAsync(span)。
        /// </summary>
        public void SendFramed(uint clientId, ReadOnlySpan<byte> framedBytes)
        {
            if (!GetSession(clientId, out var session)) return;
            session.SendAsync(framedBytes);
        }

        public bool GetSession(uint clientId, out PackSession session)
        {
            return m_sessions.TryGetValue(clientId, out session);
        }
    }
    
    public class NcServer : TransportServerBase
    {
        private PackServer m_server;
        private CancellationTokenSource m_cancelSource;

        internal CancellationToken CancellationToken => m_cancelSource.Token;
        
        public override void Start(string host, int port, CancellationTokenSource cancelSource = null)
        {
            m_cancelSource = cancelSource ?? new CancellationTokenSource();
            
            var address = host == "*" ? IPAddress.Any : IPAddress.Parse(host);
            m_server = new PackServer(this, address, port);
            m_server.Start();
        }

        public override void Stop()
        {
            if (m_server == null) return;
            
            m_server.Dispose();
            m_server = null;
        }

        // public override (uint, byte[]) Recv()
        // {
        //     return m_server.Recv();
        // }

        public override void Send(uint clientId, byte[] data)
        {
            m_server.Send(clientId, data);
        }

        /// <inheritdoc />
        public override System.Threading.Tasks.ValueTask SendAsync(uint clientId, ReadOnlyMemory<byte> framedBytes, System.Threading.CancellationToken ct)
        {
            m_server.SendFramed(clientId, framedBytes.Span);
            return default;
        }

        public override string GetClientIp(uint clientId)
        {
            if (!m_server.GetSession(clientId, out var session)) return string.Empty;
            
            return session.ClientIP;
        }

        public override void DisconnectClient(uint clientId, Exception err)
        {
            if (!m_server.GetSession(clientId, out var session)) return;
            session.Disconnect();
        }

        internal void OnConnected(uint clientId)
        {
            InvokeOnClientConnected(clientId);
        }
        
        internal void OnDisconnected(uint clientId)
        {
            InvokeOnClientDisconnected(clientId);
        }

        internal void OnClientError(uint clientId, Exception err)
        {
            InvokeOnError(clientId, err);
        }
    }
}