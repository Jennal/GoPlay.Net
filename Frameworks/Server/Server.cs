using System.Collections.Concurrent;
using GoPlay.Core.Encodes.Factory;
using GoPlay.Core;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Core.Senders;
using GoPlay.Core.Transports;
using GoPlay.Core.Utils;
using GoPlay.Exceptions;
using GoPlay.Statistics;

namespace GoPlay
{
    public abstract partial class Server : IFilterable, IPackageSender, IDisposable
    {
        protected CancellationTokenSource  m_cancelSource = new CancellationTokenSource();
        public CancellationTokenSource CancelSource => m_cancelSource;
        
        public EncodingType EncodingType = EncodingType.Protobuf;

        /// <summary>
        /// 默认每个 Processor 允许的最大并发 in-flight 请求数。
        /// 业务可在 Processor class 上标 [MaxConcurrency(N)] 单独覆盖。
        /// 
        /// 传值约定：
        /// - &gt; 0：显式指定。
        /// - &lt;= 0：auto-sizing，按 Environment.ProcessorCount 推导（至少 1）。
        /// 
        /// 构造默认值 1：严格串行，对老业务行为完全兼容。
        /// </summary>
        public int DefaultConcurrency { get; }

        protected Server(int defaultConcurrency = 1)
        {
            DefaultConcurrency = defaultConcurrency > 0
                ? defaultConcurrency
                : Math.Max(1, Environment.ProcessorCount);
        }

        public abstract Type TransportType { get; }
        
        public abstract Task Start(string host, int port);
        public abstract void Stop();
        
        public abstract void OnErrorEvent(uint clientId, Exception err);
        public abstract void Send(Package package);
        public abstract void Kick(uint clientId, string reason);
        public abstract uint ToRouteId(string route);
        public abstract string ToRouteName(ServerTag serverTag, uint route);
        public abstract string GetClientIp(uint clientId);
        public abstract void Dispose();
        public abstract void RegisterFilter(IFilter filter);
        public abstract void UnregisterFilter(IFilter filter);
        public abstract bool IsBlockRecvByFilter(Package pack);
        public abstract bool IsBlockSendByFilter(Package pack);
        public abstract void PostRecvFilter(Package pack);
        public abstract void PostSendFilter(Package pack);
        public abstract void ErrorFilter(uint clientId, Exception err);
        
        public abstract bool IsSendQueueFull { get; }
        public abstract int SendQueueCount { get; }
        
        public abstract List<Package> GetAllSendQueue();
        public abstract IEnumerable<ProcessorStatus> GetProcessorQueueStatus();
        
        public abstract string GetRoute(Package pack);
        /// <summary>
        /// 从广播队列 drain 一批事件到 <paramref name="processor"/>。
        /// <para>
        /// 单次调用最多消费 <paramref name="maxItems"/> 条，剩余留给下轮周期 tick——
        /// 防止广播洪峰把一个 Runner 周期 tick 独占、饿死 <c>Update</c>/<c>DeferCalls</c>/<c>DelayCalls</c>。
        /// </para>
        /// </summary>
        public abstract Task ResolveBroadCast(ProcessorBase processor, ConcurrentQueue<(uint, int, object)> queue, int maxItems);
        public abstract Task Update(ProcessorBase processor);
    }
    
