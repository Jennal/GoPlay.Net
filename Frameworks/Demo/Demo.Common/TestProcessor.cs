using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Exceptions;
using GoPlay.Interfaces;

namespace Demo.Common;

[Processor("test")]
public class TestProcessor : ProcessorBase, IStart, IStop, IUpdate
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

    public void OnStart()
    {
        Console.WriteLine("TestProcessor.OnStart");
    }

    public void OnStop()
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] TestProcessor.OnStop-1");
        Task.Delay(1000).Wait();
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] TestProcessor.OnStop-2");
    }

    public Task OnUpdate()
    {
        m_count++;
        Console.WriteLine($"TestProcessor.OnUpdate[{m_count}] => {DateTime.Now:hh:mm:ss.fff}");
        return Task.CompletedTask;
    }
}