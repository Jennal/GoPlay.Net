using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoPlay.Core.Transports
{
    public abstract class TransportClientBase : IDisposable
    {
        public event Action OnConnected;
        public event Action OnDisconnected; 

        public virtual void Connect(string host, int port)
        {
            Connect(host, port, Consts.TimeOut.Connect);
        }
        
        public abstract void Connect(string host, int port, TimeSpan timeout);
        public abstract void Disconnect();

        public abstract ValueTask<byte[]> Recv(CancellationTokenSource cancelSource);
        public abstract ValueTask Send(byte[] data, CancellationTokenSource cancelSource);

        public abstract void Dispose();

        protected void InvokeOnConnected()
        {
            OnConnected?.Invoke();
        }

        protected void InvokeOnDisconnected()
        {
            OnDisconnected?.Invoke();
        }
    }
}