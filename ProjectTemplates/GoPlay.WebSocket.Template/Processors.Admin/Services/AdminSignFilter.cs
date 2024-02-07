using Google.Protobuf.WellKnownTypes;
using GoPlay;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.Ws;

namespace GoPlayProj.Processors.Services;

public class AdminSignFilter : IFilter
{
    private Client<WsClient> _client;
    
    public void OnRegistered(IFilterable filterable)
    {
        _client = filterable as Client<WsClient>;
    }

    public void OnClientConnected(uint clientId)
    {
    }

    public void OnClientDisconnected(uint clientId)
    {
    }

    public bool OnPreSend(Package pack)
    {
        if (pack.Header.PackageInfo.Type != PackageType.Notify && 
            pack.Header.PackageInfo.Type != PackageType.Request) return false;
        
        pack.Header.Session = new Session
        {
            Guid = AdminConsts.AppKey,
            Values = { }
        };
        var route = _client.GetRouteById(pack.Header.PackageInfo.Route);
        pack.UpdateContentSize();
        pack.Header.Session.Values[AdminConsts.SESS_SIGN] = Any.Pack(new PbString
        {
            Value = AdminProcessor.Sign(route, pack.Header)
        });
        
        return false;
    }

    public void OnPostSend(Package pack)
    {
    }

    public bool OnPreRecv(Package pack)
    {
        return false;
    }

    public void OnPostRecv(Package pack)
    {
    }

    public void OnError(uint clientId, Exception err)
    {
    }
}