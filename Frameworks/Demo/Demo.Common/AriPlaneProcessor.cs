using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Demo;
using GoPlay.Interfaces;

namespace Demo.Common;

[Processor("ariPlane")]
public class AriPlaneProcessor : ProcessorBase, IStart, IStop, IUpdate
{
    private HashSet<uint> m_clientIds = new();
    
    public List<PlayerData> PlayerDatas = new List<PlayerData>();
    public override string[] Pushes => new string[]
    {
        "airPlane.Push_Join","airPlane.Push_ChangePos", "airPlane.Push_OffLine"
    };

    private const int xLen = 1920;
    private const int yLen = 1080;
    
    [Request("register")]
    public GameData Register(Header header, RegisterAccount str)
    {
        var xPos = GetRandPos(xLen);
        var yPos = GetRandPos(yLen);
        var playerData = new PlayerData()
        {
            HumanId = header.ClientId,
            Name = str.Name,
            DVector2 = new DVector2()
            {
                XPos = xPos,
                YPos = yPos,
            }
        };
        
        PlayerDatas.Add(playerData);
        var gameData = new GameData();
        gameData.PlayerData.AddRange(PlayerDatas);
        gameData.CurPlayerData = playerData;
        
        foreach (var clientId in m_clientIds)
        {
            if (clientId != header.ClientId)
                Push("airPlane.Push_Join", clientId, playerData);
        }
        
        return gameData;
    }

    private int GetRandPos(int maxLimit)
    {
        return new Random().Next(maxLimit);
    }
    
    [Request("changAirPos")]
    public Status ChangeAirPos(Header header, PlayerData str)
    {
        if (PlayerDatas.All(o=>o.HumanId != str.HumanId))
            return Status.Error("不存在该用户，修改失败");
        
        foreach (var item in PlayerDatas)
        {
            if (item.HumanId == str.HumanId)
            {
                item.DVector2 = new DVector2()
                {
                    XPos = str.DVector2.XPos,
                    YPos = str.DVector2.YPos
                };
                break;
            }
        }
        
        foreach (var clientId in m_clientIds)
        {
            Push("airPlane.Push_ChangePos", clientId, str);
        }
        return Status.Success;
    }
    
    public void OnStart()
    {
        Console.WriteLine("AirplaneProcessor.OnStart");
    }
    
    public override void OnClientConnected(uint clientId)
    {
        m_clientIds.Add(clientId);
    }

    public override void OnClientDisconnected(uint clientId)
    {
        var playerData = PlayerDatas.FirstOrDefault(o=>o.HumanId == clientId);
        if (playerData != null)
        {
            foreach (var cId in m_clientIds)
            {
                 Push("airPlane.Push_OffLine", cId, playerData);
            }
        }
        
        PlayerDatas.RemoveAll(o=> o.HumanId == clientId);
        m_clientIds.Remove(clientId);
    }

    public void OnStop()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] AirplaneProcessor.OnStop-1");
        Task.Delay(1000).Wait();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] AirplaneProcessor.OnStop-2");
    }

    public Task OnUpdate()
    {
        return Task.CompletedTask;
    }
}