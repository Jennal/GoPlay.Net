using GoPlay.Services.Core.Attributes;
using GoPlay.Services.Core.Processors;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Benchmarks;

[Processor("test")]
class TestProcessor : ProcessorBase
{
    private int m_count = -1;
        
    public override string[] Pushes => new string[]
    {
        "test.push"
    };

    [Request("echo")]
    public PbString Echo(Header header, PbString str)
    {
        // Console.WriteLine($">>>> Server.Echo Recv: {str.Value}");
        // Console.WriteLine(Server.SessionManager.Get<ReqHankShake>(header.ClientId, nameof(ReqHankShake)));
        return new PbString
        {
            Value = $"Server reply: {str.Value}"
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