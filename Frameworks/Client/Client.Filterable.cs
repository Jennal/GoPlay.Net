using System;
using System.Collections.Generic;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;

namespace GoPlay
{
    public partial class Client<T> : IFilterable
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
        
        public void RegisterFilter(IFilter filter)
        {
            filter.OnRegistered(this);
            m_filters.Add(filter);
        }

        public void UnregisterFilter(IFilter filter)
        {
            m_filters.Remove(filter);
        }

        public bool IsBlockRecvByFilter(Package pack)
        {
            var result = false;
            foreach (var filter in m_filters)
            {
                result = result || filter.OnPreRecv(pack);
            }

            return result;
        }
        
        public bool IsBlockSendByFilter(Package pack)
        {
            var result = false;
            foreach (var filter in m_filters)
            {
                result = result || filter.OnPreSend(pack);
            }

            return result;
        }
        
        public void PreSendHandShakeFilter(Package pack) 
        {
            foreach (var filter in m_filters)
            {
                filter.OnPreSend(pack);
            }
        }
        
        public void PostRecvFilter(Package pack)
        {
            foreach (var filter in m_filters)
            {
                filter.OnPostRecv(pack);
            }
        }

        public void PostSendFilter(Package pack)
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