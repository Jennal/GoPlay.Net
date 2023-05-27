using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetCoreServer;
using GoPlay.Services.Core.Protocols;
using GoPlay.Services.Core.Transports;

namespace GoPlay.Services.Core.Transport.NetCoreServer
{
    class PackSession : TcpSession
    {
        private byte[] m_buffer;
        
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

        protected override void OnDisconnected()
        {
            /* DO NOTHING */
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
                        PackServer.OnRecv(this, packData);
                    }

                    data = new ReadOnlySpan<byte>(m_buffer, (int)ms.Position, (int)(ms.Length - ms.Position));
                    m_buffer = data.ToArray();

                }
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
        private BlockingCollection<(uint, byte[])> m_readChannel = new BlockingCollection<(uint, byte[])>(ushort.MaxValue);

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
            m_readChannel.Add((session.ClientId, data), m_ncServer.CancellationToken);
        }

        public (uint, byte[]) Recv()
        {
            return m_readChannel.Take(m_ncServer.CancellationToken);
        }

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

        public override (uint, byte[]) Recv()
        {
            return m_server.Recv();
        }

        public override void Send(uint clientId, byte[] data)
        {
            m_server.Send(clientId, data);
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