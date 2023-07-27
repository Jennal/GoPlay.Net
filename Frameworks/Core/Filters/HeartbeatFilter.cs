using System;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;

namespace GoPlay.Core.Filters
{
    public class HeartbeatFilter : IFilter
    {
        public void OnRegistered(IFilterable filterable)
        {
        }

        public void OnClientConnected(uint clientId)
        {
            throw new System.NotImplementedException();
        }

        public void OnClientDisconnected(uint clientId)
        {
            throw new System.NotImplementedException();
        }

        public bool OnPreSend(Package pack)
        {
            throw new System.NotImplementedException();
        }

        public void OnPostSend(Package pack)
        {
            throw new System.NotImplementedException();
        }

        public bool OnPreRecv(Package pack)
        {
            throw new System.NotImplementedException();
        }

        public void OnPostRecv(Package pack)
        {
            throw new System.NotImplementedException();
        }

        public void OnError(uint clientId, Exception err)
        {
            throw new NotImplementedException();
        }
    }
}