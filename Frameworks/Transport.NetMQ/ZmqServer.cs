using System;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;

namespace GoPlay.Core.Transports.ZMQ
{
    public class ZmqServer : TransportServerBase
    {
        protected string m_connectionString;
        protected ServerSocket m_socket;
        
        public override void Start(string host, int port, CancellationTokenSource cancelSource = null)
        {
            m_connectionString = $"tcp://{host}:{port}";
            m_socket = new ServerSocket();
            m_socket.Bind(m_connectionString);
        }

        public override void Stop()
        {
            m_socket.Close();
            m_socket.Dispose();
            m_socket = null;
        }

        public override (uint, byte[]) Recv()
        {
            return m_socket.ReceiveBytes();
        }

        public override void Send(uint clientId, byte[] data)
        {
            m_socket.Send(clientId, data);
        }

        public override string GetClientIp(uint clientId)
        {
            //TODO: Not Implemented!
            return string.Empty;
        }

        public override bool IsOnline(uint clientId)
        {
            return true;
        }

        public override void DisconnectClient(uint clientId, Exception err)
        {
            throw new NotImplementedException();
        }
    }
}