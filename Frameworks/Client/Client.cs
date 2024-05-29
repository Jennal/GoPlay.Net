using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GoPlay.Core.Encodes.Factory;
using GoPlay.Core;
using GoPlay.Core.Interfaces;
using GoPlay.Core.Protocols;
using GoPlay.Core.Transports;
using GoPlay.Core.Utils;
using GoPlay.Exceptions;

namespace GoPlay
{
    public abstract partial class Client : IDisposable
    {
        public enum ClientStatus
        {
            Disconnected,
            Disconnecting,
            Connecting,
            Connected,
        }

        public ServerTag ServerTag = ServerTag.FrontEnd;
        public EncodingType EncodingType = EncodingType.Protobuf;
        public IMainThreadActionRunner MainThreadActionRunner = new MainThreadActionRunner();
        public TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
        
        protected CancellationTokenSource m_cancelSource = new CancellationTokenSource();
        protected ClientStatus m_status;
        public abstract bool IsConnected { get; }

        public ClientStatus Status => m_status;

        public string ClientVersion;
        
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnKicked;
        public event Action<Exception> OnError;
        
        protected void OnConnectedEvent()
        {
            MainThreadActionRunner.Invoke(OnConnected);
        }

        protected void OnDisconnectedEvent()
        {
            MainThreadActionRunner.Invoke(OnDisconnected);
        }

        protected void OnKickedEvent(string reason)
        {
            if (OnKicked == null) return;
            MainThreadActionRunner.Invoke(() => OnKicked(reason));
        }

        protected void OnErrorEvent(Exception err)
        {
            //ignore disconnect errors
            if (m_cancelSource.IsCancellationRequested) return;
            
            MainThreadActionRunner.Invoke(() =>
            {
                OnError?.Invoke(err);
                ErrorFilter(0, err);
            });
        }

        public virtual Task<bool> Connect(string host, int port)
        {
            return Connect(host, port, Consts.TimeOut.Connect);
        }
        
        public abstract Task<bool> Connect(string host, int port, TimeSpan timeout);
        public abstract Task DisconnectAsync();

        public void Disconnect()
        {
            DisconnectAsync().ConfigureAwait(false);
        }

        public abstract void ErrorFilter(uint clientId, Exception err);
        public abstract Task<Status> Request<T>(string route, T data);
        public abstract Task<Status> Request(string route);
        public abstract Task<(Status, RT)> Request<T, RT>(string route, T data);
        public abstract Task<(Status, RT)> Request<RT>(string route);
        public abstract void Notify<T>(string route, T data);
        public abstract void Notify(string route);

        /// <summary>
        /// T 支持
        /// - 任意Protobuf类型
        /// - Package
        /// - Package<>
        /// </summary>
        /// <param name="route"></param>
        /// <param name="action"></param>
        /// <typeparam name="T"></typeparam>
        public abstract void AddListener<T>(string route, Action<T> action);

        public abstract void AddListenerOnce<T>(string route, Action<T> action);

        public abstract void RemoveListener<T>(string route, Action<T> action);

        public abstract Task<T> WaitFor<T>(string route);

        public abstract void Dispose();
    }
    
