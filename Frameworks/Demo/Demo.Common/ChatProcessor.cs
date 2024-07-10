using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Demo;

namespace Demo.WsServer.Processors;

[Processor("chat")]
public class ChatProcessor : ProcessorBase
{
    private HashSet<uint> m_clientIds = new();
    
    public override string[] Pushes => new string[]
    {
        "chat.push"
    };

    public override void OnClientConnected(uint clientId)
    {
        m_clientIds.Add(clientId);
    }

    public override void OnClientDisconnected(uint clientId)
    {
        m_clientIds.Remove(clientId);
    }
    
    [Request("chat.send")]
    public Status Send(Header header, ChatData str)
    {
        foreach (var clientId in m_clientIds)
        {
            Push("chat.push", clientId, str);
        }

        return Status.Success;
    }
}