    public partial class Server<T> : Server
        where T : TransportServerBase, new()
    {
        public T Transport = new T();
        public IEncoder Encoder = EncoderFactory.Create(EncodingType.Protobuf); //Init instance

        /// <summary>
        /// 每 session 一个发送器：同 client 内保序，不同 client 天然并行，
        /// 取代旧版全局单线程 m_sendTask（该线程是跨 session 的共享瓶颈）。
        /// </summary>
        private readonly ConcurrentDictionary<uint, SessionSender> m_senders = new ConcurrentDictionary<uint, SessionSender>();

        /// <summary>
        /// 每个 SessionSender 的出站 Channel 容量。保留旧 m_sendQueue 的 ushort.MaxValue 上限。
        /// </summary>
        private const int SessionSenderCapacity = ushort.MaxValue;

        /// <summary>
        /// Stop 时等待 sender drain 的预算。超时后硬 Cancel。
        /// </summary>
        private static readonly TimeSpan SenderStopDrainTimeout = TimeSpan.FromSeconds(2);

        public override Type TransportType => typeof(T);
        
        public bool IsStarted
        {
            get;
            private set;
        }

        public CancellationToken CanelToken => m_cancelSource.Token;
        
        public event Action<uint, Exception> OnError;
        public event Action OnStarted;
        public event Action OnStopped;

        public event Action<uint> OnClientConnected
        {
            add => Transport.OnClientConnected += value;
            remove => Transport.OnClientConnected -= value;
        }
        public event Action<uint> OnClientDisconnected
        {
            add => Transport.OnClientDisconnected += value;
            remove => Transport.OnClientDisconnected -= value;
        }
        
        public Server() : this(1) { }

        public Server(int defaultConcurrency) : base(defaultConcurrency)
        {
            OnClientConnected += OnClientConnectEvent;
            OnClientDisconnected += OnClientDisconnectEvent;
            Transport.OnError += OnErrorEvent;

            // Step 3.14a: 优先走 span 版 dispatch，transport DrainStash 直接切片喂过来，
            // 省掉整帧 `new byte[len]` 分配。
            // transport 若未 DrainStash-span 化，TransportServerBase.InvokeOnDataReceivedSpan
            // 会 fallback 到 byte[] event；为了 fallback 路径仍然工作，这里也保留 byte[] 订阅。
            Transport.SetDataReceivedSpanHandler(OnDataReceivedSpan);
            Transport.OnDataReceived += OnDataReceived;
        }

        protected virtual void OnClientConnectEvent(uint clientId)
        {
            // 必须在下发业务事件前把 sender 建好：processor.OnClientConnected 里可能立即 Push
            GetOrCreateSender(clientId);

            FilterOnClientConnect(clientId);
            ProcessorOnClientConnect(clientId);
            SessionOnClientConnect(clientId);
        }

        protected virtual void OnClientDisconnectEvent(uint clientId)
        {
            FilterOnClientDisconnect(clientId);
            ProcessorOnClientDisconnect(clientId);
            SessionOnClientDisconnect(clientId);

            // 业务回调结束后再关闭 sender：允许断开回调里做最后一次 Send(...)，由 sender drain 写出
            RemoveAndStopSender(clientId);
        }

        public override void OnErrorEvent(uint clientId, Exception err)
        {
            OnError?.Invoke(clientId, err);
            ErrorFilter(clientId, err);
        }

        public override Task Start(string host, int port)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            
            m_cancelSource = new CancellationTokenSource();
            InitHandShake();
            Transport.Start(host, port, m_cancelSource);
            IsStarted = true;
            OnStarted?.Invoke();

            StartProcessors();

            // 发送侧不再有全局长跑线程：每 session 一个 SessionSender（由 OnClientConnectEvent 创建）。
            return Task.CompletedTask;
        }

