using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Demo;
using GoPlay.Interfaces;

namespace Demo.Common;

[Processor("plane")]
public class AirPlaneProcessor : ProcessorBase
{
    private HashSet<uint> m_clientIds = new();
    public List<PlayerData> PlayerDatas = new();
    
    public override string[] Pushes => new[]
    {
        "airplane.join",
        "airplane.pos",
        "airplane.offline"
    };

    private const int xMax = 1920;
    private const int yMax = 1080;
    
    [Request("register")]
    public GameData Register(Header header, RegisterAccount str)
    {
        var xPos = GetRandPos(xMax);
        var yPos = GetRandPos(yMax);
        var playerData = new PlayerData
        {
            Id = header.ClientId,
            Name = str.Name,
            Pos = new Vector2
            {
                X = xPos,
                Y = yPos,
            }
        };
        PlayerDatas.Add(playerData);
        
        var gameData = new GameData();
        gameData.PlayerList.AddRange(PlayerDatas);
        gameData.CurPlayer = playerData;
        
        foreach (var clientId in m_clientIds)
        {
            if (clientId != header.ClientId)
                Push("airplane.join", clientId, playerData);
        }
        
        return gameData;
    }

    private int GetRandPos(int maxLimit)
    {
        return new Random().Next(maxLimit);
    }
    
    [Request("update.pos")]
    public Status UpdatePos(Header header, PlayerData str)
    {
        if (PlayerDatas.All(o=>o.Id != str.Id))
            return Status.Error("不存在该用户，修改失败");
        
        foreach (var item in PlayerDatas)
        {
            if (item.Id == str.Id)
            {
                item.Pos = str.Pos;
                break;
            }
        }
        
        foreach (var clientId in m_clientIds)
        {
            Push("airplane.pos", clientId, str);
        }
        return Status.Success;
    }
    
    public override void OnClientConnected(uint clientId)
    {
        m_clientIds.Add(clientId);
    }

    public override void OnClientDisconnected(uint clientId)
    {
        var playerData = PlayerDatas.FirstOrDefault(o=>o.Id == clientId);
        if (playerData != null)
        {
            foreach (var cId in m_clientIds)
            {
                 Push("airplane.offline", cId, playerData);
            }
        }
        
        PlayerDatas.RemoveAll(o=> o.Id == clientId);
        m_clientIds.Remove(clientId);
    }
}