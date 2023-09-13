using System.Collections.Concurrent;
using GoPlay.Core.Encodes.Factory;
using GoPlay.Core;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports;
using GoPlay.Core.Utils;
using GoPlay.Exceptions;

namespace GoPlay
{
    public abstract partial class Server : IFilterable, IPackageSender, IDisposable
    {
        protected CancellationTokenSource  m_cancelSource = new CancellationTokenSource();
        public CancellationTokenSource CancelSource => m_cancelSource;
        
        public EncodingType EncodingType = EncodingType.Protobuf;

        public abstract Type TransportType { get; }
        
        public abstract Task Start(string host, int port);
        public abstract void Stop();
        
        public abstract void OnErrorEvent(uint clientId, Exception err);
        public abstract void Send(Package package);
        public abstract void Kick(uint clientId, string reason);
        public abstract uint ToRouteId(string route);
        public abstract string GetClientIp(uint clientId);
        public abstract void Dispose();
        public abstract void RegisterFilter(IFilter filter);
        public abstract void UnregisterFilter(IFilter filter);
        public abstract bool IsBlockRecvByFilter(Package pack);
        public abstract bool IsBlockSendByFilter(Package pack);
        public abstract void PostRecvFilter(Package pack);
        public abstract void PostSendFilter(Package pack);
        public abstract void ErrorFilter(uint clientId, Exception err);
    }
    
    public partial class Server<T> : Server
        where T : TransportServerBase, new()
    {
        public T Transport = new T();
        public IEncoder Encoder = EncoderFactory.Create(EncodingType.Protobuf); //Init instance

        protected BlockingCollection<Package> m_sendQueue;

        protected Task m_sendTask;
        protected Task m_recvTask;
        // protected Task m_updateTask;

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
        
        public Server()
        {
            OnClientConnected += OnClientConnectEvent;
            OnClientDisconnected += OnClientDisconnectEvent;
            Transport.OnError += OnErrorEvent;
        }

        protected virtual void OnClientConnectEvent(uint clientId)
        {
            FilterOnClientConnect(clientId);
            ProcessorOnClientConnect(clientId);
            SessionOnClientConnect(clientId);
        }

        protected virtual void OnClientDisconnectEvent(uint clientId)
        {
            FilterOnClientDisconnect(clientId);
            ProcessorOnClientDisconnect(clientId);
            SessionOnClientDisconnect(clientId);
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

            m_sendQueue = new BlockingCollection<Package>(ushort.MaxValue);
            
            m_recvTask = TaskUtil.LongRun(RecvLoop, m_cancelSource.Token);
            m_sendTask = TaskUtil.LongRun(SendLoop, m_cancelSource.Token);
            // m_updateTask = TaskUtil.LongRun(UpdateLoop, m_cancelSource.Token);

            StartProcessors();
            
            return Task.WhenAll(m_recvTask, m_sendTask);
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
            
            StopProcessors();
            m_cancelSource.Cancel();

            try
            {
                Task.WaitAll(m_recvTask, m_sendTask);
                m_sendQueue.Dispose();
            }
            catch
            {
                /* DO NOTHING */
            }

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

        protected void SendLoop()
        {
            var token = m_cancelSource.Token;
            while (IsStarted && !token.IsCancellationRequested)
            {
                Package package = null;
                try
                {
                    if (!m_sendQueue.TryTake(out package, Consts.TimeOut.Server)) continue;
                    if (IsBlockSendByFilter(package)) continue;

                    // Console.WriteLine($" <[S]({package.Header.ClientId})= {package}");
                    Transport.Send(package.Header.ClientId, package.GetBytes());
                    PostSendFilter(package);
                }
                catch (OperationCanceledException)
                {
                    //IGNORE ERR
                }
                catch (Exception err)
                {
                    OnErrorEvent(package?.Header.ClientId ?? IdLoopGenerator.INVALID, err);
                }
            }
        }

        protected void RecvLoop()
        {
            var token = m_cancelSource.Token;
            while (IsStarted && !token.IsCancellationRequested)
            {
                uint clientId = 0;
                byte[] data;
                try
                {
                    (clientId, data) = Transport.Recv();
                    var pack = Package.ParseRaw(data);
                    pack.Header.ClientId = clientId;
                    // Console.WriteLine($" =[S]> {pack}");

                    if (IsBlockRecvByFilter(pack)) continue;

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
                            OnRecv(pack);
                            break;
                    }

                    PostRecvFilter(pack);
                }
                catch (OperationCanceledException)
                {
                    //IGNORE ERR
                }
                catch (Exception err)
                {
                    OnErrorEvent(clientId, err);
                }
            }
        }

        public override void Send(Package package)
        {
            m_sendQueue.Add(package, CanelToken);
        }

        public override void Kick(uint clientId, string reason)
        {
            var package = Package.Create(0, PackageType.Kick, EncodingType);
            package.Header.ClientId = clientId;
            package.Header.Status.Message = reason;
            m_sendQueue.Add(package, CanelToken);

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
            m_sendQueue?.Dispose();
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
    }
}