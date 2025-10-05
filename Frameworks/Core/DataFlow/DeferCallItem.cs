using System;
using System.Threading.Tasks;
using GoPlay.Core.Protocols;

namespace GoPlay.Core.DataFlow
{
    public class DeferCallItem : DataFlowItemBase
    { 
        public Func<Task> Action;
    }
}