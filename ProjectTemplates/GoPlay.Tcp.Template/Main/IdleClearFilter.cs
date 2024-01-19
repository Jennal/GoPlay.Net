using System.Collections.Concurrent;
using System.Drawing;
using System.Text;
using Colorful;
using Newtonsoft.Json;
using GoPlay;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;
using GoPlay.Core.Routers;
using GoPlay.Core.Utils;
using Console = Colorful.Console;

namespace GoPlayProj.Main;

public class IdleClearFilter : IFilter
{
    protected readonly TimeSpan DETECT_FRAME = TimeSpan.FromMinutes(1);
    protected readonly TimeSpan IDLE_THREHOLD = TimeSpan.FromMinutes(1);
    
    protected Server _server;
    protected CancellationToken _cancellationToken;
    protected ConcurrentDictionary<uint, DateTime> _activeTime = new ConcurrentDictionary<uint, DateTime>();

    public void OnRegistered(IFilterable filterable)
    {
        _server = filterable as Server;
        _cancellationToken = _server.CancelSource.Token;
        TaskUtil.LongRun(RunLoop, _cancellationToken);
    }

    private void RunLoop()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            var list = _activeTime.ToList();
            foreach (var (clientId, activeTime) in list)
            {
                var gap = DateTime.UtcNow.Subtract(activeTime);
                if (gap <= IDLE_THREHOLD) continue;
                
                _server.Kick(clientId, Consts.ErrCode.IDLE_TIMEOUT);
            }

            Task.Delay(DETECT_FRAME, _cancellationToken).Wait(_cancellationToken);
        }
    }

    public void OnClientConnected(uint clientId)
    {
        _activeTime[clientId] = DateTime.UtcNow;
    }

    public void OnClientDisconnected(uint clientId)
    {
        _activeTime.TryRemove(clientId, out _);
    }

    public bool OnPreSend(Package pack)
    {
        return false;
    }

    public void OnPostSend(Package pack)
    {
    }

    public bool OnPreRecv(Package pack)
    {
        var clientId = pack.Header.ClientId;
        _activeTime[clientId] = DateTime.UtcNow;
        
        return false;
    }

    public void OnPostRecv(Package pack)
    {
    }

    public void OnError(uint clientId, Exception err)
    {
    }
}