    public partial class Client<T> : Client, IPackageSender
        where T : TransportClientBase, new()
    {
        protected string m_host;
        protected int m_port;
        
        public T Transport = new T();
        public IEncoder Encoder = EncoderFactory.Create(EncodingType.Protobuf); //Init instance
        
        protected BlockingCollection<Package> m_sendQueue = new BlockingCollection<Package>(byte.MaxValue);

        protected ConcurrentDictionary<uint, (DateTime, object)> m_requestCallbacks = new ConcurrentDictionary<uint, (DateTime, object)>();
        protected ConcurrentDictionary<string, List<object>> m_callbacks = new ConcurrentDictionary<string, List<object>>();

        protected TaskCompletionSource<bool> m_connectionTask;
        protected TaskCompletionSource<bool> m_disconnectionTask;
        
        protected Task m_sendTask;
        protected Task m_recvTask;
        protected Task m_timeoutTask;

        public Task SendTask => m_sendTask;
        public Task RecvTask => m_recvTask;
        public Task TimeoutTask => m_timeoutTask;

        public CancellationTokenSource CancelSource => m_cancelSource;
        
        public Client()
        {
            Transport.OnConnected += TransportConnected;
            Transport.OnDisconnected += TransportDisconnected;
        }

        private void TransportConnected()
        {
            /* DO NOTHING */
        }

        private void TransportDisconnected()
        {
            if (m_status != ClientStatus.Disconnecting && m_status != ClientStatus.Disconnected)
            {
                OnDisconnectedEvent();
            }
        }

        public override bool IsConnected => m_status == ClientStatus.Connected && Transport.IsConnected;

        public override async Task<bool> Connect(string host, int port, TimeSpan timeout)
        {
            switch (m_status)
            {
                case ClientStatus.Disconnecting:
                    await m_disconnectionTask.Task;
                    break;
                case ClientStatus.Connecting:
                    return await m_connectionTask.Task;
                    break;
                case ClientStatus.Connected:
                    if (host == m_host && port == m_port) return true;

                    await DisconnectAsync();
                    break;
                case ClientStatus.Disconnected:
                default:
                    /* DO NOTHING */
                    break;
            }

            m_host = host;
            m_port = port;
            
            m_cancelSource = new CancellationTokenSource();
            m_connectionTask = new TaskCompletionSource<bool>();

            try
            {
                m_status = ClientStatus.Connecting;
                Transport.Connect(host, port, timeout);
                m_recvTask = TaskUtil.LongRun(RecvLoop, m_cancelSource.Token);
                m_sendTask = TaskUtil.LongRun(SendLoop, m_cancelSource.Token);
                m_timeoutTask = TaskUtil.LongRun(TimeoutLoop, m_cancelSource.Token);
                SendHandShake();
            }
            catch (Exception err)
            {
                m_status = ClientStatus.Disconnected;
                OnErrorEvent(new ConnectionException(m_host, m_port, err));
                m_connectionTask.SetResult(false);
            }

            return await m_connectionTask.Task;
        }

        public override async Task DisconnectAsync()
        {
            if (m_status == ClientStatus.Disconnected) return;
            if (Transport == null) return;

            if (m_cancelSource.IsCancellationRequested || m_status == ClientStatus.Disconnecting)
            {
                await m_disconnectionTask.Task;
                return;
            }

            m_disconnectionTask = new TaskCompletionSource<bool>();
            m_status = ClientStatus.Disconnecting;
            m_cancelSource.Cancel();

            FilterOnClientDisconnect(0);
            Transport.Disconnect();
            var tasks = new[] { m_recvTask, m_sendTask, m_timeoutTask, m_heartbeatTask };
            await Task.WhenAll(tasks.Where(o => o != null));

            var errPack = Package.Create(0, PackageType.Response, EncodingType);
            errPack.Header.Status = new Status
            {
                Code = StatusCode.Error,
                Message = "NETWORK_ERROR",
            };
            foreach (var pair in m_requestCallbacks)
            {
                OnResponse(pair.Value, errPack);
            }
            m_requestCallbacks.Clear();
            //不 Clear m_callbacks，确保Listener不需要重新注册
            
            m_handshake = null;
            m_status = ClientStatus.Disconnected;

            OnDisconnectedEvent();
            m_disconnectionTask.SetResult(true);
        }

        private void TimeoutLoop()
        {
            while (!m_cancelSource.Token.IsCancellationRequested)
            {
                try
                {
                    //Check handshake
                    if (m_handshake == null)
                    {
                        var ts = DateTime.UtcNow.Subtract(m_sendHandshakeTime);
                        if (ts > RequestTimeout)
                        {
                            OnErrorEvent(new HandshakeTimeoutException());
                            DisconnectAsync().ConfigureAwait(false);
                            m_connectionTask.SetResult(false);
                            return;
                        }
                    }

                    //Check Request
                    var keys = m_requestCallbacks.Keys.ToList();
                    foreach (var key in keys)
                    {
                        if (!m_requestCallbacks.TryGetValue(key, out var tuple)) continue;

                        var (startTime, _) = tuple;
                        var ts = DateTime.UtcNow.Subtract(startTime);
                        if (ts < RequestTimeout) continue;

                        //如果无法删除，说明已经回调
                        if (!m_requestCallbacks.TryRemove(key, out _)) continue;

                        var timeoutPack = Package.Create(0, PackageType.Response, EncodingType);
                        timeoutPack.Header.Status = new Status
                        {
                            Code = StatusCode.Timeout,
                            Message = "REQUEST_TIMEOUT",
                        };
                        OnResponse(tuple, timeoutPack);
                    }

                    Task.Delay(Consts.TimeOut.TimeoutUpdate).Wait(m_cancelSource.Token);
                }
                catch (OperationCanceledException)
                {
                    //IGNORE ERR
                }
                catch (Exception err)
                {
                    OnErrorEvent(err);
                    DisconnectAsync().ConfigureAwait(false).GetAwaiter();
                }
            }
        }
        
        private void SendLoop()
        {
            while (!m_cancelSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (!m_sendQueue.TryTake(out var pack, Consts.TimeOut.Client)) continue;

                    //update content size
                    if (pack.IsLastChunk && IsBlockSendByFilter(pack)) continue;
                    // Console.WriteLine($" <[C]= {pack}");

                    Transport.Send(pack.GetBytes(), m_cancelSource).AsTask().Wait();
                    if (m_cancelSource.Token.IsCancellationRequested) break;

                    if(pack.IsLastChunk) PostSendFilter(pack);
                }
                catch (OperationCanceledException)
                {
                    //IGNORE ERR
                }
                catch (Exception err)
                {
                    OnErrorEvent(err);
                    DisconnectAsync().ConfigureAwait(false).GetAwaiter();
                }
            }
        }

