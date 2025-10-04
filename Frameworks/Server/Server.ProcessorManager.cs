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
    /// 每个Processor会使用一个线程
    /// </summary>
    public partial class Server<T>
    {
        protected List<IStart> m_starters;
        protected List<IStop> m_stoppers;
        
        private Dictionary<string, Task> m_tasks = new Dictionary<string, Task>();
        private Dictionary<string, CancellationTokenSource> m_restartProcessorTokenSources = new Dictionary<string, CancellationTokenSource>();
        private Dictionary<string, BlockingCollection<Package>> m_packageQueues = new Dictionary<string, BlockingCollection<Package>>();
        private Dictionary<string, ConcurrentQueue<(uint, int, object)>> m_broadcastQueues = new Dictionary<string, ConcurrentQueue<(uint, int, object)>>();

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
            m_packageQueues.Clear();
            m_tasks.Clear();
            m_restartProcessorTokenSources.Clear();

            m_starters = Processors.OfType<IStart>().ToList();
            m_stoppers = Processors.OfType<IStop>().ToList();

            //init data
            foreach (var processor in Processors)
            {   
                var name = processor.GetName();
                m_packageQueues[name] = new BlockingCollection<Package>(ushort.MaxValue);
                m_broadcastQueues[name] = new ConcurrentQueue<(uint, int, object)>();
                m_restartProcessorTokenSources[name] = new CancellationTokenSource();
            }
            
            //do start
            foreach (var starter in m_starters)
            {
                starter.OnStart();
            }
            
            //init thread
            foreach (var processor in Processors)
            {   
                var name = processor.GetName();
                var source = m_restartProcessorTokenSources[name];
                m_tasks[name] = TaskUtil.LongRun(() => PackageLoop(processor, m_cancelSource.Token, source.Token), source.Token);
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
            foreach (var task in m_tasks)
            {
                try
                {
                    task.Value.Wait();
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
            var processor = Processors.FirstOrDefault(o => o.IsRecognizeRoute(packRaw.Header.PackageInfo.Route));
            if (processor == null)
            {
                OnErrorEvent(packRaw.Header.ClientId, new RouteNotExistsException(packRaw.Header.PackageInfo.Route));
                return;
            }

            var name = processor.GetName();
            m_packageQueues[name].Add(packRaw);
        }

        protected void PackageLoop(ProcessorBase processor, CancellationToken cancelToken, CancellationToken restartToken)
        {
            var name = processor.GetName();
            var queue = m_packageQueues[name];
            var broadcastQueue = m_broadcastQueues[name];
            while (IsStarted && !cancelToken.IsCancellationRequested && !restartToken.IsCancellationRequested)
            {
                try
                {
                    if (!processor.PackageLoopFrame(queue, broadcastQueue, cancelToken)) break;
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
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
                    var queue = m_broadcastQueues[name];
                    queue.Enqueue((clientId, eventId, data));
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
                var packageQueue = m_packageQueues[name];
                var broadcastQueue = m_broadcastQueues[name];
                var task = m_tasks[name];
                
                yield return new ProcessorStatus
                {
                    Name = name,
                    Status = task.Status,
                    PackageQueueCount = packageQueue.Count,
                    BroadcastQueueCount = broadcastQueue.Count,
                };
            }
        }

        public override async Task RestartProcessor(ProcessorBase processor, bool clearPackageQueue = false, bool clearBroadcastQueue = false)
        {
            var name = processor.GetName();
            m_restartProcessorTokenSources[name].Cancel();
            await m_tasks[name];

            if (clearPackageQueue)
            {
                m_packageQueues[name] = new BlockingCollection<Package>(ushort.MaxValue);
            }
            
            if (clearBroadcastQueue)
            {
                m_broadcastQueues[name] = new ConcurrentQueue<(uint, int, object)>();
            }
            
            if (processor is IStart starter)
            {
                starter.OnStart();
            }
            
            m_restartProcessorTokenSources[name] = new CancellationTokenSource();
            m_tasks[name] = TaskUtil.LongRun(() => PackageLoop(processor, m_cancelSource.Token, m_restartProcessorTokenSources[name].Token), m_restartProcessorTokenSources[name].Token);
        }
    }
}