using System.Collections.Concurrent;
using Google.Protobuf;

namespace GoPlay.Services
{
    public class SessionMemoryManager : ISessionManager
    {
        protected ConcurrentDictionary<string, IMessage> m_sessions = new();
        
        public virtual void OnClientConnect(uint clientId)
        {
        }

        public virtual void OnClientDisconnect(uint clientId)
        {
            var idStr = clientId.ToString();
            var keys = m_sessions.Keys;
            foreach (var key in keys)
            {
                if ( ! key.EndsWith(idStr)) continue;
                m_sessions.TryRemove(key, out _);   
            }
        }

        public Dictionary<string, IMessage> GetAll(uint clientId)
        {
            return GetAllSuffix(clientId.ToString());
        }
        
        public Dictionary<string, IMessage> GetAllPrefix(string prefix)
        {
            return GetAll(key => key.StartsWith(prefix));
        }
        
        public Dictionary<string, IMessage> GetAllSuffix(string suffix)
        {
            return GetAll(key => key.EndsWith(suffix));
        }
        
        public Dictionary<string, IMessage> GetAll(Func<string, bool> filter)
        {
            var dict = new Dictionary<string, IMessage>();
            var keys = m_sessions.Keys;
            foreach (var key in keys)
            {
                if (!filter(key)) continue;
                if (!m_sessions.TryGetValue(key, out var val)) continue;

                dict[key] = val;
            }

            return dict;
        }

        public T Get<T>(uint clientId, string key)
            where T : class, IMessage
        {
            key = GetKey(clientId, key);
            if (m_sessions.TryGetValue(key, out var val)) return val as T;
            return null;
        }
       
        public void Set<T>(uint clientId, string key, T value)
            where T : class, IMessage
        {
            key = GetKey(clientId, key);
            m_sessions.AddOrUpdate(key, k => value, (k, oldVal) => value);
        }

        public void Remove(uint clientId, string key)
        {
            key = GetKey(clientId, key);
            m_sessions.TryRemove(key, out _);
        }

        protected string GetKey(uint clientId, string key)
        {
            return $"{key}_{clientId}";
        }
    }
}