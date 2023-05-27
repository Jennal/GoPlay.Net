using System.Collections.Generic;
using GoPlay.Services.Core.Interfaces;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services
{
    public partial class Server<T>
    {
        protected List<IFilter> m_filters = new List<IFilter>();

        protected virtual void FilterOnClientConnect(uint clientId)
        {
            foreach (var filter in m_filters)
            {
                filter.OnClientConnected(clientId);
            }
        }

        protected virtual void FilterOnClientDisconnect(uint clientId)
        {
            foreach (var filter in m_filters)
            {
                filter.OnClientDisconnected(clientId);
            }
        }
        
        public override void RegisterFilter(IFilter filter)
        {
            filter.OnRegistered(this);
            m_filters.Add(filter);
        }

        public override void UnregisterFilter(IFilter filter)
        {
            m_filters.Remove(filter);
        }

        public override bool IsBlockRecvByFilter(Package pack)
        {
            var result = false;
            foreach (var filter in m_filters)
            {
                result = result || filter.OnPreRecv(pack);
            }

            return result;
        }
        
        public override bool IsBlockSendByFilter(Package pack)
        {
            var result = false;
            foreach (var filter in m_filters)
            {
                result = result || filter.OnPreSend(pack);
            }

            return result;
        }

        public override void PostRecvFilter(Package pack)
        {
            foreach (var filter in m_filters)
            {
                filter.OnPostRecv(pack);
            }
        }

        public override void PostSendFilter(Package pack)
        {
            foreach (var filter in m_filters)
            {
                filter.OnPostSend(pack);
            }
        }

        public override void ErrorFilter(uint clientId, Exception err)
        {
            foreach (var filter in m_filters)
            {
                filter.OnError(clientId, err);
            }
        }
    }
}