using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Channels;
using GoPlay.Core.Attributes;
using GoPlay.Core.Protocols;
using GoPlay.Core.Routers;
using GoPlay.Core.Utils;
using GoPlay.Interfaces;

namespace GoPlay.Core.Processors
{
    /// <summary>
    /// 一个 ProcessorRunner 服务一个 Processor，模拟"虚拟线程"语义：
    /// - 单 reader Channel 接收业务消息
    /// - 主循环 async 化，业务 await 异步 I/O 时不再阻塞 OS 线程
    /// - MaxConcurrency==1（默认）严格串行，业务无需加锁
    /// - MaxConcurrency&gt;1 时使用 ExclusiveScheduler 限制同步代码段不并行
    ///
    /// 并发上限解析顺序：Processor class 上的 [MaxConcurrency(N)] -&gt; Server.DefaultConcurrency。
    ///
    /// 取代原先 PackageLoopFrame + BlockingCollection&lt;Package&gt; + Task.Wait 的实现。
    /// </summary>
    public sealed class ProcessorRunner
    {
        private readonly ProcessorBase _processor;
        private readonly Server _server;
        private readonly Channel<Package> _incoming;
        private readonly ConcurrentQueue<(uint, int, object)> _broadcastQueue;
        private readonly TaskFactory _scheduler;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly int _maxConcurrency;
        private readonly int _drainBatchSize;

        // routeId -> Route 的 O(1) 查表，用于在调度路径上拿到 Method 级 [MaxConcurrency] 配置。
        // 仅在 _maxConcurrency > 1 路径下使用（_maxConcurrency==1 已是严格串行，方法级限流是冗余）。
        private readonly Dictionary<uint, Route> _routeById;

        public int MaxConcurrency => _maxConcurrency;

        private CancellationTokenSource _restartCts;
        private CancellationTokenSource _linkedCts;
        private Task _runTask;

        public ProcessorRunner(ProcessorBase processor, Server server, int capacity = ushort.MaxValue, int drainBatchSize = 64)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _server = server ?? throw new ArgumentNullException(nameof(server));

