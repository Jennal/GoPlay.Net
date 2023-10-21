using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using GoPlay.Core.Interfaces;
using NetCoreServer;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports;

namespace GoPlay.Core.Transport.Wss
{
    class WssPackSession : WssSession
    {
        private byte[] m_buffer;
        public uint ClientId { get; }
        public string ClientIP;
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
        public WssPackServer PackServer => Server as WssPackServer;

        public WssPackSession(WssPackServer server, uint clientId) : base(server)
        {
            ClientId = clientId;
        }

        public void SetHeaders(HttpRequest request)
        {
            Headers.Clear();
            for (var i = 0; i < request.Headers; i++)
            {
                var (key, value) = request.Header(i);
                Headers.Add(key, value);
            }
        }

        public bool HasHeader(string key)
        {
            return Headers.ContainsKey(key);
        }
        
        public string GetHeader(string key)
        {
            if (Headers.TryGetValue(key, out var value)) return value;
            return string.Empty;
        }
        
        public override void OnWsConnected(HttpRequest request)
        {
            SetHeaders(request);
            if (HasHeader("X-Forwarded-For"))
            {
                ClientIP = GetHeader("X-Forwarded-For");
            }
            else if (HasHeader("X-Real-IP"))
            {
                ClientIP = GetHeader("X-Real-IP");
            }
            else
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
            
            Console.WriteLine($"WebSocket session with Id {Id} connected: {ClientIP}");
        }

        public override void OnWsDisconnected()
        {
            Console.WriteLine($"WebSocket session with Id {Id} disconnected!");
        }
        
        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            // Console.WriteLine($"WebSocket session with Id {Id} OnWsReceived:[{offset}, {size}] => {buffer}");
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

    class WssPackServer : global::NetCoreServer.WssServer
    {
        internal WssServer m_server;
        private IdLoopGenerator m_idGen = new IdLoopGenerator(uint.MaxValue);
        private ConcurrentDictionary<uint, WssPackSession> m_sessions = new ConcurrentDictionary<uint, WssPackSession>();
        private BlockingCollection<(uint, byte[])> m_readChannel = new BlockingCollection<(uint, byte[])>(ushort.MaxValue);

        public WssPackServer(WssServer server, SslContext context, IPAddress address, int port) : base(context, address, port)
        {
            m_server = server;
        }

        protected override SslSession CreateSession() { return new WssPackSession(this, m_idGen.Next()); }

        protected override void OnConnected(SslSession session)
        {
            base.OnConnected(session);
            if (session is WssPackSession packSession == false) return;
            
            m_sessions[packSession.ClientId] = packSession;
            m_server.OnConnected(packSession.ClientId);
        }

        protected override void OnDisconnected(SslSession session)
        {
            if (session is WssPackSession packSession == false) return;

            m_server.OnDisconnected(packSession.ClientId);
            m_sessions.TryRemove(packSession.ClientId, out _);
        }

        internal void OnRecv(SslSession session, byte[] data)
        {
            if (session is WssPackSession packSession == false) return;
            m_readChannel.Add((packSession.ClientId, data), m_server.CancellationToken);
        }

        public (uint, byte[]) Recv()
        {
            return m_readChannel.Take(m_server.CancellationToken);
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
                    
                    session.SendBinaryAsync(ms.ToArray());
                }
            }
        }

        public bool GetSession(uint clientId, out WssPackSession session)
        {
            return m_sessions.TryGetValue(clientId, out session);
        }
    }

    class StaticContentParams
    {
        public string path;
        public string prefix = "/";
        public string filter = "*.*";
        public TimeSpan? timeout = null;
    }
    
    public class WssServer : TransportServerBase, IAddStaticContent
    {
        private WssPackServer m_server;
        private CancellationTokenSource m_cancelSource;

        internal CancellationToken CancellationToken => m_cancelSource.Token;
        
        protected virtual string KeyPath => "server.pfx";
        protected virtual string KeyPass => "qwerty";

        private List<StaticContentParams> _staticContentParams = new List<StaticContentParams>();
        
        public override void Start(string host, int port, CancellationTokenSource cancelSource = null)
        {
            m_cancelSource = cancelSource ?? new CancellationTokenSource();
            
            var address = host == "*" ? IPAddress.Any : IPAddress.Parse(host);
            var context = new SslContext(SslProtocols.Tls12, new X509Certificate2(KeyPath, KeyPass), (sender, certificate, chain, sslPolicyErrors) => true);
            m_server = new WssPackServer(this, context, address, port);

            foreach (var param in _staticContentParams)
            {
                m_server.AddStaticContent(param.path, param.prefix, param.filter, param.timeout);
            }
            
            m_server.Start();
        }

        public override void Stop()
        {
            if (m_server == null) return;
            
            m_server.Dispose();
            m_server = null;
        }

        public void AddStaticContent(string path, string prefix = "/", string filter = "*.*", TimeSpan? timeout = null)
        {
            _staticContentParams.Add(new StaticContentParams
            {
                path = path,
                prefix = prefix,
                filter = filter,
                timeout = timeout
            });
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
            //TODO:
            //DO NOTHING ?
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