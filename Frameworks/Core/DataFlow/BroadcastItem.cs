using GoPlay.Core.Protocols;

namespace GoPlay.Core.DataFlow
{
    public class BroadcastItem : DataFlowItemBase
    {
        public uint ClientId;
        public int EventId;
        public object Data;
    }
}