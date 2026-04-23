using System.Collections.Concurrent;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Core.Utils;
using GoPlay.Exceptions;
using GoPlay.Interfaces;
using GoPlay.Statistics;

namespace GoPlay
{
    public abstract partial class Server
    {
        protected List<ProcessorBase> m_processors = new List<ProcessorBase>();
        public List<ProcessorBase> Processors => m_processors;

        public abstract void Register(ProcessorBase processor);

        /// <summary>
        /// 获取指定 Processor 的 Actor 调用句柄。返回 <see cref="ProcessorRef{T}"/>，
        /// 所有跨 Processor 方法调用必须通过它（或 Source Generator 为 <c>[ProcessorApi]</c> 方法
        /// 生成的扩展方法）完成，由目标 Runner 的 mailbox 串行执行，天然无竞争。
        ///
        /// 找不到对应 Processor 时返回 <c>default(ProcessorRef&lt;T&gt;)</c>（<c>IsValid == false</c>），
        /// 调用其方法会抛 <see cref="InvalidOperationException"/>。
        /// </summary>
        public abstract ProcessorRef<T> GetProcessor<T>() where T : ProcessorBase;

        /// <summary>
        /// 过渡期逃生舱：返回裸 Processor 对象，用于尚未迁移到 <c>[ProcessorApi]</c> 白名单机制的调用点。
        /// <para>
        /// 直接持有裸对象意味着方法调用发生在调用方的 Runner 线程上下文里，
        /// 和目标 Runner 形成跨线程数据竞争——仅为迁移期过渡兼容，长期目标是全部迁到 <see cref="GetProcessor{T}"/>。
        /// </para>
        /// </summary>
        [Obsolete("跨 Processor 直接持有裸对象会引入线程竞争。请把目标方法标 [ProcessorApi] 并改用 Server.GetProcessor<T>().Xxx(...)。此 API 仅为迁移期保留，合并清理后将被删除。", error: false)]
        public abstract T GetProcessorUnsafe<T>() where T : ProcessorBase;

        public abstract void Broadcast(uint clientId, int eventId, object data);
        
        public abstract Task RestartProcessor(ProcessorBase processor, bool clearPackageQueue = false, bool clearBroadcastQueue = false);
    }
    
    /// <summary>
    /// 每个 Processor 一个 ProcessorRunner（虚拟线程模型）。
    /// 路由分发通过 Dictionary&lt;uint, ProcessorRunner&gt; 做 O(1) 投递。
    /// </summary>
    public partial class Server<T>
    {
        protected List<IStart> m_starters;
        protected List<IStop> m_stoppers;
        
        // 旧字段已被 m_runners 取代。保留命名方便阅读 git history 时对照。
        private Dictionary<string, ProcessorRunner> m_runners = new Dictionary<string, ProcessorRunner>();
        private Dictionary<uint, ProcessorRunner> m_routeIdToRunner = new Dictionary<uint, ProcessorRunner>();

        protected virtual void ProcessorOnClientConnect(uint clientId)
        {
            // Per-processor try/catch：任一 Processor 的 OnClientConnected 抛异常，都不能让后续 Processor 漏事件。
            // 异常走 OnErrorEvent 统一上报，便于业务侧做告警 / 补偿。
            foreach (var processor in Processors)
            {
                try { processor.OnClientConnected(clientId); }
                catch (Exception err) { OnErrorEvent(clientId, err); }
            }
        }

        protected virtual void ProcessorOnClientDisconnect(uint clientId)
        {
            // 同 Connect：一个 Processor 抛异常不能阻断后续 Processor 的 Disconnect 回调，
            // 否则跨服务器在线数 Redis -1 会在中途漏掉，导致永久偏差。
            foreach (var processor in Processors)
            {
                try { processor.OnClientDisconnected(clientId); }
                catch (Exception err) { OnErrorEvent(clientId, err); }
            }
        }

        protected virtual void ProcessorOnHandShake(ServerTag serverTag, Package<ReqHankShake> pack)
        {
            // 同 Connect / Disconnect：单个 Processor 异常不得吞掉后续 Processor 的 HandShake 事件
            foreach (var processor in Processors)
            {
                try { processor.OnHandShake(pack, serverTag); }
                catch (Exception err) { OnErrorEvent(pack.Header.ClientId, err); }
            }
        }
        
