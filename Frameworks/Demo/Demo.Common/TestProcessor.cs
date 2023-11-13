using System;
using GoPlay;
using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.Ws;
using GoPlay.Core.Transport.Wss;

namespace Demo.Common;

[Processor("test")]
public class TestProcessor : ProcessorBase
{
    private int m_count = -1;
        
    public override string[] Pushes => new string[]
    {
        "test.push"
    };

    public string Prefix = "Test";
    
    [Request("echo")]
    public PbString Echo(Header header, PbString str)
    {
        // Console.WriteLine($">>>> Server.Echo Recv: {str.Value}");
        // Console.WriteLine(Server.SessionManager.Get<ReqHankShake>(header.ClientId, nameof(ReqHankShake)));
        return new PbString
        {
            Value = $"[{Prefix}] Server reply: {str.Value}"
        };
    }
        
    [Request("err")]
    public object Error(Header header, PbString str)
    {
        // Console.WriteLine(Server.SessionManager.Get<ReqHankShake>(header.ClientId, nameof(ReqHankShake)));
        
        m_count++;
        if (m_count % 2 == 0)
        {
            return new PbString
            {
                Value = $"Server reply: {str.Value}"
            };
        }
        else
        {
            return new Status
            {
                Code = StatusCode.Error,
                Message = "SYSTEM_ERR"
            };
        }
    }
    
    [Notify("notify")]
    public void Notify(Header header, PbString str)
    {
        // Console.WriteLine($">>>> Server.Notify Recv: {str.Value}");
        Push("test.push", header, new PbString
        {
            Value = $"Push: {str.Value} - 1"
        });

        var browser = "unkown";
        if (Server is Server<WsServer> s)
        {
            browser = s.GetClientBrowser(header.ClientId);
        }
        else if (Server is Server<WssServer> s2)
        {
            browser = s2.GetClientBrowser(header.ClientId);
        }
        
        Push("test.push", header, new PbString
        {
            Value = $"Push: {str.Value} - {browser}"
        });
    }
}