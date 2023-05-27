using System;
using System.Net.Sockets;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services.Core.Interfaces
{
    public interface IFilterable
    {
        void RegisterFilter(IFilter filter);
        void UnregisterFilter(IFilter filter);
        bool IsBlockRecvByFilter(Package pack);
        bool IsBlockSendByFilter(Package pack);
        void PostRecvFilter(Package pack);
        void PostSendFilter(Package pack);

        void ErrorFilter(uint clientId, Exception err);
    }
    
    public interface IFilter
    {
        /// <summary>
        /// 注册时
        /// </summary>
        void OnRegistered(IFilterable filterable);

        void OnClientConnected(uint clientId);
        void OnClientDisconnected(uint clientId);
        
        /// <summary>
        /// C -> S
        /// </summary>
        /// <param name="pack"></param>
        /// <returns>是否拦截，true表示拦截，即不会真的发给服务器</returns>
        bool OnPreSend(Package pack);
    
        void OnPostSend(Package pack);
        
        /// <summary>
        /// S -> C
        /// </summary>
        /// <param name="pack"></param>
        /// <returns>是否拦截，true表示拦截，即不会分发事件</returns>
        bool OnPreRecv(Package pack);
        void OnPostRecv(Package pack);
        
        void OnError(uint clientId, Exception err);
    }
}