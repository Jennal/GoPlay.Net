using System;
using System.Threading;
using System.Threading.Tasks;

namespace GoPlay.Core.Utils
{
    public static class TaskUtil
    {
        /// <summary>
        /// 启动需要长时间运行的Task
        /// </summary>
        /// <param name="action"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Task LongRun(Action action, CancellationToken token)
        {
            return Run(action, token, TaskCreationOptions.LongRunning);
        }
        
        public static Task Run(Action action, CancellationToken token)
        {
            var task = new Task(action, token, TaskCreationOptions.None);
            task.Start();
            return task;
        }
        
        public static Task Run(Action<object> action, object state, CancellationToken token)
        {
            var task = new Task(action, state, token, TaskCreationOptions.None);
            task.Start();
            return task;
        }
        
        public static Task Run(Action action, CancellationToken token, TaskCreationOptions options)
        {
            var task = new Task(action, token, options);
            task.Start();
            return task;
        }
        
        public static Task Run(Action<object> action, object state, CancellationToken token, TaskCreationOptions options)
        {
            var task = new Task(action, state, token, options);
            task.Start();
            return task;
        }

        public static Task RunChild(Action action, CancellationToken token)
        {
            return Run(action, token, TaskCreationOptions.AttachedToParent);
        }
        
        public static Task RunChild(Action<object> action, object state, CancellationToken token)
        {
            return Run(action, state, token, TaskCreationOptions.AttachedToParent);
        }

        public static async Task DelayRun(TimeSpan delay, Action action)
        {
            await Task.Delay(delay);
            action();
        }
    }
}