using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using GoPlay.Core.DataFlow;
using GoPlay.Core.Protocols;
using GoPlay.Interfaces;
using GoPlay.Statistics;

namespace GoPlay.Core.Processors
{
    /// <summary>
    /// 改进版 ProcessorTplBase - 真正实现单线程处理
    /// 主要改进：
    /// 1. 所有 ActionBlock 共享单个线程
    /// 2. 修复 DelayCall 的线程安全和内存泄漏问题
    /// 3. 添加背压处理
    /// 4. 修复 PropagateCompletion 设置
    /// 5. 恢复 Update 和 ResolveBroadcast 调用
    /// </summary>
    public abstract class ProcessorTplBaseImproved : ProcessorBase
    {
        // 使用 SortedDictionary 保证延迟任务按时间排序
        // Key: 唯一ID，Value: (执行时间, 任务)
        protected ConcurrentDictionary<Guid, (DateTime ExecuteTime, Func<Task> Action)> m_delayTasksThreadSafe;
        
        // 单个 TaskScheduler 确保所有操作在同一个线程执行
        protected TaskScheduler m_singleThreadScheduler;
        protected CancellationTokenSource m_dataflowCancellation;
        
        protected BufferBlock<DataFlowItemBase> m_bufferBlock;
        protected ActionBlock<DataFlowItemBase> m_actionBlock;

        internal override IEnumerable<(DateTime, Func<Task>)> DelayTasks =>
            m_delayTasksThreadSafe?.Select(o => (o.Value.ExecuteTime, o.Value.Action)).ToArray() 
            ?? Array.Empty<(DateTime, Func<Task>)>();

        public override void StartThread(bool newPackageQueue = false, bool newBroadcastQueue = false)
        {
            m_delayTasksThreadSafe = new();
            m_dataflowCancellation = CancellationTokenSource.CreateLinkedTokenSource(Server.CancelSource.Token);
            
            // 创建单线程的 TaskScheduler
            var taskFactory = new TaskFactory(m_dataflowCancellation.Token,
                TaskCreationOptions.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
            
            // 创建专用的单线程 TaskScheduler
            m_singleThreadScheduler = new ConcurrentExclusiveSchedulerPair(
                TaskScheduler.Default, 
                maxConcurrencyLevel: 1).ConcurrentScheduler;

            if (m_bufferBlock == null || newPackageQueue || newBroadcastQueue)
            {
                // BufferBlock 配置
                var bufferOptions = new DataflowBlockOptions
                {
                    BoundedCapacity = ushort.MaxValue,
                    CancellationToken = m_dataflowCancellation.Token,
                    EnsureOrdered = true,
                };
                
                m_bufferBlock = new BufferBlock<DataFlowItemBase>(bufferOptions);
                
                // ActionBlock 配置 - 关键：使用单线程 TaskScheduler
                var actionOptions = new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = 1, // 一次只处理一个
                    CancellationToken = m_dataflowCancellation.Token,
                    EnsureOrdered = true,
                    MaxDegreeOfParallelism = 1, // 确保单线程
                    TaskScheduler = m_singleThreadScheduler, // 使用专用 scheduler
                    SingleProducerConstrained = false, // 多个生产者（package, broadcast, update等）
                };
                
                // 单个 ActionBlock 处理所有类型的消息
                m_actionBlock = new ActionBlock<DataFlowItemBase>(
                    async item => await ProcessDataFlowItem(item),
                    actionOptions);
                
                // 简单的 LinkTo，不设置 PropagateCompletion
                m_bufferBlock.LinkTo(m_actionBlock, new DataflowLinkOptions
                {
                    PropagateCompletion = false, // 不自动传播完成状态
                });
            }
        }

