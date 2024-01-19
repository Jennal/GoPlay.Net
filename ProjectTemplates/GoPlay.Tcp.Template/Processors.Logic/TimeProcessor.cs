using Google.Protobuf.WellKnownTypes;
using GoPlay.Core.Attributes;
using GoPlay.Core.Protocols;

namespace GoPlayProj.Processors
{
    [ServerTag(Tag = ServerTag.FrontEnd)]
    [Processor("time")]
    public partial class TimeProcessor : GoPlayProjProcessor
    {
        public override string[] Pushes => null;

        [BeforeLogin]
        [Request("utc.now")]
        public PbTime GetTime(Header header)
        {
            return new PbTime
            {
                Value = DateTime.UtcNow.ToTimestamp()
            };
        }
    }
}