        private void RecvLoop()
        {
            while (!m_cancelSource.Token.IsCancellationRequested)
            {
                try
                {
                    var dataTask = Transport.Recv(m_cancelSource);
                    dataTask.AsTask().Wait(m_cancelSource.Token);
                    var data = dataTask.Result;

                    if (m_cancelSource.Token.IsCancellationRequested) break;
                    
                    var pack = Package.ParseRaw(data);
                    // Console.WriteLine($" =[C]> {pack}");
                    
                    if (pack.IsChunk)
                    {
                        pack = ResolveChunk(pack);
                        if (pack.IsChunk) continue;
                    }
                    
                    if (IsBlockRecvByFilter(pack)) continue;

                    switch (pack.Header.PackageInfo.Type)
                    {
                        case PackageType.HankShakeResp:
                            if (pack.Header.Status.Code != StatusCode.Success)
                            {
                                OnErrorEvent(new HandshakeException(pack.Header.Status.Message));
                                DisconnectAsync().ConfigureAwait(false);
                                m_connectionTask.SetResult(false);
                                break;
                            }
                            ResolveHandShake(pack);
                            if (m_connectionTask != null && !m_connectionTask.Task.IsCompleted)
                            {
                                m_status = ClientStatus.Connected;
                                OnConnectedEvent();
                                StartHeartbeat();
                                FilterOnClientConnect(0);
                                m_connectionTask.SetResult(true);
                            }
                            break;
                        case PackageType.Ping:
                            ResolvePing(pack);
                            break;
                        case PackageType.Pong:
                            ResolvePong(pack);
                            break;
                        case PackageType.Response:
                            ResolveResponse(pack);
                            break;
                        case PackageType.Push:
                            ResolveCallbacks(pack);
                            break;
                        case PackageType.Kick:
                            ResolveKick(pack.Header.Status.Message);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    PostRecvFilter(pack);
                }
                catch (OperationCanceledException)
                {
                    //IGNORE ERR
                }
                catch (Exception err)
                {
                    OnErrorEvent(err);
                    DisconnectAsync().ConfigureAwait(false);
                    break;
                }
            }
        }
        
        private void ResolveResponse(Package pack)
        {
            ResolveCallbacks(pack);
            var packId = pack.Header.PackageInfo.Id;
            if (m_requestCallbacks.TryRemove(packId, out var tuple))
            {
                OnResponse(tuple, pack);
            }
        }

        private void ResolveCallbacks(Package pack)
        {
            var routeId = pack.Header.PackageInfo.Route;
            var route = GetRouteById(routeId);
            if (!m_callbacks.ContainsKey(route)) return;

            if (!m_callbacks.TryGetValue(route, out var list)) return;
            if (list.Count <= 0) return;

            foreach (var item in list.ToArray())
            {
                OnCallback(item, pack);
            }
        }

        private void ResolveKick(string reason)
        {
            OnKickedEvent(reason);
            DisconnectAsync().ConfigureAwait(false);
        }

        private void OnCallback(object action, Package packRaw)
        {
            object pack = packRaw;
            object data = packRaw;

            var argType = action.GetType().GenericTypeArguments[0];
            var dataType = argType;

            if (argType.Name == typeof(Package<>).Name) dataType = argType.GenericTypeArguments[0];
            var method = typeof(Package).GetMethod("ParseFromRaw", BindingFlags.Static | BindingFlags.Public);
            if (dataType != typeof(Package))
            {
                method = method.MakeGenericMethod(dataType);
                pack = method.Invoke(null, new object[] {packRaw});

                //get data
                var dataField = pack.GetType().GetField("Data");
                data = dataField.GetValue(pack);
            }

            //task set result
            method = action.GetType().GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);
            MainThreadActionRunner.Invoke(() =>
            {
                try
                {
                    if (argType == typeof(Package))
                    {
                        method.Invoke(action, new object[] {packRaw});
                    }
                    else if (argType.Name == typeof(Package<>).Name)
                    {
                        method.Invoke(action, new object[] {pack});
                    }
                    else
                    {
                        method.Invoke(action, new object[] {data});
                    }
                }
                catch (Exception err)
                {
                    OnErrorEvent(err);
                }
            });
        }

