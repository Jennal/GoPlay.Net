using System.Collections.Concurrent;
using GoPlay.Core;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Core.Utils;
using GoPlay.Exceptions;
using GoPlay.Interfaces;

namespace GoPlay
{
    public abstract partial class Server
    {
        protected List<ProcessorBase> m_processors = new List<ProcessorBase>();
        public List<ProcessorBase> Processors => m_processors;

        public abstract void Register(ProcessorBase processor);
        public abstract T GetProcessor<T>() where T : ProcessorBase;
        public abstract void Broadcast(uint clientId, int eventId, object data);
    }
    
    /// <summary>
    /// 每个Processor会使用一个线程
    /// </summary>
    public partial class Server<T>
    {
        protected List<IStart> m_starters;
        protected List<IStop> m_stoppers;
        
        private Dictionary<string, Task> m_tasks = new Dictionary<string, Task>();
        private Dictionary<string, BlockingCollection<Package>> m_packageQueues = new Dictionary<string, BlockingCollection<Package>>();
        private Dictionary<string, BlockingCollection<(uint, int, object)>> m_broadcastQueues = new Dictionary<string, BlockingCollection<(uint, int, object)>>();

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

        protected virtual void ProcessorOnHandShake(ServerTag serverTag, Header header)
        {
            foreach (var processor in Processors)
            {
                processor.OnHandShake(header, serverTag);
            }
        }
        
        protected void StartProcessors()
        {
            m_packageQueues.Clear();
            m_tasks.Clear();

            m_starters = Processors.OfType<IStart>().ToList();
            m_stoppers = Processors.OfType<IStop>().ToList();

            foreach (var starter in m_starters)
            {
                starter.OnStart();
            }
            
            foreach (var processor in Processors)
            {   
                var name = processor.GetName();
                m_packageQueues[name] = new BlockingCollection<Package>(ushort.MaxValue);
                m_broadcastQueues[name] = new BlockingCollection<(uint, int, object)>(byte.MaxValue);
                m_tasks[name] = TaskUtil.LongRun(() => PackageLoop(processor, m_cancelSource.Token), m_cancelSource.Token);
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
            m_cancelSource.Cancel();
            Task.WaitAll(m_tasks.Values.ToArray());
        }
        
        protected virtual void OnRecv(Package packRaw)
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

        protected void PackageLoop(ProcessorBase processor, CancellationToken cancelToken)
        {
            var name = processor.GetName();
            var queue = m_packageQueues[name];
            var broadcastQueue = m_broadcastQueues[name];
            while (IsStarted && !cancelToken.IsCancellationRequested)
            {
                Update(processor);
                ResolveBroadCast(processor, broadcastQueue);

                if (!queue.TryTake(out var pack, Consts.TimeOut.Server)) continue;
                try
                {
                    var result = processor.OnPreRecv(pack!);
                    if (result != null)
                    {
                        Send(result);
                        processor.OnPostSendResult(result);
                        processor.DoDeferCalls().Wait(cancelToken);
                        continue;
                    }

                    var task = processor.Invoke(pack!);
                    task.Wait(cancelToken);
                    if (task.IsCanceled) return;
                    
                    result = task.Result;
                    if (result != null)
                    {
                        Send(result);
                        processor.OnPostSendResult(result);
                        processor.DoDeferCalls().Wait(cancelToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    //IGNORE ERR
                }
                catch (Exception err)
                {
                    OnErrorEvent(pack.Header.ClientId, err);
                }
            }
        }

        protected void ResolveBroadCast(ProcessorBase processor, BlockingCollection<(uint, int, object)> queue)
        {
            if (!queue.TryTake(out var tuple)) return;
            var (clientId, eventId, data) = tuple;
            try
            {
                processor.OnBroadcast(clientId, eventId, data);
            }
            catch (Exception err)
            {
                OnErrorEvent(clientId, err);
            }
        }
        
        protected void Update(ProcessorBase processor)
        {
            var updater = processor as IUpdate;
            if (updater == null) return;
            
            var ts = DateTime.UtcNow.Subtract(processor.LastUpdate);
            if (ts < Consts.TimeOut.Update) return;

            processor.LastUpdate = DateTime.UtcNow;
            try
            {
                updater.OnUpdate();
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
                    var name = processor.GetName();
                    var queue = m_broadcastQueues[name];
                    queue.Add((clientId, eventId, data), CanelToken);
                }
                catch (OperationCanceledException)
                {
                    /* IGNORE */
                }
                catch (Exception err)
                {
                    OnErrorEvent(clientId, err);
                }
            }
        }
    }
}