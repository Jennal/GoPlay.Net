using Google.Protobuf;

namespace GoPlay
{
    public interface ISessionManager
    {
        void OnClientConnect(uint clientId);
        void OnClientDisconnect(uint clientId);
        
        T Get<T>(uint clientId, string key)
            where T : class, IMessage;

        void Set<T>(uint clientId, string key, T value)
            where T : class, IMessage;

        void Remove(uint clientId, string key);

        Dictionary<string, IMessage> GetAll(uint clientId);

        Dictionary<string, IMessage> GetAllPrefix(string prefix);

        Dictionary<string, IMessage> GetAllSuffix(string suffix);
    }
}