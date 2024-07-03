using System;
using GoPlay;
using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transport.Ws;
using GoPlay.Core.Transport.Wss;
using GoPlay.Exceptions;

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
    
    [Request("inc")]
    public PbLong Inc(Header header, PbLong value)
    {
        return new PbLong
        {
            Value = value.Value + 1
        };
    }
    
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
    public PbString Error(Header header, PbString str)
    {
        throw new ProcessorMethodException(StatusCode.Failed, "Test Error");
    }
    
    [Notify("notify")]
    public void Notify(Header header, PbString str)
    {
        // Console.WriteLine($">>>> Server.Notify Recv: {str.Value}");
        Push("test.push", header, new PbString
        {
            Value = $"Push: {str.Value}"
        });
    }
}