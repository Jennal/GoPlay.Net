using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NetCoreServer;
using GoPlay.Services.Core.Protocols;
using GoPlay.Services.Core.Transports;

namespace GoPlay.Services.Core.Transport.NetCoreServer
{
    class HttpPackSession : HttpSession
    {
        public uint ClientId { get; }
        public string ClientIP;
        public HttpPackServer PackServer => Server as HttpPackServer;

        public HttpPackSession(HttpPackServer server, uint clientId) : base(server)
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

        protected override void OnReceivedRequest(HttpRequest request)
        {
            if (request.Method != "POST")
            {
                SendResponseAsync(Response.MakeOkResponse(404));
                return;
            }

            if (request.Url != HttpConsts.POST_URL)
            {
                SendResponseAsync(Response.MakeOkResponse(404));
                return;
            }
            
            var data = Convert.FromBase64String(request.Body);
            PackServer.OnRecv(this, data);
       }

        protected override void OnReceivedRequestError(HttpRequest request, string error)
        {
            Console.WriteLine($"Request error: {error}");
            PackServer.m_ncServer.OnClientError(ClientId, new Exception(error));
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Session[{ClientId}] caught an error with code {error}");
        }
    }

    class HttpPackServer : global::NetCoreServer.HttpServer
    {
        internal HttpServer m_ncServer;
        private IdLoopGenerator m_idGen = new IdLoopGenerator(uint.MaxValue);
        private ConcurrentDictionary<uint, HttpPackSession> m_sessions = new ConcurrentDictionary<uint, HttpPackSession>();
        private BlockingCollection<(uint, byte[])> m_readChannel = new BlockingCollection<(uint, byte[])>(ushort.MaxValue);

        public HttpPackServer(HttpServer server, IPAddress address, int port) : base(address, port)
        {
            m_ncServer = server;
        }

        protected override TcpSession CreateSession() { return new HttpPackSession(this, m_idGen.Next()); }

        protected override void OnConnected(TcpSession session)
        {
            if (session is HttpPackSession packSession == false) return;
            
            m_sessions[packSession.ClientId] = packSession;
            m_ncServer.OnConnected(packSession.ClientId);
        }

        protected override void OnDisconnected(TcpSession session)
        {
            if (session is HttpPackSession packSession == false) return;

            m_ncServer.OnDisconnected(packSession.ClientId);
            m_sessions.TryRemove(packSession.ClientId, out _);
        }

        internal void OnRecv(HttpPackSession session, byte[] data)
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

            var text = Convert.ToBase64String(data);
            session.SendResponse(session.Response.MakeGetResponse(text));
        }

        public bool GetSession(uint clientId, out HttpPackSession session)
        {
            return m_sessions.TryGetValue(clientId, out session);
        }
    }
    
    public class HttpServer : TransportServerBase
    {
        private HttpPackServer m_server;
        private CancellationTokenSource m_cancelSource;

        internal CancellationToken CancellationToken => m_cancelSource.Token;
        
        public override void Start(string host, int port, CancellationTokenSource cancelSource = null)
        {
            m_cancelSource = cancelSource ?? new CancellationTokenSource();
            
            var address = host == "*" ? IPAddress.Any : IPAddress.Parse(host);
            m_server = new HttpPackServer(this, address, port);
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