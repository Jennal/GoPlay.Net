using System;
using System.Threading.Tasks;

namespace GoPlay.Core.DataFlow
{
    /// <summary>
    /// 改进版 DelayCallItem - 使用 Guid 作为唯一标识符
    /// 解决原版中使用 Func<Task> 作为 Key 的问题
    /// </summary>
    public class DelayCallItemImproved : DataFlowItemBase
    {
        /// <summary>
        /// 唯一标识符，用于在 ConcurrentDictionary 中定位和移除任务
        /// </summary>
        public Guid Id;
        
        /// <summary>
        /// 任务执行时间
        /// </summary>
        public DateTime ExecuteTime; 
        
        /// <summary>
        /// 要执行的任务
        /// </summary>
        public Func<Task> Action;
    }
}