        protected void OnResponse((DateTime, object) tuple, Package packRaw)
        {
            var (_, task) = tuple;
            var retType = task.GetType().GenericTypeArguments[0];
            if (retType.Name == typeof(ValueTuple<,>).Name)
            {
                var dataType = task.GetType().GenericTypeArguments[0].GenericTypeArguments[1];
                var method = typeof(Package).GetMethod("ParseFromRaw", BindingFlags.Static | BindingFlags.Public);
                method = method.MakeGenericMethod(dataType);
                var pack = method.Invoke(null, new object[] {packRaw});

                //get data
                var dataField = pack.GetType().GetField("Data");
                var data = dataField.GetValue(pack);

                //tuple
                var val = Activator.CreateInstance(retType);
                retType.GetField("Item1").SetValue(val, packRaw.Header.Status);
                retType.GetField("Item2").SetValue(val, data);

                //task set result
                method = task.GetType().GetMethod("SetResult", BindingFlags.Instance | BindingFlags.Public);
                method.Invoke(task, new object[] {val});   
            }
            else if (retType == typeof(Status))
            {
                //task set result
                var method = task.GetType().GetMethod("SetResult", BindingFlags.Instance | BindingFlags.Public);
                method.Invoke(task, new object[] {packRaw.Header.Status});
            }
            else
            {
                throw new Exception("Unknown return type!");
            }
        }

