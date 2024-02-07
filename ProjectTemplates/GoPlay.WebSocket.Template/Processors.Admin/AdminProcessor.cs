using GoPlay.Core.Attributes;
using GoPlay.Core.Protocols;
using GoPlayProj.Utils;

namespace GoPlayProj.Processors;

[ServerTag(Tag = ServerTag.BackEnd)]
[Processor("admin")]
public partial class AdminProcessor : GoPlayProjProcessor
{
    public override string[] Pushes => Array.Empty<string>();

    public override Package? OnPreRecv(Package pack)
    {
        //判断有效客户端
        if (!IsValidAdminClient(pack.Header))
        {
            var header = pack.Header.Clone();
            header.ClientId = pack.Header.ClientId;
            header.PackageInfo.Type = PackageType.Response;
            header.Status = new Status
            {
                Code = StatusCode.Failed,
                Message = "INVALID_APP_KEY",
            };
            return new Package
            {
                Header = header
            };
        }

        //判断是否已经登录
        var result = base.OnPreRecv(pack);
        if (result != null) return result;
        
        return null;
    }

    protected override bool IsLogined(Header header)
    {
        var adminId = GetAdminId(header);
        return adminId > 0;
    }

    private bool IsValidAdminClient(Header header)
    {
        // Console.WriteLine($"Header={JsonConvert.SerializeObject(header, Formatting.Indented)}");
        if (header.Session == null) return false;
        if (header.Session.Guid != AdminConsts.AppKey) return false;
        if (!header.Session.Values.ContainsKey(AdminConsts.SESS_SIGN)) return false;
        if (!header.Session.Values[AdminConsts.SESS_SIGN].TryUnpack<PbString>(out var clientSign)) return false;

        var route = GetRoute(header);
        var sign = Sign(route.RouteString, header).ToLower();
        var clientSignStr = clientSign.Value.ToLower();
        if (sign != clientSignStr)
        {
            // Console.WriteLine($"{route} => {sign} != {clientSign.Value}");
            return false;
        }

        return true;
    }

    public static string Sign(string route, Header header)
    {
        var content = $"{route}.{header.PackageInfo.ContentSize}.{AdminConsts.AppKey}";
        // Console.WriteLine($"content = {content}");
        return Md5Utils.Encode(content);
    }
    
    protected virtual int GetAdminId(Header header)
    {
        return GetAdminId(header.ClientId);
    }

    protected virtual int GetAdminId(uint clientId)
    {
        var userId = SessionManager.Get<PbInt>(clientId, Consts.SessionKeys.AdminId);
        return userId?.Value ?? Consts.Id.INVALID_ADMIN_ID;
    }

    protected virtual void SetAdminId(uint clientId, int userId)
    {
        SessionManager.Set(clientId, Consts.SessionKeys.AdminId, new PbInt
        {
            Value = userId
        });
    }

    protected virtual void RemoveAdminId(uint clientId)
    {
        SessionManager.Remove(clientId, Consts.SessionKeys.AdminId);
    }

    protected virtual string[] GetAdminPermits(uint clientId)
    {
        var permits = SessionManager.Get<PbStringArray>(clientId, Consts.SessionKeys.AdminPermits);
        return permits?.Value.ToArray() ?? Array.Empty<string>();
    }
    
    protected virtual void SetAdminPermits(uint clientId, string[] permits)
    {
        SessionManager.Set(clientId, Consts.SessionKeys.AdminPermits, new PbStringArray
        {
            Value = { permits }
        });
    }
    
    protected virtual void RemoveAdminPermits(uint clientId)
    {
        SessionManager.Remove(clientId, Consts.SessionKeys.AdminPermits);
    }
}