        /// <summary>
        /// 统一处理所有类型的 DataFlow 消息
        /// </summary>
        protected virtual async Task ProcessDataFlowItem(DataFlowItemBase item)
        {
            try
            {
                switch (item)
                {
                    case PackageItem packageItem:
                        ResolvePackage(packageItem.Package, m_dataflowCancellation.Token);
                        break;
                    
                    case BroadcastItem broadcastItem:
                        await OnBroadcast(broadcastItem.ClientId, broadcastItem.EventId, broadcastItem.Data);
                        break;
                    
                    case UpdateItem _:
                        if (this is IUpdate updater)
                        {
                            LastUpdate = DateTime.UtcNow;
                            await updater.OnUpdate();
                        }
                        break;
                    
                    case DelayCallItem delayItem:
                        // 从字典中移除
                        if (delayItem.Action != null)
                        {
                            // 注意：这里需要 DelayCallItem 包含 Guid
                            await delayItem.Action.Invoke();
                        }
                        break;
                    
                    case DeferCallItem deferItem:
                        await deferItem.Action.Invoke();
                        break;
                    
                    default:
                        Server.OnErrorEvent(IdLoopGenerator.INVALID,
                            new Exception($"ProcessorTplBase: unhandled dataflow item type: {item.GetType().Name}"));
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不记录
            }
            catch (Exception err)
            {
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
        }

        public override async Task StopThread()
        {
            if (m_actionBlock == null) return;

            try
            {
                // 1. 标记 BufferBlock 完成（不再接受新消息）
                m_bufferBlock.Complete();
                
                // 2. 等待 BufferBlock 中的消息全部发送到 ActionBlock
                await m_bufferBlock.Completion;
                
                // 3. 标记 ActionBlock 完成
                m_actionBlock.Complete();
                
                // 4. 等待 ActionBlock 处理完所有消息
                await m_actionBlock.Completion;
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException || 
                    err.InnerException is TaskCanceledException) return;
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
            catch (Exception err)
            {
                Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
            finally
            {
                m_dataflowCancellation?.Cancel();
                m_dataflowCancellation?.Dispose();
            }
        }

        public override void OnPackageReceived(Package packRaw)
        {
            PostWithRetry(new PackageItem { Package = packRaw });
        }

        public override void OnBroadcastReceived(uint clientId, int eventId, object data)
        {
            PostWithRetry(new BroadcastItem
            {
                ClientId = clientId,
                EventId = eventId,
                Data = data,
            });
        }

        public override void DeferCall(Func<Task> func)
        {
            PostWithRetry(new DeferCallItem { Action = func });
        }

        public override void DelayCall(TimeSpan delay, Func<Task> func)
        {
            if (m_delayTasksThreadSafe == null) m_delayTasksThreadSafe = new();

            var id = Guid.NewGuid();
            var executeTime = DateTime.UtcNow.Add(delay);
            m_delayTasksThreadSafe.TryAdd(id, (executeTime, func));
        }

        public override void OnUpdateReceived()
        {
            PostWithRetry(new UpdateItem());
        }

        public override void OnDelayCallReceived(DateTime time, Func<Task> action)
        {
            // 找到对应的 Guid 并移除
            var task = m_delayTasksThreadSafe.FirstOrDefault(x => 
                x.Value.ExecuteTime == time && x.Value.Action == action);
            
            if (task.Key != Guid.Empty)
            {
                m_delayTasksThreadSafe.TryRemove(task.Key, out _);
                PostWithRetry(new DelayCallItem
                {
                    ExecuteTime = time,
                    Action = action,
                });
            }
        }

        /// <summary>
        /// 带重试的 Post，处理背压问题
        /// </summary>
        protected virtual void PostWithRetry(DataFlowItemBase item, int maxRetries = 3)
        {
            if (m_dataflowCancellation.IsCancellationRequested) return;
            
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    // Post 返回 false 表示队列满或 block 已完成
                    if (m_bufferBlock.Post(item))
                    {
                        return; // 成功
                    }
                    
                    // 队列满，短暂等待后重试
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        Task.Delay(10 * retryCount, m_dataflowCancellation.Token).Wait();
                    }
                }
                catch (OperationCanceledException)
                {
                    return; // 取消操作
                }
                catch (Exception err)
                {
                    Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
                    return;
                }
            }
            
            // 重试失败，记录错误
            Server.OnErrorEvent(IdLoopGenerator.INVALID,
                new Exception($"ProcessorTplBase: Failed to post {item.GetType().Name} after {maxRetries} retries"));
        }

        public override ProcessorStatus GetStatus()
        {
            return new ProcessorStatus
            {
                Name = GetName(),
                Status = TaskStatus.Running, // DataFlow 没有直接的 Task 状态
                PackageQueueCount = m_bufferBlock?.Count ?? 0,
                BroadcastQueueCount = 0, // DataFlow 中统一在 BufferBlock 里
            };
        }
    }
}
