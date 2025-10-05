using System.Collections.Concurrent;
using GoPlay.Core;
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
            m_starters = Processors.OfType<IStart>().ToList();
            m_stoppers = Processors.OfType<IStop>().ToList();
            
            //init thread
            foreach (var processor in Processors)
            {   
                processor.StartThread();
            }
   
            //do start
            foreach (var starter in m_starters)
            {
                starter.OnStart();
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
            var tasks = new List<Task>();
            foreach (var processor in m_processors)
            {
                var task = processor.StopThread();
                tasks.Add(task);
            }
            
            Task.WaitAll(tasks.ToArray());
        }
        
        protected virtual void OnDataReceived(Package packRaw)
        {
            var processor = Processors.FirstOrDefault(o => o.IsRecognizeRoute(packRaw.Header.PackageInfo.Route));
            if (processor == null)
            {
                OnErrorEvent(packRaw.Header.ClientId, new RouteNotExistsException(packRaw.Header.PackageInfo.Route));
                return;
            }

            processor.OnPackageReceived(packRaw);
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
                    
                    processor.OnBroadcastReceived(clientId, eventId, data);
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
                yield return processor.GetStatus();
            }
        }

        public override async Task RestartProcessor(ProcessorBase processor, bool clearPackageQueue = false, bool clearBroadcastQueue = false)
        {
            await processor.StopThread();
            processor.StartThread(clearPackageQueue, clearBroadcastQueue);
        }
    }
}