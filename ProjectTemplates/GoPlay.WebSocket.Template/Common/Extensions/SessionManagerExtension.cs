using GoPlay;
using GoPlay.Core.Protocols;

namespace GoPlayProj.Extensions;

public static class SessionManagerExtension
{
    public static uint GetClientId(this ISessionManager sessionManager, int userId)
    {
        var items = sessionManager.GetAllPrefix(Consts.SessionKeys.UserId).Reverse();
        foreach (var item in items)
        {
            if (item.Value is PbInt pbInt && pbInt.Value == userId)
            {
                return uint.Parse(item.Key.Substring(Consts.SessionKeys.UserId.Length + 1));
            }
        }

        return Consts.Id.INVALID_CLIENT_ID;
    }
    
    public static List<uint> GetClientIds(this ISessionManager sessionManager, int userId)
    {
        var result = new List<uint>();
        var items = sessionManager.GetAllPrefix(Consts.SessionKeys.UserId);
        foreach (var item in items)
        {
            if (item.Value is PbInt pbInt && pbInt.Value == userId)
            {
                result.Add(uint.Parse(item.Key.Substring(Consts.SessionKeys.UserId.Length + 1)));
            }
        }

        return result;
    }
    
    public static uint GetClientIdByToken(this ISessionManager sessionManager, string token)
    {
        var items = sessionManager.GetAllPrefix(Consts.SessionKeys.Token).Reverse();
        foreach (var item in items)
        {
            if (item.Value is PbString pbString && pbString.Value == token)
            {
                return uint.Parse(item.Key.Substring(Consts.SessionKeys.Token.Length + 1));
            }
        }

        return Consts.Id.INVALID_CLIENT_ID;
    }
}