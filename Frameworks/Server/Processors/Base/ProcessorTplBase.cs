using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using GoPlay.Core.DataFlow;
using GoPlay.Core.Protocols;
using GoPlay.Core.Routers;
using GoPlay.Core.Utils;
using GoPlay.Interfaces;

namespace GoPlay.Core.Processors
{
    public abstract class ProcessorTplBase : ProcessorBase
    {
        protected ConcurrentDictionary<Func<Task>, DateTime> m_delayTasksThreadSafe;

        internal override IEnumerable<(DateTime, Func<Task>)> DelayTasks =>
            m_delayTasksThreadSafe?.Select(o => (o.Value, o.Key)).ToArray() ?? Array.Empty<(DateTime, Func<Task>)>();

        protected BufferBlock<DataFlowItemBase> m_bufferBlock;
        protected ActionBlock<DataFlowItemBase> m_packageBlock;
        protected ActionBlock<DataFlowItemBase> m_broadcastBlock;
        protected ActionBlock<DataFlowItemBase> m_updateBlock;
        protected ActionBlock<DataFlowItemBase> m_delayBlock;
        protected ActionBlock<DataFlowItemBase> m_deferBlock;
        protected ActionBlock<DataFlowItemBase> m_fallbackBlock;

        public override void StartThread(bool newPackageQueue = false, bool newBroadcastQueue = false)
        {
            m_delayTasksThreadSafe = new();

            m_packageBlock = new ActionBlock<DataFlowItemBase>(ResolvePackage);
            m_broadcastBlock = new ActionBlock<DataFlowItemBase>(ResolveBroadcast);
            m_updateBlock = new ActionBlock<DataFlowItemBase>(ResolveUpdate);
            m_delayBlock = new ActionBlock<DataFlowItemBase>(ResolveDelayCall);
            m_deferBlock = new ActionBlock<DataFlowItemBase>(ResolveDeferCall);
            m_fallbackBlock = new ActionBlock<DataFlowItemBase>(ResolveFallback);

            if (m_bufferBlock == null || newPackageQueue || newBroadcastQueue)
            {
                m_bufferBlock = new BufferBlock<DataFlowItemBase>(new DataflowBlockOptions
                {
                    BoundedCapacity = ushort.MaxValue,
                    CancellationToken = Server.CancelSource.Token,
                    EnsureOrdered = true,
                    MaxMessagesPerTask = 1,
                });
                m_bufferBlock.LinkTo(m_packageBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = true,
                }, data => data is PackageItem);

                m_bufferBlock.LinkTo(m_broadcastBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = true,
                }, data => data is BroadcastItem);

                m_bufferBlock.LinkTo(m_updateBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = true,
                }, data => data is UpdateItem);

                m_bufferBlock.LinkTo(m_delayBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = true,
                }, data => data is DelayCallItem);

                m_bufferBlock.LinkTo(m_deferBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = true,
                }, data => data is DeferCallItem);

                m_bufferBlock.LinkTo(m_fallbackBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = true,
                }, data => data is not PackageItem &&
                           data is not BroadcastItem &&
                           data is not UpdateItem &&
                           data is not DelayCallItem &&
                           data is not DeferCallItem);
            }
        }

        protected virtual void ResolvePackage(DataFlowItemBase arg)
        {
            var data = arg as PackageItem;
            ResolvePackage(data.Package, Server.CancelSource.Token);
        }

        protected virtual async Task ResolveBroadcast(DataFlowItemBase arg)
        {
            var data = arg as BroadcastItem;
            await OnBroadcast(data.ClientId, data.EventId, data.Data);
        }

        protected virtual async Task ResolveUpdate(DataFlowItemBase arg)
        {
            if (this is IUpdate updater)
            {
                await updater.OnUpdate();
                LastUpdate = DateTime.UtcNow;
            }
        }

        protected virtual async Task ResolveDelayCall(DataFlowItemBase arg)
        {
            var data = arg as DelayCallItem;

            m_delayTasksThreadSafe.TryRemove(data.Action, out _);
            await data.Action.Invoke();
        }

        protected virtual async Task ResolveDeferCall(DataFlowItemBase arg)
        {
            var data = arg as DeferCallItem;
            await data.Action.Invoke();
        }

        protected virtual Task ResolveFallback(DataFlowItemBase arg)
        {
            Server.OnErrorEvent(IdLoopGenerator.INVALID,
                new Exception($"ProcessorTplBase: unhandled dataflow item type: {arg.GetType().Name}"));
            return Task.CompletedTask;
        }

        public override async Task StopThread()
        {
            if (m_broadcastBlock == null) return;

            try
            {
                m_bufferBlock.Complete();
                await m_bufferBlock.Completion;
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException) return;
                if (err.InnerException is TaskCanceledException) return;
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
            catch (Exception err)
            {
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
        }

        public override void OnPackageReceived(Package packRaw)
        {
            try
            {
                m_bufferBlock.Post(new PackageItem
                {
                    Package = packRaw,
                });
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException) return;
                if (err.InnerException is TaskCanceledException) return;
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
            catch (Exception err)
            {
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
        }

        public override void OnBroadcastReceived(uint clientId, int eventId, object data)
        {
            try
            {
                m_bufferBlock.Post(new BroadcastItem
                {
                    ClientId = clientId,
                    EventId = eventId,
                    Data = data,
                });
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException) return;
                if (err.InnerException is TaskCanceledException) return;
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
            catch (Exception err)
            {
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
        }

        public override void DeferCall(Func<Task> func)
        {
            try
            {
                m_bufferBlock.Post(new DeferCallItem
                {
                    Action = func
                });
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException) return;
                if (err.InnerException is TaskCanceledException) return;
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
            catch (Exception err)
            {
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
        }

        public override void DelayCall(TimeSpan delay, Func<Task> func)
        {
            if (m_delayTasksThreadSafe == null) m_delayTasksThreadSafe = new();

            var time = DateTime.UtcNow.Add(delay);
            m_delayTasksThreadSafe.AddOrUpdate(func, time, (_, _) => time);
        }

        public override void OnUpdateReceived()
        {
            try
            {
                m_bufferBlock.Post(new UpdateItem());
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException) return;
                if (err.InnerException is TaskCanceledException) return;
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
            catch (Exception err)
            {
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
        }

        public override void OnDelayCallReceived(DateTime time, Func<Task> action)
        {
            try
            {
                m_bufferBlock.Post(new DelayCallItem
                {
                    ExecuteTime = time,
                    Action = action,
                });
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException) return;
                if (err.InnerException is TaskCanceledException) return;
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
            catch (Exception err)
            {
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
        }
    }
}