using System;
using System.Threading.Tasks;
using GoPlay.Core.Protocols;

namespace GoPlay.Core.DataFlow
{
    public class DelayCallItem : DataFlowItemBase
    {
        public DateTime ExecuteTime; 
        public Func<Task> Action;
    }
}