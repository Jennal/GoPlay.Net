using System;
using System.Linq;
using GoPlay.Core.Protocols;

namespace GoPlay
{
    public partial class Client<T>
    {
        protected RespHandShake m_handshake;
        protected DateTime m_sendHandshakeTime = DateTime.MaxValue;
        
        protected void SendHandShake()
        {
            var pack = Package.Create(0, new ReqHankShake
            {
                ClientVersion = string.IsNullOrEmpty(ClientVersion) ? "GoPlay Client/0.1" : ClientVersion,
                ServerTag = ServerTag,
                AppKey = string.Empty,
            }, PackageType.HankShakeReq, EncodingType);
            PreSendHandShakeFilter(pack);
            Send(pack);
            m_sendHandshakeTime = DateTime.UtcNow;
        }

        public uint GetRouteId(string route)
        {
            // 区分两种失败：握手尚未完成(路由表还没下发) vs 路由表里确实没有该路由。
            // 前者通常是调用方没等 Connect 返回 true 就发请求(连接未就绪)，给出明确提示，
            // 避免被误诊成"路由不存在"。
            if (m_handshake?.Routes == null)
            {
                throw new Exception($"Client: handshake not completed, route table unavailable (connection not ready): {route}");
            }

            if (!m_handshake.Routes.ContainsKey(route)) throw new Exception($"Client: route not exists: {route}");
            return m_handshake.Routes[route];
        }

        public string GetRouteById(uint routeId)
        {
            var item = m_handshake.Routes.FirstOrDefault(o => o.Value == routeId);
            return item.Key;
        }

        public override string GetRoute(Package pack)
        {
            var routeId = pack.Header.PackageInfo.Route;
            return GetRouteById(routeId);
        }
        
        private void ResolveHandShake(Package packRaw)
        {
            var pack = Package.ParseFromRaw<RespHandShake>(packRaw);
            m_handshake = pack.Data;
        }
    }
}