        protected void StartProcessors()
        {
            m_runners.Clear();
            m_routeIdToRunner.Clear();

            m_starters = Processors.OfType<IStart>().ToList();
            m_stoppers = Processors.OfType<IStop>().ToList();

            // 1. 为每个 processor 建一个 runner
            foreach (var processor in Processors)
            {
                var name = processor.GetName();
                var runner = new ProcessorRunner(processor, this);
                m_runners[name] = runner;
            }

            // 2. 建立 routeId -> runner 的 O(1) 路由表
            BuildRouteMap();

            // 3. 触发 starter
            foreach (var starter in m_starters)
            {
                starter.OnStart();
            }

            // 4. 启动 runner
            foreach (var pair in m_runners)
            {
                pair.Value.Start(m_cancelSource.Token);
            }
        }

        private void BuildRouteMap()
        {
            m_routeIdToRunner.Clear();
            foreach (var processor in Processors)
            {
                var name = processor.GetName();
                if (!m_runners.TryGetValue(name, out var runner)) continue;

                foreach (var route in processor.GetRoutes())
                {
                    m_routeIdToRunner[route.RouteId] = runner;
                }
            }
        }

        public override void Register(ProcessorBase processor)
        {
            if (m_processors.Contains(processor)) return;

            processor.Server = this;
            var name = processor.GetName();
            if (m_processors.Any(o => o.GetName() == name)) throw new Exception($"ProcessorManager: duplicate processor name: {name}");
            
            m_processors.Add(processor);
        }

        public override ProcessorRef<TP> GetProcessor<TP>()
        {
            var target = m_processors.OfType<TP>().FirstOrDefault();
            if (target == null) return default;

            var name = target.GetName();
            if (!m_runners.TryGetValue(name, out var runner)) return default;

            return new ProcessorRef<TP>(target, runner);
        }

        [Obsolete("跨 Processor 直接持有裸对象会引入线程竞争。请把目标方法标 [ProcessorApi] 并改用 Server.GetProcessor<T>().Xxx(...)。此 API 仅为迁移期保留，合并清理后将被删除。", error: false)]
        public override TP GetProcessorUnsafe<TP>()
        {
            return m_processors.OfType<TP>().FirstOrDefault();
        }
        
        /// <summary>
        /// Stop 每个 Processor 的默认 graceful drain 预算。
        /// 选 5s 的依据：既给业务 Processor 足够窗口把 <see cref="ProcessorRunner._incoming"/> /
        /// <see cref="ProcessorRunner._control"/> / <see cref="ProcessorRunner._broadcastQueue"/> 吃干净，
        /// 又给 Server.Stop 后续 (StopAllSenders + Transport.Stop + m_stoppers.OnStop) 留出预算，
        /// 保证整体 Stop 不超过 k8s 典型 terminationGracePeriodSeconds（30s）。
        /// 调用方想要更长/更短可以改用 <see cref="StopProcessors(TimeSpan)"/>。
        /// </summary>
        private static readonly TimeSpan DefaultStopProcessorsDrainTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 默认预算版：等价 <see cref="StopProcessors(TimeSpan)"/> 传入 <see cref="DefaultStopProcessorsDrainTimeout"/>。
        /// </summary>
        protected void StopProcessors() => StopProcessors(DefaultStopProcessorsDrainTimeout);