        private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            OnErrorEvent(IdLoopGenerator.INVALID, (Exception)e.ExceptionObject);
        }

        public override void Stop()
        {
            if (!IsStarted) return;
            
            IsStarted = false;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomainOnUnhandledException;
            
            m_cancelSource.Cancel();
            StopProcessors();

            // 关闭所有 SessionSender：先 complete，Drain timeout 后硬 cancel
            StopAllSenders();

            Transport.Stop();

            if (m_stoppers?.Count > 0)
            {
                foreach (var stopper in m_stoppers)
                {
                    stopper.OnStop();
                }
            }

            OnStopped?.Invoke();
        }

        private void StopAllSenders()
        {
            var snapshot = m_senders.ToArray();
            m_senders.Clear();
            if (snapshot.Length == 0) return;

            var tasks = new List<Task>(snapshot.Length);
            foreach (var kv in snapshot)
            {
                tasks.Add(kv.Value.StopAsync(SenderStopDrainTimeout));
            }

            try { Task.WaitAll(tasks.ToArray(), SenderStopDrainTimeout + TimeSpan.FromSeconds(1)); }
            catch { /* best-effort */ }
        }

        private SessionSender GetOrCreateSender(uint clientId)
        {
            if (m_senders.TryGetValue(clientId, out var sender)) return sender;

            var created = new SessionSender(clientId, this, Transport, capacity: SessionSenderCapacity);
            if (m_senders.TryAdd(clientId, created))
            {
                created.Start(m_cancelSource.Token);
                return created;
            }

            // 并发下别的线程已经建好一份：直接弃掉 created（还没 Start，无副作用）
            return m_senders[clientId];
        }

        private void RemoveAndStopSender(uint clientId)
        {
            if (!m_senders.TryRemove(clientId, out var sender)) return;
            // fire-and-forget：不阻塞 IO 线程
            _ = sender.StopAsync(SenderStopDrainTimeout);
        }

        // protected void RecvLoop()
        // {
        //     var token = m_cancelSource.Token;
        //     while (IsStarted && !token.IsCancellationRequested)
        //     {
        //         uint clientId = 0;
        //         byte[] data = null;
        //         try
        //         {
        //             (clientId, data) = Transport.Recv();
        //             var pack = Package.ParseRaw(data);
        //             pack.Header.ClientId = clientId;
        //             // Console.WriteLine($" =[S]> {pack}");
        //
        //             //处理分包
        //             if (pack.IsChunk)
        //             {
        //                 pack = ResolveChunk(pack);
        //                 if (pack.IsChunk) return;
        //             }
        //
        //             if (IsBlockRecvByFilter(pack)) return;
        //             
        //             switch (pack.Header.PackageInfo.Type)
        //             {
        //                 case PackageType.HankShakeReq:
        //                     OnHandShake(pack);
        //                     break;
        //                 case PackageType.Ping:
        //                     OnPing(pack);
        //                     break;
        //                 case PackageType.Pong:
        //                     OnPong(pack);
        //                     break;
        //                 case PackageType.Notify:
        //                 case PackageType.Request:
        //                     OnDataReceived(pack);
        //                     break;
        //             }
        //
        //             PostRecvFilter(pack);
        //         }
        //         catch (OperationCanceledException)
        //         {
        //             //IGNORE ERR
        //         }
        //         catch (AggregateException err)
        //         {
        //             if (err.InnerException is OperationCanceledException) return;
        //             if (err.InnerException is TaskCanceledException) return;
        //             
        //             OnErrorEvent(clientId, err);
        //         }
        //         catch (Exception err)
        //         {
        //             OnErrorEvent(clientId, err);
        //         }
        //     }
        // }

        /// <summary>
        /// Step 3.14a: span 版收包入口。transport 子类 DrainStash 直接以 stash 切片调用
        /// <see cref="TransportServerBase.InvokeOnDataReceivedSpan"/>，省掉整帧 <c>new byte[len]</c>。
        ///
        /// <para>
        /// 语义等价于 <see cref="OnDataReceived(ValueTuple{uint,byte[]})"/>，只有第一步 <see cref="Package.ParseRaw"/>
        /// 改走 <see cref="ReadOnlySpan{T}"/> 重载。之后 Package 走 Channel 跨 async，不影响 span 生命周期
        /// （ParseRaw 内部 body 仍然 <c>.ToArray()</c> 拷出独立 byte[]，那份由 Step 3.15 候选 ArrayPool 化再议）。
        /// </para>
        /// </summary>
        private void OnDataReceivedSpan(uint clientId, ReadOnlySpan<byte> data)
        {
            try
            {
                var pack = Package.ParseRaw(data);
                pack.Header.ClientId = clientId;
                DispatchReceived(clientId, pack);
            }
            catch (OperationCanceledException)
            {
                //IGNORE ERR
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException) return;
                if (err.InnerException is TaskCanceledException) return;

                OnErrorEvent(clientId, err);
            }
            catch (Exception err)
            {
                OnErrorEvent(clientId, err);
            }
        }

        /// <summary>
        /// byte[] 版 fallback 入口：未 span 化的 transport 仍然走这里。
        /// 逻辑由 <see cref="DispatchReceived"/> 与 span 版共享。
        /// </summary>
        private void OnDataReceived(ValueTuple<uint, byte[]> val)
        {
            var (clientId, data) = val;
            try
            {
                var pack = Package.ParseRaw(data);
                pack.Header.ClientId = clientId;
                DispatchReceived(clientId, pack);
            }
            catch (OperationCanceledException)
            {
                //IGNORE ERR
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException) return;
                if (err.InnerException is TaskCanceledException) return;

                OnErrorEvent(clientId, err);
            }
            catch (Exception err)
            {
                OnErrorEvent(clientId, err);
            }
        }

        /// <summary>
        /// span 版与 byte[] 版共享的 Package dispatch 逻辑（chunk 重组、filter、按 PackageType 分流）。
        /// 本方法**不**捕获异常：由上层的 <see cref="OnDataReceivedSpan"/> / <see cref="OnDataReceived"/>
        /// 统一做 try/catch（两者 catch 集合一致）。
        /// </summary>
        private void DispatchReceived(uint clientId, Package pack)
        {
            // Console.WriteLine($" =[S]> {pack}");

            //处理分包
            if (pack.IsChunk)
            {
                pack = ResolveChunk(pack);
                if (pack.IsChunk) return;
            }

            if (IsBlockRecvByFilter(pack)) return;

            switch (pack.Header.PackageInfo.Type)
            {
                case PackageType.HankShakeReq:
                    OnHandShake(pack);
                    break;
                case PackageType.Ping:
                    OnPing(pack);
                    break;
                case PackageType.Pong:
                    OnPong(pack);
                    break;
                case PackageType.Notify:
                case PackageType.Request:
                    OnDataReceived(pack);
                    break;
            }

            PostRecvFilter(pack);
        }

        public override void Send(Package package)
        {
            try
            {
                if (!Transport.SupportPush && package.Header.PackageInfo.Type == PackageType.Push)
                {
                    throw new PushNotSupportedException();
                }

                var list = package.Split();
                foreach (var p in list)
                {
                    var sender = GetOrCreateSender(p.Header.ClientId);
                    sender.Enqueue(p);
                }
            }
            catch (Exception err)
            {
                OnErrorEvent(package.Header.ClientId, err);
            }
        }

        public override void Kick(uint clientId, string reason)
        {
            var package = Package.Create(0, PackageType.Kick, EncodingType);
            package.Header.ClientId = clientId;
            package.Header.Status.Message = reason;
            var sender = GetOrCreateSender(clientId);
            sender.Enqueue(package);

            TaskUtil.DelayRun(Consts.TimeOut.KickDelayDisconnect, () =>
            {
                DisconnectClient(clientId, new KickException(reason));
            }).ConfigureAwait(false);
        }

        protected virtual void DisconnectClient(uint clientId, Exception err)
        {
            Transport.DisconnectClient(clientId, err);
        }
        
        public override void Dispose()
        {
            Stop();
            m_cancelSource?.Dispose();
        }

        public override string GetClientIp(uint clientId)
        {
            return Transport.GetClientIp(clientId);
        }

        public override uint ToRouteId(string route)
        {
            foreach (var processor in m_processors)
            {
                var id = processor.ToRouteId(route);
                if (id != ProcessorBase.UNKOWN_ROUTE_ID) return id;
            }

            return ProcessorBase.UNKOWN_ROUTE_ID;
        }

        public override string ToRouteName(ServerTag serverTag, uint routeId)
        {
            var resp = GetHandShake(serverTag);
            if (resp != null)
            {
                var item = resp.Routes.FirstOrDefault(o => o.Value == routeId);
                if (!string.IsNullOrEmpty(item.Key)) return item.Key;
            }

            return $"UNKNOWN_ROUTE({routeId})";
        }

        /// <summary>
        /// 任一 sender 满即视作整体满：这是对旧"全局单队列满"语义的保守对应。
        /// </summary>
        public override bool IsSendQueueFull
        {
            get
            {
                foreach (var pair in m_senders)
                {
                    if (pair.Value.QueueCount >= SessionSenderCapacity) return true;
                }
                return false;
            }
        }

        public override int SendQueueCount
        {
            get
            {
                var total = 0;
                foreach (var pair in m_senders) total += pair.Value.QueueCount;
                return total;
            }
        }

        /// <summary>
        /// 诊断接口：枚举当前所有 sender 的出站快照。
        /// 注意：原实现返回 BlockingCollection.ToList()（非破坏性）。
        /// 由于 Channel 不暴露非破坏性枚举，这里返回的是 drain 出来的包，
        /// 仅在诊断/停机路径上调用，不要在运行时调。
        /// </summary>
        public override List<Package> GetAllSendQueue()
        {
            var list = new List<Package>();
            foreach (var pair in m_senders)
            {
                list.AddRange(pair.Value.DrainSnapshot());
            }
            return list;
        }
    }
}