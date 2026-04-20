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
        public abstract T GetProcessor<T>() where T : ProcessorBase;
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
            foreach (var processor in Processors)
            {
                processor.OnClientConnected(clientId);
            }
        }

        protected virtual void ProcessorOnClientDisconnect(uint clientId)
        {
            foreach (var processor in Processors)
            {
                processor.OnClientDisconnected(clientId);
            }
        }

        protected virtual void ProcessorOnHandShake(ServerTag serverTag, Package<ReqHankShake> pack)
        {
            foreach (var processor in Processors)
            {
                processor.OnHandShake(pack, serverTag);
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

        public override TP GetProcessor<TP>()
        {
            return m_processors.OfType<TP>().FirstOrDefault();
        }
        
        protected void StopProcessors()
        {
            foreach (var pair in m_runners)
            {
                try
                {
                    pair.Value.StopAsync().Wait();
                }
                catch (AggregateException err)
                {
                    if (err.InnerException is OperationCanceledException) continue;
                    if (err.InnerException is TaskCanceledException) continue;
                    OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
                catch (Exception err)
                {
                    OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
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

        public override async Task ResolveBroadCast(ProcessorBase processor, ConcurrentQueue<(uint, int, object)> queue)
        {
            while (queue.TryDequeue(out var tuple))
            {
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
                    BroadcastQueueCount = runner.BroadcastQueue.Count,
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