        /// <summary>
        /// 并行停止所有 <see cref="ProcessorRunner"/>：给每个 Runner 传入同一个
        /// <paramref name="gracefulDrainTimeout"/>，让它们自行排干内部三队列。
        /// <para>
        /// 并行而非串行的原因：假如有 10 个 Processor、每个 drain 最多 5s，串行最坏 50s，
        /// 并行是 5s。Runner 之间彼此无依赖，并行是安全的。
        /// </para>
        /// <para>
        /// 整体等待上限：<c>gracefulDrainTimeout + 1s</c>。多出的 1s 给 StopAsync 内部硬 Cancel
        /// 后 await runTask 收尾的时间，避免恰好卡在窗口边界时 Task.WaitAll 自己先超时。
        /// </para>
        /// </summary>
        protected void StopProcessors(TimeSpan gracefulDrainTimeout)
        {
            if (m_runners.Count == 0) return;

            var tasks = new List<Task>(m_runners.Count);
            foreach (var pair in m_runners)
            {
                tasks.Add(pair.Value.StopAsync(gracefulDrainTimeout));
            }

            try
            {
                // 整体等一个 Runner-drain-window + 缓冲。各 Runner StopAsync 内部已经自带硬超时兜底，
                // 这里的 Task.WaitAll 超时只是"所有人都在硬超时边界上"的极端兜底。
                Task.WaitAll(tasks.ToArray(), gracefulDrainTimeout + TimeSpan.FromSeconds(1));
            }
            catch (AggregateException err)
            {
                foreach (var inner in err.InnerExceptions)
                {
                    if (inner is OperationCanceledException) continue;
                    if (inner is TaskCanceledException) continue;
                    OnErrorEvent(IdLoopGenerator.INVALID, inner);
                }
            }
            catch (Exception err)
            {
                OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
        }
        
        protected virtual void OnDataReceived(Package packRaw)
        {
            if (!m_routeIdToRunner.TryGetValue(packRaw.Header.PackageInfo.Route, out var runner))
            {
                OnErrorEvent(packRaw.Header.ClientId, new RouteNotExistsException(packRaw.Header.PackageInfo.Route));
                return;
            }

            runner.Enqueue(packRaw);
        }

        public override async Task ResolveBroadCast(ProcessorBase processor, ConcurrentQueue<(uint, int, object)> queue, int maxItems)
        {
            // maxItems <= 0 视为不限；正常调用方应显式传一个合理上限，
            // 避免在一次 tick 内把 Update/DeferCalls/DelayCalls 饿死。
            var unlimited = maxItems <= 0;
            var remaining = maxItems;
            while ((unlimited || remaining > 0) && queue.TryDequeue(out var tuple))
            {
                if (!unlimited) remaining--;
                var (clientId, eventId, data) = tuple;
                try
                {
                    await processor.OnBroadcast(clientId, eventId, data);
                }
                catch (Exception err)
                {
                    OnErrorEvent(clientId, err);
                }
            }
        }
        
        public override async Task Update(ProcessorBase processor)
        {
            var updater = processor as IUpdate;
            if (updater == null) return;
            
            var ts = DateTime.UtcNow.Subtract(processor.LastUpdate);
            if (ts < processor.UpdateDeltaTime) return;

            processor.LastUpdate = DateTime.UtcNow;
            try
            {
                await updater.OnUpdate();
            }
            catch (Exception err)
            {
                OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
        }
        
        public override void Broadcast(uint clientId, int eventId, object data)
        {
            foreach (var processor in m_processors)
            {
                try
                {
                    if (!processor.IsRecognizeBroadcastEvent(eventId)) continue;

                    var name = processor.GetName();
                    if (!m_runners.TryGetValue(name, out var runner)) continue;
                    runner.BroadcastQueue.Enqueue((clientId, eventId, data));
                }
                catch (OperationCanceledException)
                {
                    /* IGNORE */
                }
                catch (AggregateException err)
                {
                    if (err.InnerException is OperationCanceledException) continue;
                    if (err.InnerException is TaskCanceledException) continue;
                    
                    OnErrorEvent(clientId, err);
                }
                catch (Exception err)
                {
                    OnErrorEvent(clientId, err);
                }
            }
        }
        
        public override IEnumerable<ProcessorStatus> GetProcessorQueueStatus()
        {
            foreach (var processor in m_processors)
            {
                var name = processor.GetName();
                if (!m_runners.TryGetValue(name, out var runner)) continue;

                yield return new ProcessorStatus
                {
                    Name = name,
                    Status = runner.Status,
                    PackageQueueCount = runner.PackageQueueCount,
                    BroadcastQueueCount = runner.BroadcastQueueCount,
                    // 纯读，不 reset——这里的契约是"偷看一眼"。
                    // 若外部要做滑动窗口峰值，应走 Runner.SampleAndResetBroadcastPeakDepth()，
                    // 或后续加一个显式的 ResetPeak 重载，而不是在 Status 读取里埋隐式副作用。
                    BroadcastPeakDepth = runner.BroadcastPeakDepth,
                };
            }
        }

        public override async Task RestartProcessor(ProcessorBase processor, bool clearPackageQueue = false, bool clearBroadcastQueue = false)
        {
            var name = processor.GetName();
            if (!m_runners.TryGetValue(name, out var oldRunner)) return;

            await oldRunner.StopAsync();

            // 取出旧 runner 的状态以便选择性继承
            var oldBroadcasts = clearBroadcastQueue ? null : oldRunner.BroadcastQueue;
            // 注意：当前实现的 channel 不支持读残留再迁移，clearPackageQueue=false 时会丢失尚未消费的包。
            //      原实现使用 BlockingCollection，可以原样保留。如果业务依赖此特性，需要在 ProcessorRunner 增加迁移 API。

            var newRunner = new ProcessorRunner(processor, this);
            if (oldBroadcasts != null)
            {
                while (oldBroadcasts.TryDequeue(out var item))
                {
                    newRunner.BroadcastQueue.Enqueue(item);
                }
            }

            m_runners[name] = newRunner;
            BuildRouteMap();

            if (processor is IStart starter)
            {
                starter.OnStart();
            }

            newRunner.Start(m_cancelSource.Token);
        }
    }
}