            _incoming = Channel.CreateBounded<Package>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
            });
            _broadcastQueue = new ConcurrentQueue<(uint, int, object)>();

            _maxConcurrency = ResolveMaxConcurrency(processor, server);
            _drainBatchSize = Math.Max(1, drainBatchSize);

            // 启动期 lint：扫描 Processor 类型检测 [MaxConcurrency] 误用，仅打 Warning 不抛错
            WarnOnMaxConcurrencyMisuse(processor);

            if (_maxConcurrency > 1)
            {
                // 仅在允许流水线并发时才启用 ExclusiveScheduler，串行化同步代码段
                var pair = new ConcurrentExclusiveSchedulerPair();
                _scheduler = new TaskFactory(pair.ExclusiveScheduler);
                _concurrencyLimiter = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);

                // 构建 routeId 查表，并启动期校验 Method 级 [MaxConcurrency] 不超过 Processor 级
                _routeById = new Dictionary<uint, Route>();
                foreach (var r in processor.GetRoutes())
                {
                    _routeById[r.RouteId] = r;
                    ValidateMethodConcurrency(processor, r);
                }
            }
            else
            {
                _scheduler = new TaskFactory(TaskScheduler.Default);

                // 即使 processor 串行，方法级标注若 > 1 也算误用，启动期直接报错
                foreach (var r in processor.GetRoutes())
                {
                    ValidateMethodConcurrency(processor, r);
                }
            }
        }

        private void ValidateMethodConcurrency(ProcessorBase processor, Route r)
        {
            if (r.MethodMaxConcurrency.HasValue && r.MethodMaxConcurrency.Value > _maxConcurrency)
            {
                throw new InvalidOperationException(
                    $"[MaxConcurrency] 配置非法：{processor.GetType().Name}.{r.Method.Name} 标注 {r.MethodMaxConcurrency.Value} 超过 Processor 解析后的并发上限 {_maxConcurrency}");
            }
        }

        private static int ResolveMaxConcurrency(ProcessorBase processor, Server server)
        {
            // class 上 [MaxConcurrency(N)] 优先；否则用 Server 默认
            var attr = processor.GetType().GetCustomAttribute<MaxConcurrencyAttribute>(inherit: true);
            var n = attr?.Value ?? server.DefaultConcurrency;
            return Math.Max(1, n);
        }

        /// <summary>
        /// 启动期 lint：[MaxConcurrency] 误用检测（非致命，仅打 Warning）。
        /// 场景：
        /// 1. 方法标了 [MaxConcurrency] 但没有 [Request]/[Notify]：attribute 完全无效。
        /// 2. 方法 [MaxConcurrency] N 不大于 1：方法级限流写 1 是冗余（单 Processor 总闸已限制）。
        /// 3. Class 标了 [MaxConcurrency] 但类型继承链上没有 ProcessorBase：理论上走不到此处，留个兜底。
        /// 真正的类型级校验由 Roslyn analyzer 负责（后续补齐），这里是运行期兑底。
        /// </summary>
        private static void WarnOnMaxConcurrencyMisuse(ProcessorBase processor)
        {
            var type = processor.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var m in methods)
            {
                var mc = m.GetCustomAttribute<MaxConcurrencyAttribute>(inherit: true);
                if (mc == null) continue;

                var isRouteHandler =
                    m.GetCustomAttribute<RequestAttribute>(inherit: true) != null ||
                    m.GetCustomAttribute<NotifyAttribute>(inherit: true) != null;

                if (!isRouteHandler)
                {
                    Console.Error.WriteLine(
                        $"WARN [MaxConcurrency]: {type.Name}.{m.Name} 标了 [MaxConcurrency({mc.Value})] 但不是 [Request]/[Notify] 路由方法，attribute 将被忽略。");
                    continue;
                }

                if (mc.Value == 1)
                {
                    Console.Error.WriteLine(
                        $"WARN [MaxConcurrency]: {type.Name}.{m.Name} 标 [MaxConcurrency(1)] 是冗余：Processor 已是串行或上限已更小。考虑去掉该标注。");
                }
            }
        }

        public ProcessorBase Processor => _processor;

        public ConcurrentQueue<(uint, int, object)> BroadcastQueue => _broadcastQueue;

        public int PackageQueueCount => _incoming.Reader.Count;

        public Task RunTask => _runTask;

        public TaskStatus Status => _runTask?.Status ?? TaskStatus.Created;

        /// <summary>
        /// 投递消息。IO 线程调用，非阻塞。
        /// </summary>
        public bool Enqueue(Package pack)
        {
            if (_incoming.Writer.TryWrite(pack)) return true;

            // 满了：异步等位以保留 back-pressure 语义；不要在 IO 线程同步阻塞
            _ = WriteSlowAsync(pack);
            return false;
        }

        private async Task WriteSlowAsync(Package pack)
        {
            try
            {
                await _incoming.Writer.WriteAsync(pack, _linkedCts?.Token ?? CancellationToken.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (Exception err)
            {
                _server.OnErrorEvent(pack?.Header?.ClientId ?? IdLoopGenerator.INVALID, err);
            }
        }

        public void Start(CancellationToken serverToken)
        {
            _restartCts = new CancellationTokenSource();
            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverToken, _restartCts.Token);

            // 不能用 TaskCreationOptions.LongRunning：那会绑定一根独占线程，
            // 我们恰恰希望主循环 await 时把 OS 线程归还。
            var token = _linkedCts.Token;
            _runTask = _scheduler.StartNew(() => RunAsync(token), token).Unwrap();
        }

        /// <summary>
        /// 触发重启：取消当前主循环，等待结束。调用者负责再 Start。
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                _restartCts?.Cancel();
                _incoming.Writer.TryComplete();
                if (_runTask != null) await _runTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (AggregateException err) when (err.InnerException is OperationCanceledException) { }
            finally
            {
                _restartCts?.Dispose();
                _linkedCts?.Dispose();
            }
        }

        private async Task RunAsync(CancellationToken ct)
        {
            var reader = _incoming.Reader;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await DoPeriodicWorkAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception err)
                {
                    _server.OnErrorEvent(IdLoopGenerator.INVALID, err);
                }

                if (_processor.IsOnlyUpdate)
                {
                    if (!await SafeDelay(_processor.UpdateDeltaTime, ct).ConfigureAwait(false)) break;
                    continue;
                }

                // 取尽当前可读的包（最多 _drainBatchSize 个），保证周期性任务有机会跑
                var drained = 0;
                while (drained < _drainBatchSize && reader.TryRead(out var pack))
                {
                    drained++;
                    await DispatchOne(pack, ct).ConfigureAwait(false);
                    if (ct.IsCancellationRequested) break;
                }

                if (drained == 0)
                {
                    // 没消息：等待新消息或周期超时，然后回到顶部跑周期性任务
                    if (!await WaitForSignalAsync(_processor.RecvTimeout, ct).ConfigureAwait(false)) break;
                }
            }

            // 收尾：等所有 in-flight 完成（仅 MaxConcurrency>1 路径）
            if (_concurrencyLimiter != null)
            {
                for (var i = 0; i < _maxConcurrency; i++)
                {
                    try { await _concurrencyLimiter.WaitAsync(CancellationToken.None).ConfigureAwait(false); }
                    catch { }
                }
            }
        }

        private async Task DoPeriodicWorkAsync(CancellationToken ct)
        {
            await _server.Update(_processor).ConfigureAwait(false);
            await _server.ResolveBroadCast(_processor, _broadcastQueue).ConfigureAwait(false);
            await _processor.DoDeferCalls().ConfigureAwait(false);
            await _processor.DoDelayCalls().ConfigureAwait(false);
        }

        private Task DispatchOne(Package pack, CancellationToken ct)
        {
            if (_maxConcurrency == 1)
            {
                return ProcessOne(pack, ct);
            }

            return DispatchOneThrottled(pack, ct);
        }

        private async Task DispatchOneThrottled(Package pack, CancellationToken ct)
        {
            // Method 级限流：先 acquire，避免占用 Processor 并发名额去等方法槽位（防止头阻塞）
            SemaphoreSlim methodSem = null;
            var routeId = pack?.Header?.PackageInfo?.Route ?? 0;
            if (_routeById != null && _routeById.TryGetValue(routeId, out var route))
            {
                methodSem = route.MethodConcurrencySem; // 未标注 [MaxConcurrency] 时为 null
            }

            if (methodSem != null)
            {
                try { await methodSem.WaitAsync(ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }

            try
            {
                await _concurrencyLimiter.WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                methodSem?.Release();
                return;
            }

            _ = ProcessOneAndRelease(pack, ct, methodSem);
        }

        private async Task ProcessOneAndRelease(Package pack, CancellationToken ct, SemaphoreSlim methodSem)
        {
            try { await ProcessOne(pack, ct).ConfigureAwait(false); }
            finally
            {
                _concurrencyLimiter.Release();
                methodSem?.Release();
            }
        }

        private async Task ProcessOne(Package pack, CancellationToken ct)
        {
            try
            {
                var pre = _processor.OnPreRecv(pack);
                if (pre != null)
                {
                    _server.Send(pre);
                    _processor.OnPostSendResult(pre);
                    return;
                }

                var result = await _processor.Invoke(pack).ConfigureAwait(false);
                if (ct.IsCancellationRequested) return;

                if (result != null)
                {
                    _server.Send(result);
                    _processor.OnPostSendResult(result);
                }
            }
            catch (OperationCanceledException) { /* IGNORE */ }
            catch (AggregateException err) when (err.InnerException is OperationCanceledException || err.InnerException is TaskCanceledException) { /* IGNORE */ }
            catch (Exception err)
            {
                _server.OnErrorEvent(pack?.Header?.ClientId ?? IdLoopGenerator.INVALID, err);
            }
        }

        private async Task<bool> WaitForSignalAsync(TimeSpan timeout, CancellationToken ct)
        {
            var reader = _incoming.Reader;
            if (timeout <= TimeSpan.Zero)
            {
                try { return await reader.WaitToReadAsync(ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { return false; }
            }

            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            try
            {
                return await reader.WaitToReadAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                // 周期超时，回到顶部跑周期任务
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private static async Task<bool> SafeDelay(TimeSpan delay, CancellationToken ct)
        {
            try { await Task.Delay(delay, ct).ConfigureAwait(false); return true; }
            catch (OperationCanceledException) { return false; }
        }
    }
}
