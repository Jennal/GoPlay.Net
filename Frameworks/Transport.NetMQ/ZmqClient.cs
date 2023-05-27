using System;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace GoPlay.Services.Core.Transports.ZMQ
{
    public class ZmqClient : TransportClientBase
    {
        protected string m_connectionString;
        protected ClientSocket m_socket;

        public override void Connect(string host, int port, TimeSpan timeout)
        {
            m_connectionString = $"tcp://{host}:{port}";
            m_socket = new ClientSocket();
            m_socket.Connect(m_connectionString);
        }

        public override void Disconnect()
        {
            m_socket.Disconnect(m_connectionString);
            m_socket.Close();
            m_socket.Dispose();
            m_socket = null;
        }

        public override ValueTask<byte[]> Recv(CancellationTokenSource cancelSource)
        {
            return m_socket.ReceiveBytesAsync();
        }

        public override ValueTask Send(byte[] data, CancellationTokenSource cancelSource)
        {
            return m_socket.SendAsync(data);
        }

        public override void Dispose()
        {
            m_socket?.Dispose();
        }
    }
}