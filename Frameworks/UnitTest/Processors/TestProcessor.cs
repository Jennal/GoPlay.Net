using System;
using System.Threading.Tasks;
using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;

namespace UnitTest.Processors;

[Processor("test")]
class TestProcessor : ProcessorBase
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
    
    [Request("echo.defer")]
    public PbString EchoDefer(Header header, PbString str)
    {
        // Console.WriteLine($">>>> Server.Echo Recv: {str.Value}");
        // Console.WriteLine(Server.SessionManager.Get<ReqHankShake>(header.ClientId, nameof(ReqHankShake)));
        DeferCall(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            Push("test.push", header, new PbString
            {
                Value = "Delay Push"
            });
        });
        return new PbString
        {
            Value = $"[{Prefix}] Server reply: {str.Value}"
        };
    }
    
    [Request("echo.delay")]
    public PbString EchoDelay(Header header, PbString str)
    {
        Console.WriteLine($"EchoDelay-1: {DateTime.UtcNow:HH:mm:ss.fff}");
        DelayCall(TimeSpan.FromSeconds(1), async () =>
        {
            Console.WriteLine($"EchoDelay-2: {DateTime.UtcNow:HH:mm:ss.fff}");
            Push("test.push", header, new PbString
            {
                Value = "Delay Push"
            });
        });
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
        str.Value = $"Push: {str.Value}";
        Push("test.push", header, str);
        Push("test.push", header, str);
    }
}