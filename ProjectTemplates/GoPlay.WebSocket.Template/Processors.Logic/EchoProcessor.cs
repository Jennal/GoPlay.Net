using GoPlay.Core.Attributes;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Exceptions;

namespace GoPlayProj.Processors
{
    [ServerTag(Tag = ServerTag.FrontEnd)]
    [Processor("echo")]
    public class EchoProcessor : ProcessorBase
    {
        public override string[] Pushes => new[]
        {
            "echo.push"
        };
        
        [BeforeLogin]
        [Request("request")]
        public PbString Request(Header header, PbString data)
        {
            return new PbString
            {
                Value = $"Serv reply: {data.Value}"
            };
        }
        
        [BeforeLogin]
        [Notify("notify")]
        public void Notify(Header header, PbString data)
        {
            Push("echo.push", header, new PbString
            {
                Value = $"Serv push: {data.Value}"
            });
        }
        
        [BeforeLogin]
        [Request("timeout")]
        public async Task<PbString> Timeout(Header header, PbString data)
        {
            await Task.Delay(TimeSpan.FromSeconds(6));
            return new PbString
            {
                Value = $"Serv timeout: {data.Value}"
            };
        }
        
        [BeforeLogin]
        [Request("err")]
        public async Task<PbString> Err(Header header, PbString data)
        {
            throw new ProcessorMethodException(StatusCode.Failed, "ERROR_CODE");
        }
    }
}