        public override Task<Status> Request<TD>(string route, TD data)
        {
            var routeId = GetRouteId(route);
            var pack = Package.Create(routeId, data, PackageType.Request, EncodingType);
            
            var task = new TaskCompletionSource<Status>(TaskCreationOptions.RunContinuationsAsynchronously);
            m_requestCallbacks[pack.Header.PackageInfo.Id] = (DateTime.UtcNow, task);
            
            Send(pack);
            return task.Task;
        }

        public override Task<Status> Request(string route)
        {
            var routeId = GetRouteId(route);
            var pack = Package.Create(routeId, PackageType.Request, EncodingType);
            
            var task = new TaskCompletionSource<Status>(TaskCreationOptions.RunContinuationsAsynchronously);
            m_requestCallbacks[pack.Header.PackageInfo.Id] = (DateTime.UtcNow, task);
            
            Send(pack);
            return task.Task;
        }
        
        public override Task<(Status, TR)> Request<TD, TR>(string route, TD data)
        {
            var routeId = GetRouteId(route);
            var pack = Package.Create(routeId, data, PackageType.Request, EncodingType);
            
            var task = new TaskCompletionSource<(Status, TR)>(TaskCreationOptions.RunContinuationsAsynchronously);
            m_requestCallbacks[pack.Header.PackageInfo.Id] = (DateTime.UtcNow, task);
            
            Send(pack);
            return task.Task;
        }

        public override Task<(Status, TR)> Request<TR>(string route)
        {
            var routeId = GetRouteId(route);
            var pack = Package.Create(routeId, PackageType.Request, EncodingType);
            
            var task = new TaskCompletionSource<(Status, TR)>(TaskCreationOptions.RunContinuationsAsynchronously);
            m_requestCallbacks[pack.Header.PackageInfo.Id] = (DateTime.UtcNow, task);
            
            Send(pack);
            return task.Task;
        }

        public override void Notify<TD>(string route, TD data)
        {
            var routeId = GetRouteId(route);
            var pack = Package.Create(routeId, data, PackageType.Notify, EncodingType);
            Send(pack);
        }
        
        public override void Notify(string route)
        {
            var routeId = GetRouteId(route);
            var pack = Package.Create(routeId, PackageType.Notify, EncodingType);
            Send(pack);
        }

        /// <summary>
        /// TD 支持
        /// - 任意Protobuf类型
        /// - Package
        /// - Package<>
        /// </summary>
        /// <param name="route"></param>
        /// <param name="action"></param>
        /// <typeparam name="TD"></typeparam>
        public override void AddListener<TD>(string route, Action<TD> action)
        {
            if (action == null) return;

            if (!m_callbacks.ContainsKey(route)) m_callbacks[route] = new List<object>();
            m_callbacks[route].Add(action);
        }

        public override void AddListenerOnce<TD>(string route, Action<TD> action)
        {
            if (action == null) return;

            if (!m_callbacks.ContainsKey(route)) m_callbacks[route] = new List<object>();

            Action<TD> a = null;
            a = val =>
            {
                action(val);
                RemoveListener(route, a);
            };
            m_callbacks[route].Add(a);
        }

        public override void RemoveListener<TD>(string route, Action<TD> action)
        {
            if (action == null) return;
            if (!m_callbacks.TryGetValue(route, out var list)) return;

            list.Remove(action);
        }

        public override Task<TD> WaitFor<TD>(string route)
        {
            var task = new TaskCompletionSource<TD>();
            AddListenerOnce<TD>(route, data =>
            {
                if (task.Task.IsCompleted) return;
                task.SetResult(data);
            });

            return task.Task;
        }

        public void Send(Package pack)
        {
            if (m_cancelSource.Token.IsCancellationRequested) return;

            var list = pack.Split();
            foreach (var p in list)
            {
                m_sendQueue.Add(p, m_cancelSource.Token);    
            }
        }

        public override void Dispose()
        {
            Disconnect();
            m_cancelSource?.Dispose();
            m_sendQueue?.Dispose();
        }
    }
}