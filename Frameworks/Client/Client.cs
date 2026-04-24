using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
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

        public abstract string GetRoute(Package pack);
        
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
        
        // 出站队列：async pipeline 取代旧版 BlockingCollection + Task.Wait。
        // SingleReader=true（SendLoopAsync 独占消费），SingleWriter=false（多线程可 Send）。
        // 容量对齐旧版 byte.MaxValue，保留 back-pressure。
        protected Channel<Package> m_sendChannel = Channel.CreateBounded<Package>(
            new BoundedChannelOptions(byte.MaxValue)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
            });

        protected ConcurrentDictionary<uint, (DateTime, object)> m_requestCallbacks = new ConcurrentDictionary<uint, (DateTime, object)>();
        protected ConcurrentDictionary<string, List<object>> m_callbacks = new ConcurrentDictionary<string, List<object>>();

        protected TaskCompletionSource<bool> m_connectionTask;
        protected TaskCompletionSource<bool> m_disconnectionTask;
        
        protected Task m_sendTask;
        protected Task m_recvTask;
        protected Task m_timeoutTask;

        // SendLoopAsync 独占复用的 wire-frame 组包 buffer：
        // Package.WriteTo(writer) 把 [outerLen][headerLen][header][body] 直接写进这里，
        // 然后 Transport.Send(writer.WrittenMemory, cts) 一次性下发。
        // 整个连接生命周期只分配一次（按包体自然扩容到上游最大包规模），
        // 替代 Step 3.10/3.11 之前 `pack.GetBytes()` 每包 MemoryStream + ToArray 的双份分配。
        // 初始容量取 4 KB：小消息热路径不触发扩容，大消息按 IBufferWriter 标准 grow。
        private readonly ArrayBufferWriter<byte> m_sendWriter = new ArrayBufferWriter<byte>(4 * 1024);

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
                m_status = ClientStatus.Disconnected;
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
            // RunContinuationsAsynchronously 必备：push 模式下 SetResult 跑在 socket IOCP 完成回调线程
            // （NcClient/WsClient/WssClient 的 OnDataReceivedSpan → DispatchPack → 此 TCS）。
            // 不加这个 flag，await 之后的 continuation（含外层 Connect 的调用方、async [SetUp] 之后的
            // NUnit 调度）会同步反向占用 IOCP 线程，既拖慢收包又导致 Rider 在 IOCP 线程上 step 跟丢，
            // 表现为 "Debug 永远 step 不进下一个测试方法、Run 正常"。
            m_connectionTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            try
            {
                m_status = ClientStatus.Connecting;
                // 使用 async 版本：子类（NcClient 等）可以用 TCS+事件驱动不占 ThreadPool worker。
                // 100 并发 Task.Run 发起 Connect 时不会互相饥饿（见 TransportClientBase.ConnectAsync 注释）。
                await Transport.ConnectAsync(host, port, timeout).ConfigureAwait(false);

                // Push 路径（Nc/Ws/Wss）：ConnectAsync 返回后底层已连接，此时可能已经或即将收到 server
                // HandshakeResp。必须在启动 SendLoop 之前 bind span handler，否则 IOCP 回调触发
                // DrainStash → InvokeOnDataReceivedSpan(null handler) → silent drop → 永远收不到
                // HandshakeResp → RequestTimeout。bind 动作本身是单字段赋值，和 DrainStash 天然无竞态。
                if (Transport.SupportPush)
                {
                    Transport.SetDataReceivedSpanHandler(OnDataReceivedSpan);
                }

                // Send loop 的 "ready" 屏障：消除 TaskUtil.LongRun 线程调度延迟到 SendHandShake 之间的时序窗口。
                // 屏障放过后，SendLoopAsync 已持有 reader，handshake 包入队立刻被消费，不再依赖 m_sendChannel 的 buffer。
                // RunContinuationsAsynchronously：避免 ContinueWith 续跑在 loop 线程上反客为主。
                var sendReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                // SendLoopAsync 的第一轮（发 Handshake）全同步跑到底。用 TaskUtil.LongRun（LongRunning 专用线程）
                // 绕过 ThreadPool worker 队列，和 Timeout loop 对齐。SendLoopAsync 是 async，
                // 用 GetAwaiter().GetResult() 把 Task 桥回同步 Action。
                m_sendTask = TaskUtil.LongRun(
                    () => SendLoopAsync(m_cancelSource.Token, sendReady).GetAwaiter().GetResult(),
                    m_cancelSource.Token);
                m_timeoutTask = TaskUtil.LongRun(TimeoutLoop, m_cancelSource.Token);

                // 兜底：SendLoop 启动阶段若抛异常（或被取消）而 sendReady 尚未 set，await 就会永远 hang。
                // ContinueWith 在 task 终态时把结果透传到 ready TCS；TrySet* 对已设置过的 TCS 是 no-op。
                AttachReadyFallback(m_sendTask, sendReady);

                // Pull 路径（TCP）：仍需 RecvLoop 轮询 Transport.Recv；屏障包含 recvReady，保证进入主循环后才发 handshake。
                TaskCompletionSource<bool> recvReady = null;
                if (!Transport.SupportPush)
                {
                    recvReady = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    m_recvTask = TaskUtil.LongRun(() => RecvLoop(recvReady), m_cancelSource.Token);
                    AttachReadyFallback(m_recvTask, recvReady);
                }

                if (recvReady != null)
                {
                    await Task.WhenAll(recvReady.Task, sendReady.Task).ConfigureAwait(false);
                }
                else
                {
                    await sendReady.Task.ConfigureAwait(false);
                }

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

            // 同 m_connectionTask：DisconnectAsync 的 SetResult 调用点可能在 IOCP 回调线程链上
            // （TransportDisconnected → 上层 await DisconnectAsync 的反向触发），保持 continuation 排回 ThreadPool。
            m_disconnectionTask = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            m_status = ClientStatus.Disconnecting;
            m_cancelSource.Cancel();

            FilterOnClientDisconnect(0);
            // Push 路径先解绑 span handler：Transport.Disconnect 之后 IOCP 回调还可能在飞行中，
            // 此时调用 OnDataReceivedSpan 会看到 m_cancelSource.IsCancellationRequested=true 早返回，
            // 但直接 null 化更保险，避免回调期间访问已 Cancel 的 Client 状态。
            if (Transport.SupportPush)
            {
                Transport.SetDataReceivedSpanHandler(null);
            }
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
                catch (AggregateException err)
                {
                    if (err.InnerException is OperationCanceledException) continue;
                    if (err.InnerException is TaskCanceledException) continue;
                    
                    OnErrorEvent(err);
                    DisconnectAsync().ConfigureAwait(false);
                }
                catch (Exception err)
                {
                    OnErrorEvent(err);
                    DisconnectAsync().ConfigureAwait(false).GetAwaiter();
                }
            }
        }
        
        /// <summary>
        /// <paramref name="ready"/>：Connect 屏障。方法头部（拿到 reader 之后、首次 WaitToReadAsync 之前）
        /// TrySetResult(true)，通知 Connect 可以安全 SendHandShake 了——此时 channel 读者已就位，
        /// 不再依赖 m_sendChannel 的 buffer slot 暂存握手包。
        /// </summary>
        private async Task SendLoopAsync(CancellationToken ct, TaskCompletionSource<bool> ready)
        {
            var reader = m_sendChannel.Reader;
            var writer = m_sendWriter;

            ready.TrySetResult(true);

            while (!ct.IsCancellationRequested)
            {
                Package pack = null;
                try
                {
                    // WaitToReadAsync 让渡线程，而不是 TryTake 的轮询 + timeout
                    if (!await reader.WaitToReadAsync(ct).ConfigureAwait(false)) break;
                    if (!reader.TryRead(out pack)) continue;

                    if (pack.IsLastChunk && IsBlockSendByFilter(pack)) continue;

                    // 零拷贝组包：直接写到复用 writer，跳过 `pack.GetBytes()` 的
                    // `encoder.Encode(Data)->byte[]` + `MemoryStream`+`ToArray()` 三重分配。
                    // writer 贯穿整个连接生命周期，只分配一次（按最大包体自然扩容）。
                    try
                    {
                        pack.WriteTo(writer);
                    }
                    catch (Exception writeErr)
                    {
                        // 组包阶段任何异常都不要把 writer 留在脏状态，直接 reset 继续
                        ResetSendWriter(writer);
                        OnErrorEvent(writeErr);
                        continue;
                    }

                    try
                    {
                        // Transport.Send(Memory) 契约：子类必须在返回 ValueTask 之前把数据拷进自己的 pending buffer，
                        // 以便调用方 reset writer 复用底层 byte[]。NcClient/WsClient/WssClient/TcpClient 均遵守。
                        await Transport.Send(writer.WrittenMemory, m_cancelSource).ConfigureAwait(false);
                    }
                    finally
                    {
                        ResetSendWriter(writer);
                    }

                    if (ct.IsCancellationRequested) break;

                    if (pack.IsLastChunk) PostSendFilter(pack);
                }
                catch (OperationCanceledException) { /* 断开中，正常退出 */ }
                catch (ChannelClosedException) { break; }
                catch (AggregateException err) when (err.InnerException is OperationCanceledException
                                                     || err.InnerException is TaskCanceledException)
                {
                    /* 断开中 */
                }
                catch (Exception err)
                {
                    OnErrorEvent(err);
                    _ = DisconnectAsync();
                    break;
                }
            }
        }

        /// <summary>
        /// 把 <paramref name="loopTask"/> 的终态透传到 <paramref name="ready"/>——loop 线程若在 TrySetResult(true)
        /// 之前就抛异常/被取消（极少但并非不可能，例如 LongRun 内部启动期失败），WhenAll 会永远 hang。
        /// TrySet* 对已设置的 TCS 是 no-op，所以正常路径（loop 已自行 ready）完全无副作用。
        /// <see cref="TaskContinuationOptions.ExecuteSynchronously"/>：在 task 完成所在线程直接跑，零调度开销。
        /// </summary>
        private static void AttachReadyFallback(Task loopTask, TaskCompletionSource<bool> ready)
        {
            loopTask.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    ready.TrySetException(t.Exception!.InnerExceptions);
                }
                else if (t.IsCanceled)
                {
                    ready.TrySetCanceled();
                }
                else
                {
                    ready.TrySetResult(true);
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        /// <summary>
        /// 归零 <see cref="ArrayBufferWriter{T}"/>。net8+ 有 <c>ResetWrittenCount()</c> 只归零 index 保留底层 buffer；
        /// 旧 runtime 只能 <c>Clear()</c>（对 ArrayBufferWriter 语义相同：不重分配 buffer，仅 index 清零）。
        /// </summary>
        private static void ResetSendWriter(ArrayBufferWriter<byte> writer)
        {
#if NET8_0_OR_GREATER
            writer.ResetWrittenCount();
#else
            writer.Clear();
#endif
        }

        /// <summary>
        /// <paramref name="ready"/>：Connect 屏障。进入 while 之前 TrySetResult(true)，通知 Connect
        /// RecvLoop 已进入主循环；下一瞬即 Transport.Recv，server HandShakeResp 一到就被消费，
        /// 不再依赖底层 Transport 层 buffer 的 slot。
        ///
        /// <para>仅供 pull 语义 transport（如 <c>TcpClient</c>）使用。push 语义 transport
        /// （Nc/Ws/Wss，<see cref="Core.Transports.TransportClientBase.SupportPush"/>=true）
        /// 由 <see cref="OnDataReceivedSpan"/> 在 IOCP 回调线程直接分发，不起本 loop。</para>
        /// </summary>
        private void RecvLoop(TaskCompletionSource<bool> ready)
        {
            ready.TrySetResult(true);

            while (!m_cancelSource.Token.IsCancellationRequested)
            {
                try
                {
                    var dataTask = Transport.Recv(m_cancelSource);
                    dataTask.AsTask().Wait(m_cancelSource.Token);
                    var data = dataTask.Result;

                    if (m_cancelSource.Token.IsCancellationRequested) break;

                    var pack = Package.ParseRaw(data);
                    DispatchPack(pack);
                }
                catch (OperationCanceledException)
                {
                    //IGNORE ERR
                }
                catch (AggregateException err)
                {
                    if (err.InnerException is OperationCanceledException) continue;
                    if (err.InnerException is TaskCanceledException) continue;
                    
                    OnErrorEvent(err);
                    DisconnectAsync().ConfigureAwait(false);
                }
                catch (Exception err)
                {
                    OnErrorEvent(err);
                    DisconnectAsync().ConfigureAwait(false);
                    break;
                }
            }
        }

        /// <summary>
        /// Push 路径入口：由 push transport 的 IOCP 回调线程在 DrainStash 里同步调用，
        /// span 只在本方法返回前有效，内部 <see cref="Package.ParseRaw(ReadOnlySpan{byte})"/>
        /// 同步解码；body 需 async 持有的那一份在 ParseRaw 内 ToArray 拷出独立 byte[]。
        ///
        /// <para>异常语义与 RecvLoop 对齐：OCE 忽略，其余错误上报并触发 Disconnect。
        /// 注意本方法跑在 socket 完成回调线程上，未处理异常会让 net8 进程 failfast，
        /// 因此 try/catch 必须覆盖全部分支（DispatchPack 内部也只做同步分派不会抛到外面）。</para>
        /// </summary>
        private void OnDataReceivedSpan(ReadOnlySpan<byte> frame)
        {
            if (m_cancelSource == null || m_cancelSource.IsCancellationRequested) return;

            try
            {
                var pack = Package.ParseRaw(frame);
                DispatchPack(pack);
            }
            catch (OperationCanceledException)
            {
                //IGNORE ERR
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException) return;
                if (err.InnerException is TaskCanceledException) return;

                OnErrorEvent(err);
                DisconnectAsync().ConfigureAwait(false);
            }
            catch (Exception err)
            {
                OnErrorEvent(err);
                DisconnectAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 分派 decoded pack 到对应处理（chunk 组装、filter、类型 switch）。
        /// 被 RecvLoop（pull）和 OnDataReceivedSpan（push）共用，保持两条路径语义完全一致。
        /// </summary>
        private void DispatchPack(Package pack)
        {
            if (pack.IsChunk)
            {
                pack = ResolveChunk(pack);
                if (pack.IsChunk) return;
            }

            if (IsBlockRecvByFilter(pack)) return;

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
            // Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ResolveCallbacks-1");
            var routeId = pack.Header.PackageInfo.Route;
            var route = GetRouteById(routeId);
            if (!m_callbacks.ContainsKey(route)) return;

            // Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ResolveCallbacks-2");
            if (!m_callbacks.TryGetValue(route, out var list)) return;
            if (list.Count <= 0) return;

            // Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ResolveCallbacks-3");
            foreach (var item in list.ToArray())
            {
                OnCallback(item, pack);
            }
            // Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ResolveCallbacks-4 => {list.Count}");
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
            var writer = m_sendChannel.Writer;
            foreach (var p in list)
            {
                if (writer.TryWrite(p)) continue;

                // 满了：异步等位，保留 back-pressure 语义但不在调用线程上死锁
                _ = WriteSlowAsync(p);
            }
        }

        private async Task WriteSlowAsync(Package pack)
        {
            try
            {
                await m_sendChannel.Writer.WriteAsync(pack, m_cancelSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* 断开中 */ }
            catch (ChannelClosedException) { /* 断开中 */ }
            catch (Exception err) { OnErrorEvent(err); }
        }

        public override void Dispose()
        {
            Disconnect();
            m_cancelSource?.Dispose();
            m_sendChannel?.Writer?.TryComplete();
        }
    }
}