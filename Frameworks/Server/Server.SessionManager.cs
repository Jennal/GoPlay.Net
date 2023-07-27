using GoPlay.Core.Protocols;

namespace GoPlay
{
    public abstract partial class Server
    {
        public ISessionManager SessionManager = new SessionMemoryManager();
    }
    
    public partial class Server<T>
    {
        protected virtual void SessionOnClientConnect(uint clientId)
        {
            SessionManager.OnClientConnect(clientId);
        }

        protected virtual void SessionOnClientDisconnect(uint clientId)
        {
            SessionManager.OnClientDisconnect(clientId);
        }

        protected virtual void SessionOnHandShake(Package<ReqHankShake> package)
        {
            SessionManager.Set(package.Header.ClientId, nameof(ReqHankShake), package.Data);
        }
    }
}