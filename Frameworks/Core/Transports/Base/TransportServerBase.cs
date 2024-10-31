using System;
using System.Threading;

namespace GoPlay.Core.Transports
{
    public abstract class TransportServerBase
    {
        public event Action<uint> OnClientConnected;
        public event Action<uint> OnClientDisconnected;
        public event Action<uint, Exception> OnError;

        public virtual bool SupportPush => true;
        
        public abstract void Start(string host, int port, CancellationTokenSource cancelSource=null);
        public abstract void Stop();

        public abstract (uint, byte[]) Recv();
        public abstract void Send(uint clientId, byte[] data);

        public abstract string GetClientIp(uint clientId);
        public abstract bool IsOnline(uint clientId);

        public abstract void DisconnectClient(uint clientId, Exception err);
        
        protected void InvokeOnClientConnected(uint clientId)
        {
            OnClientConnected?.Invoke(clientId);
        }
        
        protected void InvokeOnClientDisconnected(uint clientId)
        {
            OnClientDisconnected?.Invoke(clientId);
        }

        protected void InvokeOnError(uint clientId, Exception err)
        {
            OnError?.Invoke(clientId, err);
        }
    }
}