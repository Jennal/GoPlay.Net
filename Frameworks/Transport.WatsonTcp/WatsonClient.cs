using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using WatsonTcp;

namespace GoPlay.Core.Transports.Watson
{
    public class WatsonClient : TransportClientBase
    {
        protected WatsonTcpClient m_client;

        protected BlockingCollection<byte[]> m_readChannel;

        protected TaskCompletionSource<bool> m_connectTask;
        protected TaskCompletionSource<bool> m_disconnectTask;

        public override bool IsConnected => m_client?.Connected ?? false;

        public override void Connect(string host, int port, TimeSpan timeout)
        {
            m_readChannel = new BlockingCollection<byte[]>();
            m_connectTask = new TaskCompletionSource<bool>();
            
            m_client = new WatsonTcpClient(host, port);
            m_client.Events.ServerConnected += OnWatsonClientConnected;
            m_client.Events.ServerDisconnected += OnWatsonClientDisconnected;
            m_client.Events.MessageReceived += OnWatsonMessageReceived;
            
            m_client.Connect();
            m_connectTask.Task.Wait();
        }

        private void OnWatsonClientConnected(object sender, ConnectionEventArgs e)
        {
            m_connectTask.SetResult(true);
            InvokeOnConnected();
        }

        private void OnWatsonClientDisconnected(object sender, DisconnectionEventArgs e)
        {
            if (m_disconnectTask != null && !m_disconnectTask.Task.IsCompleted)
            {
                //正常手动断线
                m_disconnectTask.SetResult(true);
            }
            else
            {
                InvokeOnDisconnected();
            }
        }

        private void OnWatsonMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            m_readChannel.Add(e.Data);
        }

        public override void Disconnect()
        {
            if (!m_client.Connected) return;
            
            m_disconnectTask = new TaskCompletionSource<bool>();
            m_client.Disconnect();
            m_disconnectTask.Task.Wait();
            m_disconnectTask = null;
        }

        public override ValueTask<byte[]> Recv(CancellationTokenSource cancelSource)
        {
            if (!m_client.Connected) throw new Exception("Not connected!");
            
            var data = m_readChannel.Take(cancelSource.Token);
            return new ValueTask<byte[]>(data);
        }

        public override async ValueTask Send(byte[] data, CancellationTokenSource cancelSource)
        {
            if (!m_client.Connected) throw new Exception("Not connected!");
            
            await m_client.SendAsync(data, null, 0, cancelSource.Token);
        }

        public override void Dispose()
        {
            m_client.Dispose();
        }
    }
}