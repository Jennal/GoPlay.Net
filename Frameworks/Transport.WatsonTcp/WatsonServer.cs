using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using GoPlay.Core.Protocols;
using WatsonTcp;

namespace GoPlay.Core.Transports.Watson
{
    public class WatsonServer : TransportServerBase
    {
        protected WatsonTcpServer m_server;
        protected IdLoopGenerator m_idGen = new IdLoopGenerator(uint.MaxValue);
        protected ConcurrentDictionary<uint, string> m_clientMap = new ConcurrentDictionary<uint, string>();
        protected BlockingCollection<(uint, byte[])> m_readChannel = new BlockingCollection<(uint, byte[])>();

        protected CancellationTokenSource m_cancelSource;
        
        public override void Start(string host, int port, CancellationTokenSource cancelSource = null)
        {
            m_cancelSource = cancelSource == null ? new CancellationTokenSource() : cancelSource;

            if (host == "*") host = string.Empty;
            m_server = new WatsonTcpServer(host, port);
            m_server.Events.ClientConnected += OnWatsonClientConnected;
            m_server.Events.ClientDisconnected += OnWatsonClientDisconnected;
            m_server.Events.MessageReceived += OnWatsonMessageReceived;
            m_server.Events.ExceptionEncountered += OnWatsonExceptionEncountered;
            
            m_server.Start();
        }

        private void OnWatsonClientConnected(object sender, ConnectionEventArgs e)
        {
            var clientId = m_idGen.Next();
            m_clientMap[clientId] = e.Client.IpPort;
            
            InvokeOnClientConnected(clientId);
        }

        private void OnWatsonClientDisconnected(object sender, DisconnectionEventArgs e)
        {
            var pair = m_clientMap.FirstOrDefault(o => o.Value == e.Client.IpPort);
            if (pair.Value != e.Client.IpPort) return;

            m_clientMap.TryRemove(pair.Key, out _);
            InvokeOnClientDisconnected(pair.Key);
        }

        private void OnWatsonExceptionEncountered(object sender, ExceptionEventArgs e)
        {
            InvokeOnError(IdLoopGenerator.INVALID, e.Exception);
        }

        private void OnWatsonMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var pair = m_clientMap.FirstOrDefault(o => o.Value == e.Client.IpPort);
            if (pair.Value != e.Client.IpPort) return;
            
            m_readChannel.Add((pair.Key, e.Data));
        }

        public override void Stop()
        {
            m_server.Stop();
        }

        public override (uint, byte[]) Recv()
        {
            return m_readChannel.Take(m_cancelSource.Token);
        }

        public override void Send(uint clientId, byte[] data)
        {
            if (!m_clientMap.TryGetValue(clientId, out var ipPort)) return;
            m_server.Send(ipPort, data);
        }

        public override string GetClientIp(uint clientId)
        {
            if (!m_clientMap.TryGetValue(clientId, out var ipPort)) return string.Empty;

            var arr = ipPort.Split(":".ToCharArray());
            return arr[0];
        }

        public override bool IsOnline(uint clientId)
        {
            return m_clientMap.ContainsKey(clientId);
        }

        public override void DisconnectClient(uint clientId, Exception err)
        {
            throw new NotImplementedException();
        }
    }
}