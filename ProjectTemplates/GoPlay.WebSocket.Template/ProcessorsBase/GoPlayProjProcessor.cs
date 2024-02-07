using GoPlay;
using Microsoft.Extensions.Configuration;
using GoPlayProj.Extensions;
using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.Ws;
using GoPlay.Core.Transport.Wss;
using StackExchange.Redis;
using GoPlayProj.Database;

namespace GoPlayProj;

public abstract partial class GoPlayProjProcessor : ProcessorBase
{
    public virtual T GetService<T>()
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public virtual ContextScope GetDbContextScope()
    {
        return ServiceProvider.GetContextScope();
    }

    public virtual Task<IDatabase> GetRedis()
    {
        return ServiceProvider.GetRedis();
    }
    
    protected virtual IConfiguration GetConfiguration()
    {
        return ServiceProvider.GetRequiredService<IConfigurationRoot>();
    }

    protected virtual string? GetConfigurationValue(string key)
    {
        return GetConfiguration()[key];
    }

    protected string? GetWechatAppId()
    {
        return GetConfigurationValue("Wechat:AppId");
    }

    protected string? GetWechatAppSecret()
    {
        return GetConfigurationValue("Wechat:AppSecret");
    }

    protected string GetBscMemo()
    {
        return GetConfigurationValue("HDWallet:Bsc:Memo") ?? "";
    }
    
    protected string GetBscPass()
    {
        return GetConfigurationValue("HDWallet:Bsc:Pass") ?? "";
    }
    
    protected string GetTronMemo()
    {
        return GetConfigurationValue("HDWallet:Tron:Memo") ?? "";
    }
    
    protected string GetTronPass()
    {
        return GetConfigurationValue("HDWallet:Tron:Pass") ?? "";
    }
    
    protected string[] GetBscApiKeys()
    {
        var configure = GetConfiguration();
        var section = configure.GetSection("BlockChainAPIKey:Bsc");
        return section.Get<string[]>() ?? Array.Empty<string>();
    }
    
    protected string[] GetTronApiKeys()
    {
        var configure = GetConfiguration();
        var section = configure.GetSection("BlockChainAPIKey:Tron");
        return section.Get<string[]>() ?? Array.Empty<string>();
    }
    
    public override Package? OnPreRecv(Package pack)
    {
        var route = GetRoute(pack);
        var attrs = route.Method.GetCustomAttributes(typeof(BeforeLoginAttribute), true);
        if (attrs.Any()) return null;

        if (!IsLogined(pack.Header))
        {
            var header = pack.Header.Clone();
            header.ClientId = pack.Header.ClientId;
            header.PackageInfo.Type = PackageType.Response;
            header.Status = new Status
            {
                Code = StatusCode.Failed,
                Message = Consts.ErrCode.LOGIN_REQUIRED,
            };
            return new Package
            {
                Header = header
            };
        }

        return null;
    }

    protected virtual bool IsLogined(Header header)
    {
        var userId = GetUserId(header);
        return userId > 0;
    }

    protected virtual uint GetClientId(int userId)
    {
        return SessionManager.GetClientId(userId);
    }
    
    protected virtual List<uint> GetClientIds(int userId)
    {
        return SessionManager.GetClientIds(userId);
    }
    
    protected virtual uint GetClientIdByToken(string token)
    {
        return SessionManager.GetClientIdByToken(token);
    }
    
    protected virtual int GetUserIdByToken(string token)
    {
        var clientId = GetClientIdByToken(token);
        if (clientId == Consts.Id.INVALID_CLIENT_ID) return Consts.Id.INVALID_USER_ID;

        return GetUserId(clientId);
    }
    
    protected virtual int GetUserId(Header header)
    {
        return GetUserId(header.ClientId);
    }

    protected virtual int GetUserId(uint clientId)
    {
        var userId = SessionManager.Get<PbInt>(clientId, Consts.SessionKeys.UserId);
        return userId?.Value ?? Consts.Id.INVALID_USER_ID;
    }

    protected virtual void SetUserId(uint clientId, int userId)
    {
        SessionManager.Set(clientId, Consts.SessionKeys.UserId, new PbInt
        {
            Value = userId
        });
    }

    protected virtual void RemoveUserId(uint clientId)
    {
        SessionManager.Remove(clientId, Consts.SessionKeys.UserId);
    }
    
    protected virtual string GetClientAgent(uint clientId)
    {
        var handShake = SessionManager.Get<ReqHankShake>(clientId, nameof(ReqHankShake));
        return handShake?.ClientVersion ?? "";
    }
    
    protected virtual string GetClientBrowser(uint clientId)
    {
        if (Server is Server<WsServer> s)
        {
            return s.GetClientBrowser(clientId);
        }

        if (Server is Server<WssServer> s2)
        {
            return s2.GetClientBrowser(clientId);
        }

        return string.Empty;

    }

    protected virtual string GetClientIp(uint clientId)
    {
        return Server.GetClientIp(clientId);
    }
    
    protected virtual string GetClientVersion(uint clientId)
    {
        try
        {
            var clientVersion = GetClientAgent(clientId);
            var arr = clientVersion.Split(";");
            if (arr.Length < 2) return "";

            arr = arr[1].Split("/");
            if (arr.Length < 2) return "";

            return arr[1];
        }
        catch
        {
            return "";
        }
    }
    
    protected virtual void PushByUserId<T>(string route, int userId, T data)
    {
        foreach (var clientId in GetClientIds(userId))
        {
            Push(route, clientId, data);
        }
    }
    
    protected virtual void PushToOtherByUserId<T>(Header header, string route, int userId, T data)
    {
        foreach (var clientId in GetClientIds(userId))
        {
            if (clientId == header.ClientId) continue;
            Push(route, clientId, data);
        }
    }
    
    public override void OnPostSendResult(Package pack)
    {
    }

    public override async void OnBroadcast(uint clientId, int eventId, object data)
    {
        try
        {
            var evt = (EventId)eventId;
            var userId = GetUserId(clientId);
            if (userId <= Consts.Id.INVALID_USER_ID) return;

            switch (evt)
            {
                case EventId.LoginSuccess:
                    var pack = data as Package;
                    await OnLoginSuccess(pack!.Header, userId);
                    break;
                case EventId.LogoutSuccess:
                    await OnLogoutSuccess(clientId, userId);
                    break;
                default:
                    break;
            }
        }
        catch (Exception err)
        {
            Server.OnErrorEvent(clientId, err);
        }
    }

    protected virtual Task OnLoginSuccess(Header header, int userId)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnLogoutSuccess(uint clientId, int userId)
    {
        return Task.CompletedTask;
    }
}