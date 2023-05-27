using GoPlay.Services.Core;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services
{
    public partial class Server<T>
    {
        protected RespHandShake m_respHandShakeFrontEnd;
        protected RespHandShake m_respHandShakeBackEnd;
        
        protected virtual void InitHandShake()
        {
            m_respHandShakeFrontEnd = GetHandShake(ServerTag.FrontEnd);
            m_respHandShakeBackEnd = GetHandShake(ServerTag.BackEnd);
        }

        private RespHandShake GetHandShake(ServerTag serverTag)
        {
            var resp = new RespHandShake
            {
                ServerVersion = "GoPlay Service/0.1",
                HeartBeatInterval = (uint)Consts.HeartBeat.Interval.TotalMilliseconds,
                Routes = {},
            };

            var allRoutes = Processors.SelectMany(o => o.GetRouteIdDict());
            foreach (var route in allRoutes)
            {
                if ((route.Item3 & serverTag) != serverTag) continue;
                resp.Routes.Add(route.Item1, route.Item2);
            }

            return resp;
        }

        protected virtual void OnHandShake(Package pack)
        {
            //TODO: 判断客户端 AppKey
            var request = Package.ParseFromRaw<ReqHankShake>(pack);
            SessionOnHandShake(request);

            if ((request.Data.ServerTag & ServerTag.FrontEnd) == ServerTag.FrontEnd)
            {
                var respPack = Package.Create(0, m_respHandShakeFrontEnd, PackageType.HankShakeResp, EncodingType);
                respPack.Header.ClientId = pack.Header.ClientId;
                Send(respPack);
            }
            else if ((request.Data.ServerTag & ServerTag.BackEnd) == ServerTag.BackEnd)
            {
                var respPack = Package.Create(0, m_respHandShakeBackEnd, PackageType.HankShakeResp, EncodingType);
                respPack.Header.ClientId = pack.Header.ClientId;
                Send(respPack);
            }
        }
    }
}