using System.Collections.Concurrent;
using System.Drawing;
using System.Text;
using Colorful;
using Newtonsoft.Json;
using GoPlay;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;
using GoPlay.Core.Routers;
using Console = Colorful.Console;

namespace GoPlayProj.Main;

public class LoggerFilter : IFilter
{
    protected Server _server;
    protected ConcurrentDictionary<uint, RespHandShake> _handShakes = new ConcurrentDictionary<uint, RespHandShake>();
    protected ConcurrentDictionary<string, DateTime> _processTime = new ConcurrentDictionary<string, DateTime>();
    protected ConcurrentDictionary<uint, byte> _clients = new ConcurrentDictionary<uint, byte>();

    protected int ClientCount => _clients.Count;
    
    public void OnRegistered(IFilterable filterable)
    {
        _server = filterable as Server;
    }

    public void OnClientConnected(uint clientId)
    {
        _clients.TryAdd(clientId, 1);
        var ip = _server.GetClientIp(clientId);
        Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Client Connect[{clientId}/{ClientCount}]: {ip}");
    }

    public void OnClientDisconnected(uint clientId)
    {
        _clients.TryRemove(clientId, out _);
        var ip = _server.GetClientIp(clientId);
        Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Client Disconnect[{clientId}/{ClientCount}]: {ip}");
        _handShakes.TryRemove(clientId, out _);
    }

    public bool OnPreSend(Package pack)
    {
        if (pack.Header.PackageInfo.Type == PackageType.Ping ||
            pack.Header.PackageInfo.Type == PackageType.Pong) return false;
        
        if (pack.Header.PackageInfo.Type == PackageType.HankShakeResp)
        {
            var p = pack as Package<RespHandShake>;
            _handShakes[p.Header.ClientId] = p.Data;
        }
        
        var clientId = pack.Header.ClientId.ToString();
        var reqId = pack.Header.PackageInfo.Id;
        var userId = GetUserId(pack);
        var routeStr = GetRouteStr(pack);
        var data = GetData(pack);
        
        var timeStr = "";
        var key = GetTimeKey(clientId, reqId, routeStr);
        if (_processTime.TryRemove(key, out var startTime))
        {
            var ts = DateTime.UtcNow.Subtract(startTime);
            timeStr = $"({ts.TotalMilliseconds:F2} ms)";
        }
        
        var args = new Formatter[]
        {
            new(userId, Color.LightGreen),
            new(routeStr, Color.Pink),
            new(timeStr, Color.Coral),
            new(pack.Header.Status, Color.Aquamarine),
            new(data, Color.Aqua),
        };
            
        Console.WriteLineFormatted($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Send[CID:{clientId}][RID:{reqId}]" + "[{0}][{1}]{2} => {3} | {4}", Color.White, args);

        return false;
    }

    public void OnPostSend(Package pack)
    {
    }

    public bool OnPreRecv(Package pack)
    {
        if (pack.Header.PackageInfo.Type == PackageType.Ping ||
            pack.Header.PackageInfo.Type == PackageType.Pong) return false;
        
        var clientId = pack.Header.ClientId.ToString();
        var reqId = pack.Header.PackageInfo.Id;
        var userId = GetUserId(pack);
        var routeStr = GetRouteStr(pack);
        var data = GetData(pack);

        var args = new Formatter[]
        {
            new(userId, Color.LightGreen),
            new(routeStr, Color.Pink),
            new(data, Color.Fuchsia),
        };
        Console.WriteLineFormatted($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] Recv[CID:{clientId}][RID:{reqId}]" + "[{0}][{1}] <= {2}", Color.LightGray, args);

        var key = GetTimeKey(clientId, reqId, routeStr);
        _processTime[key] = DateTime.UtcNow;
        return false;
    }

    private static string GetTimeKey(string clientId, uint reqId, string routeStr)
    {
        return $"{clientId}_{reqId}_{routeStr}";
    }

    public void OnPostRecv(Package pack)
    {
    }

    public void OnError(uint clientId, Exception err)
    {
        var args = new Formatter[]
        {
            new(err.Message, Color.Red),
            new(err.StackTrace, Color.DeepPink),
        };
        Console.WriteLineFormatted($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}] ERROR[CID:{clientId}]" + "\n{0}\n[{1}", Color.LightGray, args);
    }

    private string GetUserId(Package pack)
    {
        var userId = _server.SessionManager.Get<PbInt>(pack.Header.ClientId, Consts.SessionKeys.UserId);
        if (userId == null) return "GUEST";
        return $"{userId.Value}";
    }

    private string GetRouteStr(Package pack)
    {
        if (pack.Header.PackageInfo.Type == PackageType.HankShakeReq) return "HankShakeReq";
        if (pack.Header.PackageInfo.Type == PackageType.HankShakeResp) return "HankShakeResp";

        if (_handShakes.ContainsKey(pack.Header.ClientId))
        {
            var handShake = _handShakes[pack.Header.ClientId];
            var routeInfo = handShake.Routes.FirstOrDefault(o => o.Value == pack.Header.PackageInfo.Route);
            if (!string.IsNullOrEmpty(routeInfo.Key)) return routeInfo.Key;
        }
        
        var route = GetRoute(pack);
        if (route != null) return route.RouteString;

        return $"UNKNOWN_ROUTE({pack.Header.PackageInfo.Route})";
    }

    private Route? GetRoute(Package pack)
    {
        foreach (var processor in _server.Processors)
        {
            if (!processor.IsRecognizeRoute(pack.Header.PackageInfo.Route)) continue;

            return processor.GetRoute(pack);
        }

        return null;
    }

    private string GetData(Package pack)
    {
        var route = GetRoute(pack);
        if (route != null && !pack.IsChunk && (pack.Header.PackageInfo.Type == PackageType.Notify || pack.Header.PackageInfo.Type == PackageType.Request))
        {
            foreach (var paramInfo in route.Method.GetParameters())
            {
                if (paramInfo.ParameterType == typeof(Header)) continue;

                //result = Package.ParseFromRaw<paramInfo.ParameterType>(package);
                var method = Route.GetParseFromRawMethod(paramInfo.ParameterType);
                var result = method.Invoke(null, new object[] { pack });

                //field = result.Data;
                var fieldInfo = Route.GetDataField(paramInfo.ParameterType);
                var field = fieldInfo.GetValue(result);

                var json = JsonConvert.SerializeObject(field);
                return json;
            }
        }

        return GetByteArray(pack);
    }
    
    private string GetByteArray(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return "byte[] {}";
        
        var sb = new StringBuilder("byte[] { ");
        for (var i = 0; i < Math.Min(200, bytes.Length); i++)
        {
            var b = bytes[i];
            sb.Append(b);
            if (i != bytes.Length - 1) sb.Append(", ");
        }

        if (bytes.Length > 200) sb.Append("... ");

        sb.Append(" }");
        return sb.ToString();
    }

    private string GetByteArray(Package pack)
    {
        var fieldInfo = pack.GetType().GetField("Data");
        if (fieldInfo == null) return GetByteArray(pack.RawData);

        var data = fieldInfo.GetValue(pack);
        var json = JsonConvert.SerializeObject(data);
        return json;
    }
}