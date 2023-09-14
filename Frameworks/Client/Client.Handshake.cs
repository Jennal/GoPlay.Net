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
                AppKey = string.Empty, //TODO:
            }, PackageType.HankShakeReq, EncodingType);
            Send(pack);
            m_sendHandshakeTime = DateTime.UtcNow;
        }

        public uint GetRouteId(string route)
        {
            if (!m_handshake?.Routes?.ContainsKey(route) ?? true) throw new Exception($"Client: route not exists: {route}");
            return m_handshake.Routes[route];
        }

        public string GetRouteById(uint routeId)
        {
            var item = m_handshake.Routes.FirstOrDefault(o => o.Value == routeId);
            return item.Key;
        }

        private void ResolveHandShake(Package packRaw)
        {
            var pack = Package.ParseFromRaw<RespHandShake>(packRaw);
            m_handshake = pack.Data;
        }
    }
}