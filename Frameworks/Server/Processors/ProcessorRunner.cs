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
    /// - 单 reader Channel 接收业务消息和跨 Processor 调用闭包（统一邮箱）
    /// - 主循环 async 化，业务 await 异步 I/O 时不再阻塞 OS 线程
    /// - MaxConcurrency==1（默认）严格串行，业务无需加锁
    /// - MaxConcurrency&gt;1 时使用 ExclusiveScheduler 限制同步代码段不并行
    ///
    /// 并发上限解析顺序：Processor class 上的 [MaxConcurrency(N)] -&gt; Server.DefaultConcurrency。
    ///
    /// 邮箱语义（本版本改动）：
    /// - 客户端请求（<see cref="Package"/>）和 <see cref="ProcessorRef{T}"/> 投递的跨 Processor 调用
    ///   闭包共享同一个 <see cref="_incoming"/> Channel，按 FIFO 公平排队。
    /// - <c>_maxConcurrency == 1</c> 路径下两者强制互斥，天然消除数据竞争。
    /// - <c>_maxConcurrency &gt; 1</c> 路径下两者共享同一把 <see cref="_concurrencyLimiter"/>，
    ///   总并发不会超过配置值。
    /// - 方法级 [MaxConcurrency] semaphore 对 Package 和 <see cref="ProcessorRef{T}"/> 闭包两条路径
    ///   共同生效：<c>ProcessorRef</c> 在投递闭包时带上 <c>routeKey</c>（和 <see cref="Routers.Route.RouteString"/>
    ///   完全一致的生成规则），Runner 按此 key 解析方法级 sem 并预缓存到 WorkItem，
    ///   dispatch 时和客户端路径共享同一把 semaphore。
    /// </summary>
    public sealed class ProcessorRunner
    {
        /// <summary>
        /// 邮箱工作项：客户端 Package 与跨 Processor 调用闭包的 union。
        /// 通过 <see cref="IsInvoke"/> 判别：<c>Work != null</c> 表示闭包模式，否则是 Package。
        ///
        /// 做成 readonly struct 是为了避免 channel 入队时的堆分配；
        /// Channel 内部会以值类型装箱策略存放（Channel&lt;T&gt; where T 是 struct 时零装箱）。
        /// </summary>
        internal readonly struct RunnerWorkItem
        {
            public readonly Package Pack;              // 非 null => 客户端/网络包路径
            public readonly Func<Task> Work;           // 非 null => 跨 Processor 调用闭包
            public readonly uint RouteId;              // Package 路径下 = Header.Route；Invoke 路径下 = 0
            public readonly SemaphoreSlim MethodSem;   // Invoke 路径下：投递时预先解析的方法级 sem（可为 null）

            public RunnerWorkItem(Package pack)
            {
                Pack = pack;
                Work = null;
                RouteId = pack?.Header?.PackageInfo?.Route ?? 0;
                MethodSem = null;
            }

            public RunnerWorkItem(Func<Task> work, SemaphoreSlim methodSem)
            {
                Pack = null;
                Work = work;
                RouteId = 0;
                MethodSem = methodSem;
            }

            public bool IsInvoke => Work != null;
        }

        private readonly ProcessorBase _processor;
        private readonly Server _server;
        private readonly Channel<RunnerWorkItem> _incoming;

        /// <summary>
        /// <c>_maxConcurrency == 1</c> 路径专用的同步邮箱（与 <see cref="_incoming"/> 二选一）。
        ///
        /// <para>
        /// 选用 <see cref="BlockingCollection{T}"/> 而非 <see cref="Channel{T}"/> 的核心理由是
        /// **不让 idle 等待路径产生异常**：<see cref="BlockingCollection{T}.TryTake(out T, int, CancellationToken)"/>
        /// 是 <see cref="SemaphoreSlim"/> 的内核 wait，timeout 到期返回 false 不抛异常，仅取消 token
        /// 才会抛 <see cref="OperationCanceledException"/>。
        /// </para>
        ///
        /// <para>
        /// 与之对比，<see cref="WaitForSignalAsync"/> 用 <c>Channel.WaitToReadAsync(linkedToken)</c> +
        /// <see cref="CancellationTokenSource"/>(<c>RecvTimeout</c>) 实现 timeout：idle 时每 <c>RecvTimeout</c>
        /// 抛一次 <see cref="OperationCanceledException"/> 做"超时信号"。N 个 Processor × 20Hz 量级的
        /// first-chance exception 在 IDE Debug 模式下会触发巨量 stack walk / filter 求值，把 debugger
        /// event 队列打爆——业务侧 EF Core / DI cold-path 实测被放大 100x+。
        /// </para>
        ///
        /// <para>
        /// 行为与 1c1cbe8 之前的 <c>BlockingCollection.TryTake(timeout)</c> 路径一致；
        /// <c>_maxConcurrency &gt; 1</c> 时仍走 <see cref="_incoming"/> Channel——流水线并发场景下
        /// async 归还 ThreadPool worker 是必要的，且这种 Processor 数量极少，异常风暴贡献可忽略。
        /// </para>
        /// </summary>
        private readonly BlockingCollection<RunnerWorkItem> _incomingSync;

        private readonly ConcurrentQueue<(uint, int, object)> _broadcastQueue;
        private readonly TaskFactory _scheduler;
        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly int _maxConcurrency;
        private readonly int _drainBatchSize;

        // 广播队列峰值观测（采样峰值，不是生产者侧真实峰值）：
        // - Runner 在每轮周期 tick 里顺手采样一次 _broadcastQueue.Count，若超过历史峰值则更新。
        // - 这是"机制"，不是"策略"：Runner 不做阈值判断、不打日志，外部根据 ProcessorStatus 自行决定。
        // - 由于 Runner 主循环（写入方）和外部监控（读取/重置方）在不同线程，使用 Interlocked 原子更新。
        private int _broadcastPeakDepth;

        /// <summary>
        /// 当前正在执行工作的 Runner。<see cref="ProcessorRef{T}"/> 用它做回环检测：
        /// 同一 Runner 上的再入调用直接 inline 执行，避免 mailbox 自等死锁。
        ///
        /// 由于主循环在 <c>Post</c>-投递的闭包里会 <c>await</c>，必须用 <see cref="AsyncLocal{T}"/>
        /// 而不是 <c>ThreadStatic</c>——跨 await 之后 OS 线程可能换，但执行上下文跟 Runner 保持一致。
        /// </summary>
        internal static readonly AsyncLocal<ProcessorRunner> _current = new AsyncLocal<ProcessorRunner>();
        internal static ProcessorRunner Current => _current.Value;

        /// <summary>
        /// 暴露给 <see cref="ProcessorRef{T}"/> 在 Notify 异常路径上调 <c>OnErrorEvent</c>。
        /// </summary>
        internal Server Server => _server;

        // routeId -> Route 的 O(1) 查表，用于在调度路径上拿到 Method 级 [MaxConcurrency] 配置。
        // 仅在 _maxConcurrency > 1 路径下使用（_maxConcurrency==1 已是严格串行，方法级限流是冗余）。
        private readonly Dictionary<uint, Route> _routeById;

        // routeKey -> 方法级 <see cref="SemaphoreSlim"/> 的 O(1) 查表。
        // 两个来源：
        // 1. [Request]/[Notify] 方法：key = <see cref="Route.RouteString"/>，
        //    即 "{processorName}.{methodName}".ToLower()，methodName 取自 [Request/Notify] attr name 或方法名
        // 2. 纯 [ProcessorApi] 方法（无 [Request]/[Notify]）：key 同样是
        //    "{processorName}.{methodName}".ToLower()，methodName fallback 到 C# 方法名
        // 以上 key 生成规则和 Generator.ProcessorRef.ComputeRouteKey / Route.GetRoute 完全一致，
        // 三方对齐，确保 ProcessorRef 侧传来的 routeKey 命中同一把 sem。
        //
        // 没有 [MaxConcurrency] 的方法不进该表（null sem 也不必存）——
        // <see cref="ResolveMethodSemaphore"/> 查不到时返回 null，表示无方法级限流。
        //
        // 仅在 _maxConcurrency > 1 路径下需要。
        private readonly Dictionary<string, SemaphoreSlim> _methodSemByKey;

        public int MaxConcurrency => _maxConcurrency;

        private CancellationTokenSource _restartCts;
        private CancellationTokenSource _linkedCts;
        private Task _runTask;

        public ProcessorRunner(
            ProcessorBase processor,
            Server server,
            int capacity = ushort.MaxValue,
            int drainBatchSize = 64)
        {
            _processor = processor ?? throw new ArgumentNullException(nameof(processor));
            _server = server ?? throw new ArgumentNullException(nameof(server));

            // 反向注入：DeferCall / DelayCall 用这个字段把闭包 Post 回本 Runner 的邮箱，
            // 保证跨线程调用时队列结构不会被撕裂。
            processor._runner = this;

            // 统一邮箱：客户端 Package 和跨 Processor 调用闭包共用同一个 Channel，
            // 严格 FIFO。这是让 [MaxConcurrency(1)] 在两条路径间真正互斥的关键。
            //
            // 容量选择沿用历史：Bounded(ushort.MaxValue)。
            // - 背压：网络洪峰下 Enqueue 会在 WriteSlowAsync 异步等位，不阻塞 IO 线程。
            // - 跨 Processor 调用也共享这个容量，极端情况下目标 Runner 卡住时，发起方的
            //   Post 会通过 WriteSlowAsync 异步等位；这是合并邮箱换来"严格限流保证"的代价。
            _broadcastQueue = new ConcurrentQueue<(uint, int, object)>();

            _maxConcurrency = ResolveMaxConcurrency(processor, server);
            _drainBatchSize = Math.Max(1, drainBatchSize);

            // 邮箱二选一：_maxConcurrency==1 走 BlockingCollection 同步路径（IDE Debug 友好），
            // _maxConcurrency>1 走 Channel async 路径（流水线归还 ThreadPool）。
            // 详见 _incomingSync 字段注释。
            if (_maxConcurrency == 1)
            {
                _incomingSync = new BlockingCollection<RunnerWorkItem>(capacity);
                _incoming = null;
            }
            else
            {
                _incoming = Channel.CreateBounded<RunnerWorkItem>(new BoundedChannelOptions(capacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.Wait,
                });
                _incomingSync = null;
            }

            // 启动期 lint：扫描 Processor 类型检测 [MaxConcurrency] 误用，仅打 Warning 不抛错
            WarnOnMaxConcurrencyMisuse(processor);

            if (_maxConcurrency > 1)
            {
                // 仅在允许流水线并发时才启用 ExclusiveScheduler，串行化同步代码段
                var pair = new ConcurrentExclusiveSchedulerPair();
                _scheduler = new TaskFactory(pair.ExclusiveScheduler);
                _concurrencyLimiter = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);

                // 构建 routeId 与 routeKey->sem 两张查表。
                _routeById = new Dictionary<uint, Route>();
                _methodSemByKey = new Dictionary<string, SemaphoreSlim>(StringComparer.Ordinal);

                // 来源 1：[Request]/[Notify] 路由方法（sem 已由 Route 构造器创建）
                foreach (var r in processor.GetRoutes())
                {
                    _routeById[r.RouteId] = r;
                    // RouteString 已经由 Route 构造时 ToLower 过（见 Route.GetRoute），
                    // ProcessorRef 侧传进来的 routeKey 也会是小写，Ordinal 比较足够。
                    if (r.MethodConcurrencySem != null && !string.IsNullOrEmpty(r.RouteString))
                    {
                        _methodSemByKey[r.RouteString] = r.MethodConcurrencySem;
                    }
                    ValidateMethodConcurrency(processor.GetType().Name, r.Method.Name, r.MethodMaxConcurrency);
                }

                // 来源 2：纯 [ProcessorApi]（非 [Request]/[Notify]）方法。
                // 给这类方法也建方法级 sem 表，让跨 Processor 调用路径按 routeKey 命中限流。
                // routeKey 生成规则："{processorName}.{methodName}".ToLower()，methodName fallback 到 C# 方法名——
                // 与 Generator.ProcessorRef.ComputeRouteKey 对纯 [ProcessorApi] 的 fallback 规则完全一致。
                RegisterProcessorApiMethodSems(processor);
            }
            else
            {
                _scheduler = new TaskFactory(TaskScheduler.Default);

                // 即使 processor 串行（_maxConcurrency==1），方法级标注若 > 1 也算误用，启动期直接报错。
                // 这里同时覆盖 [Request]/[Notify] 路由方法与纯 [ProcessorApi] 方法。
                foreach (var r in processor.GetRoutes())
                {
                    ValidateMethodConcurrency(processor.GetType().Name, r.Method.Name, r.MethodMaxConcurrency);
                }
                foreach (var (methodName, mcValue) in EnumerateProcessorApiOnlyMethodConcurrency(processor.GetType()))
                {
                    ValidateMethodConcurrency(processor.GetType().Name, methodName, mcValue);
                }
            }
        }

        /// <summary>
        /// 扫描 processor 类上的纯 <c>[ProcessorApi]</c>（无 <c>[Request]</c>/<c>[Notify]</c>）方法，
        /// 为标了 <c>[MaxConcurrency]</c> 的创建 <see cref="SemaphoreSlim"/> 并塞进
        /// <see cref="_methodSemByKey"/>。启动期同时做 Method &lt;= Class 的校验。
        /// </summary>
        private void RegisterProcessorApiMethodSems(ProcessorBase processor)
        {
            var type = processor.GetType();
            var procName = type.GetProcessorName();
            foreach (var (method, mcValue) in EnumerateProcessorApiOnlyMethods(type))
            {
                ValidateMethodConcurrency(type.Name, method.Name, mcValue);

                // key 与 Route.RouteString 采用同一套 ToLower + "." 规则，保证 ProcessorRef 侧一致。
                var key = (procName + "." + method.Name).ToLower();
                if (_methodSemByKey.ContainsKey(key))
                {
                    // 理论不可能（方法要么是 Request/Notify 要么是纯 ProcessorApi，不会同时），防御性兜底
                    continue;
                }
                _methodSemByKey[key] = new SemaphoreSlim(mcValue, mcValue);
            }
        }

        /// <summary>
        /// 枚举类型上"纯 [ProcessorApi] 且带 [MaxConcurrency]"的方法 (method, mcValue) 对，
        /// 供 <see cref="RegisterProcessorApiMethodSems"/> 和 <c>_maxConcurrency==1</c> 路径的启动期校验共用。
        /// </summary>
        private static IEnumerable<(MethodInfo Method, int McValue)> EnumerateProcessorApiOnlyMethods(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var m in methods)
            {
                if (m.GetCustomAttribute<ProcessorApiAttribute>(inherit: true) == null) continue;
                if (m.GetCustomAttribute<RequestAttribute>(inherit: true) != null) continue;
                if (m.GetCustomAttribute<NotifyAttribute>(inherit: true) != null) continue;

                var mc = m.GetCustomAttribute<MaxConcurrencyAttribute>(inherit: true);
                if (mc == null) continue;

                yield return (m, mc.Value);
            }
        }

        /// <summary>
        /// 简化版：只返回 (methodName, mcValue) 用于纯校验场景（不需要 MethodInfo）。
        /// </summary>
        private static IEnumerable<(string MethodName, int McValue)> EnumerateProcessorApiOnlyMethodConcurrency(Type type)
        {
            foreach (var (m, v) in EnumerateProcessorApiOnlyMethods(type))
            {
                yield return (m.Name, v);
            }
        }

        /// <summary>
        /// 启动期校验：方法级 <c>[MaxConcurrency]</c> 不得超过 Processor 级（不论来源是 Route 还是纯 ProcessorApi）。
        /// 超过即抛 <see cref="InvalidOperationException"/>，由上层处理。
        /// </summary>
        private void ValidateMethodConcurrency(string typeName, string methodName, int? mcValue)
        {
            if (!mcValue.HasValue) return;
            if (mcValue.Value > _maxConcurrency)
            {
                throw new InvalidOperationException(
                    $"[MaxConcurrency] 配置非法：{typeName}.{methodName} 标注 {mcValue.Value} 超过 Processor 解析后的并发上限 {_maxConcurrency}");
            }
        }

        // 兼容原签名的重载，避免改动调用点
        private void ValidateMethodConcurrency(string typeName, string methodName, int mcValue)
            => ValidateMethodConcurrency(typeName, methodName, (int?)mcValue);

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
        /// 1. 方法标了 [MaxConcurrency] 但既不是 [Request]/[Notify] 也不是 [ProcessorApi]：
        ///    该 attribute 不会被任何路径消费，完全无效。
        /// 2. 方法 [MaxConcurrency(1)] <b>且</b> class 级 <c>_maxConcurrency &lt;= 1</c>：
        ///    Processor 本身已经串行，方法级再标 1 纯冗余。
        ///    class 级 > 1 时，方法级标 1 是合法的"单人通道"配置，<b>不</b>告警。
        /// 真正的类型级校验由 Roslyn analyzer (MAXCONC001/002/003) 负责，这里是运行期兜底。
        ///
        /// 合法的"可挂 [MaxConcurrency]"身份集：
        /// - [Request] / [Notify]（客户端请求路径 + 若同时 [ProcessorApi] 也覆盖跨 Processor 调用）
        /// - 纯 [ProcessorApi]（仅跨 Processor 调用路径）——本 runner 会为其建立方法级 sem。
        /// </summary>
        private void WarnOnMaxConcurrencyMisuse(ProcessorBase processor)
        {
            var type = processor.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var m in methods)
            {
                var mc = m.GetCustomAttribute<MaxConcurrencyAttribute>(inherit: true);
                if (mc == null) continue;

                var hasRequest      = m.GetCustomAttribute<RequestAttribute>(inherit: true) != null;
                var hasNotify       = m.GetCustomAttribute<NotifyAttribute>(inherit: true) != null;
                var hasProcessorApi = m.GetCustomAttribute<ProcessorApiAttribute>(inherit: true) != null;
                var isLimitable     = hasRequest || hasNotify || hasProcessorApi;

                if (!isLimitable)
                {
                    Console.Error.WriteLine(
                        $"WARN [MaxConcurrency]: {type.Name}.{m.Name} 标了 [MaxConcurrency({mc.Value})] " +
                        $"但既不是 [Request]/[Notify]，也不是 [ProcessorApi]，attribute 不会生效。");
                    continue;
                }

                // 方法级 N==1 仅在 processor 级也 <= 1 时才算冗余；
                // class > 1 + method == 1 是"单人通道"语义（跨客户端和 ProcessorRef 共享的总 in-flight=1），
                // 这是方法级标 1 唯一有意义的用途，不应警告。
                if (mc.Value == 1 && _maxConcurrency <= 1)
                {
                    Console.Error.WriteLine(
                        $"WARN [MaxConcurrency]: {type.Name}.{m.Name} 标 [MaxConcurrency(1)] 冗余：" +
                        $"Processor 级 _maxConcurrency={_maxConcurrency}，本身已经串行。请移除该标注。");
                }
            }
        }

        public ProcessorBase Processor => _processor;

        public ConcurrentQueue<(uint, int, object)> BroadcastQueue => _broadcastQueue;

        /// <summary>
        /// 广播队列的当前深度（瞬时值）。多线程安全。
        /// </summary>
        public int BroadcastQueueCount => _broadcastQueue.Count;

        /// <summary>
        /// 广播队列的采样峰值深度：自 Runner 启动（或上次 <see cref="ResetBroadcastPeakDepth"/>）以来，
        /// Runner 在周期 tick 里观察到的最大深度。
        /// <para>
        /// 采样非生产者侧，两次 tick 之间的瞬时尖峰可能被错过。
        /// 这个指标用于回答："这段时间里，消费端落后得最严重的一刻有多严重？"
        /// 外部监控可据此决定是否打日志 / 告警 / 熔断，<b>Runner 本身不做任何策略判断</b>。
        /// </para>
        /// </summary>
        public int BroadcastPeakDepth => Volatile.Read(ref _broadcastPeakDepth);

        /// <summary>
        /// 原子地读出当前采样峰值并重置为当前队列深度。
        /// <para>
        /// 推荐的采样窗口用法：外部监控周期性调用，得到"自上次采样以来的窗口峰值"，
        /// 用于滑动窗口统计，而不是"自 Runner 启动以来的历史峰值"。
        /// </para>
        /// <para>
        /// 重置为 <see cref="BroadcastQueueCount"/> 而不是 0，是为了保证重置后立即读到的峰值
        /// 不低于真实瞬时深度——否则下一轮 Update 前会有一段"峰值低于实际"的窗口，
        /// 外部若在此刻读取会被误导。
        /// </para>
        /// </summary>
        public int SampleAndResetBroadcastPeakDepth()
        {
            var floor = _broadcastQueue.Count;
            return Interlocked.Exchange(ref _broadcastPeakDepth, floor);
        }

        /// <summary>
        /// 将采样峰值重置为当前队列深度。等价于丢弃 <see cref="SampleAndResetBroadcastPeakDepth"/> 的返回值。
        /// </summary>
        public void ResetBroadcastPeakDepth() => Interlocked.Exchange(ref _broadcastPeakDepth, _broadcastQueue.Count);

        /// <summary>
        /// 邮箱当前深度（瞬时值）。包括客户端 Package 和跨 Processor 调用闭包的总和。
        /// 历史版本仅统计 Package；合并邮箱后语义变更为"邮箱总深度"，更准确反映消费压力。
        /// </summary>
        public int PackageQueueCount => _incomingSync != null ? _incomingSync.Count : _incoming.Reader.Count;

        public Task RunTask => _runTask;

        public TaskStatus Status => _runTask?.Status ?? TaskStatus.Created;

        /// <summary>
        /// 投递客户端消息到邮箱。IO 线程调用，非阻塞。
        /// </summary>
        public bool Enqueue(Package pack)
        {
            var item = new RunnerWorkItem(pack);
            if (_incomingSync != null)
            {
                // 同步邮箱：BlockingCollection 容量上限是构造时给的 capacity（默认 ushort.MaxValue），
                // 满时 TryAdd 会立即返回 false。背压交给上层 Server 决定（IO 线程不应阻塞）。
                if (_incomingSync.IsAddingCompleted) return false;
                try
                {
                    return _incomingSync.TryAdd(item);
                }
                catch (InvalidOperationException)
                {
                    // CompleteAdding 与 TryAdd 之间的极小竞态：吞掉，等同投递失败
                    return false;
                }
            }

            if (_incoming.Writer.TryWrite(item)) return true;

            // 满了：异步等位以保留 back-pressure 语义；不要在 IO 线程同步阻塞
            _ = WriteSlowAsync(item);
            return false;
        }

        /// <summary>
        /// 跨 Processor 投递一段异步工作到本 Runner 邮箱串行执行。
        /// 由 <see cref="ProcessorBase.DeferCall"/> / <see cref="ProcessorBase.DelayCall"/> 以及
        /// <see cref="ProcessorRef{T}"/> 无 routeKey 的重载调用；业务代码不应直接使用。
        /// </summary>
        /// <remarks>
        /// 闭包与客户端 Package 共享 <see cref="_incoming"/>，按 FIFO 公平排队。
        /// 容量耗尽时通过 <see cref="WriteSlowAsync"/> 异步等位，保持 back-pressure；
        /// 若业务上确实需要在发起侧限流，应在调用方加显式 <c>SemaphoreSlim</c>。
        /// <para>
        /// 本重载没有 routeKey 信息，闭包仅受 Processor 级 <see cref="_concurrencyLimiter"/> 约束，
        /// 不走方法级 sem。<see cref="ProcessorRef{T}"/> 由 Source Generator 生成的扩展方法会走
        /// <see cref="Post(Func{Task}, string)"/> 重载以启用方法级限流。
        /// </para>
        /// </remarks>
        internal void Post(Func<Task> work)
        {
            if (work == null) return;
            var item = new RunnerWorkItem(work, methodSem: null);
            if (_incomingSync != null)
            {
                if (_incomingSync.IsAddingCompleted) return;
                try { _incomingSync.TryAdd(item); }
                catch (InvalidOperationException) { /* 关闭中 */ }
                return;
            }
            if (!_incoming.Writer.TryWrite(item))
            {
                _ = WriteSlowAsync(item);
            }
        }

        /// <summary>
        /// 跨 Processor 投递带 <paramref name="routeKey"/> 的异步工作到本 Runner 邮箱。
        /// Runner 在投递时按 <paramref name="routeKey"/> 查 <see cref="_methodSemByKey"/>，
        /// 若命中（方法标了 <c>[MaxConcurrency]</c>）就把对应 <see cref="SemaphoreSlim"/>
        /// 预缓存到 <see cref="RunnerWorkItem.MethodSem"/>；dispatch 阶段该 sem 和客户端 Package
        /// 路径共享，跨 Processor 调用和客户端请求之间按方法级并发上限共同受限。
        /// <para>
        /// <paramref name="routeKey"/> 的生成方案与 <see cref="Routers.Route.RouteString"/> 及
        /// <see cref="RegisterProcessorApiMethodSems"/> 三方对齐：<c>"{processorName}.{methodName}".ToLower()</c>，
        /// processorName 取自 <c>[Processor("name")]</c>（缺省 type.Name），
        /// methodName 取自 <c>[Request("name")]</c>/<c>[Notify("name")]</c>（缺省 method.Name）；
        /// 纯 <c>[ProcessorApi]</c> 方法因为没有 Request/Notify attr，直接 fallback 到 C# 方法名。
        /// 这样无论方法来自哪一路都能命中同一把 sem。传 null/空/找不到时回落到"无方法级限流"。
        /// </para>
        /// </summary>
        internal void Post(Func<Task> work, string routeKey)
        {
            if (work == null) return;
            var item = new RunnerWorkItem(work, ResolveMethodSemaphore(routeKey));
            if (_incomingSync != null)
            {
                if (_incomingSync.IsAddingCompleted) return;
                try { _incomingSync.TryAdd(item); }
                catch (InvalidOperationException) { /* 关闭中 */ }
                return;
            }
            if (!_incoming.Writer.TryWrite(item))
            {
                _ = WriteSlowAsync(item);
            }
        }

        /// <summary>
        /// 按 routeKey 解析方法级 <see cref="SemaphoreSlim"/>；找不到返回 null。
        /// _maxConcurrency == 1 时 Runner 已严格串行，不建 <see cref="_methodSemByKey"/>，直接 null。
        /// </summary>
        private SemaphoreSlim ResolveMethodSemaphore(string routeKey)
        {
            if (_methodSemByKey == null) return null;
            if (string.IsNullOrEmpty(routeKey)) return null;
            return _methodSemByKey.TryGetValue(routeKey, out var sem) ? sem : null;
        }

        private async Task WriteSlowAsync(RunnerWorkItem item)
        {
            try
            {
                await _incoming.Writer.WriteAsync(item, _linkedCts?.Token ?? CancellationToken.None).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (ChannelClosedException) { /* shutting down */ }
            catch (Exception err)
            {
                var clientId = item.Pack?.Header?.ClientId ?? IdLoopGenerator.INVALID;
                _server.OnErrorEvent(clientId, err);
            }
        }

        public void Start(CancellationToken serverToken)
        {
            _restartCts = new CancellationTokenSource();
            _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(serverToken, _restartCts.Token);

            var token = _linkedCts.Token;

            // _maxConcurrency == 1：独占 OS 线程 + 同步邮箱。详见 _incomingSync / RunSyncLoop 注释。
            // _maxConcurrency > 1：保持 ExclusiveScheduler + Channel async 路径不变。
            if (_maxConcurrency == 1)
            {
                _runTask = Task.Factory.StartNew(
                    () => RunSyncLoop(token),
                    token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
            else
            {
                _runTask = _scheduler.StartNew(() => RunAsync(token), token).Unwrap();
            }
        }

        /// <summary>
        /// 同步阻塞版主循环：仅 <c>_maxConcurrency == 1</c> 路径使用，跑在 LongRunning 独占线程上，
        /// idle 用 <see cref="BlockingCollection{T}.TryTake(out RunnerWorkItem, int, CancellationToken)"/>
        /// 做内核 wait——整条 idle 路径不走 async/await、不抛异常、不创建 state machine。
        /// 详见 <see cref="_incomingSync"/> 注释。
        ///
        /// <para>
        /// 有消息时 <see cref="DispatchWorkItem"/> 仍是 async（业务必要），通过
        /// <see cref="Task.Wait(CancellationToken)"/> 折叠为同步阻塞执行；业务 await 后的 continuation
        /// 仍按 default scheduler 回 ThreadPool。
        /// </para>
        /// </summary>
        private void RunSyncLoop(CancellationToken ct)
        {
            _current.Value = this;

            var queue = _incomingSync;
            // idle 唤醒间隔：用 RecvTimeout（默认 50ms）跑一次周期任务循环，
            // 与 1c1cbe8 之前的 BlockingCollection.TryTake(RecvTimeout) 行为一致。
            var idleTimeoutMs = (int)_processor.RecvTimeout.TotalMilliseconds;
            if (idleTimeoutMs <= 0) idleTimeoutMs = 1;

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    DoPeriodicWorkAsync(ct).Wait(ct);
                }
                catch (OperationCanceledException) { break; }
                catch (AggregateException err) when (err.InnerException is OperationCanceledException) { break; }
                catch (Exception err)
                {
                    _server.OnErrorEvent(IdLoopGenerator.INVALID, err);
                }

                if (ct.IsCancellationRequested) break;

                // drain：从 _incomingSync 一次最多消费 _drainBatchSize 条
                var drained = 0;
                while (drained < _drainBatchSize)
                {
                    if (!TryTakeNonBlocking(queue, out var item)) break;
                    drained++;
                    try
                    {
                        DispatchWorkItem(item, ct).Wait(ct);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (AggregateException err) when (err.InnerException is OperationCanceledException) { break; }
                    catch (Exception err)
                    {
                        // DispatchWorkItem 内部已经把每条业务异常落到 OnErrorEvent；
                        // 能冒到这里基本上是 Cancel 之外的奇葩，兜底。
                        var clientId = item.Pack?.Header?.ClientId ?? IdLoopGenerator.INVALID;
                        _server.OnErrorEvent(clientId, err);
                    }
                    if (ct.IsCancellationRequested) break;
                }

                if (ct.IsCancellationRequested) break;

                if (drained == 0)
                {
                    // 优雅停机：邮箱已 CompleteAdding 且为空
                    if (queue.IsCompleted)
                    {
                        if (_broadcastQueue.IsEmpty) break;
                        // broadcast 还有残留：让下一轮 DoPeriodicWorkAsync 的 ResolveBroadCast 继续消费。
                        // 用纯线程 sleep，不走 Task.Delay 的 async 路径。
                        try { Thread.Sleep(1); }
                        catch (ThreadInterruptedException) { break; }
                        continue;
                    }

                    if (_processor.IsOnlyUpdate)
                    {
                        // IsOnlyUpdate：定步长 tick，不等邮箱信号。纯 Thread.Sleep，不走 async。
                        var deltaMs = (int)_processor.UpdateDeltaTime.TotalMilliseconds;
                        if (deltaMs <= 0) deltaMs = 1;
                        try { Thread.Sleep(deltaMs); }
                        catch (ThreadInterruptedException) { break; }
                        continue;
                    }

                    // 内核 wait：timeout 返回 false 不抛异常；ct 取消才抛 OCE。
                    bool got;
                    RunnerWorkItem item;
                    try
                    {
                        got = queue.TryTake(out item, idleTimeoutMs, ct);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (InvalidOperationException)
                    {
                        // queue 已 CompleteAdding 且 empty：等同 IsCompleted 路径
                        continue;
                    }

                    if (got)
                    {
                        try
                        {
                            DispatchWorkItem(item, ct).Wait(ct);
                        }
                        catch (OperationCanceledException) { break; }
                        catch (AggregateException err) when (err.InnerException is OperationCanceledException) { break; }
                        catch (Exception err)
                        {
                            var clientId = item.Pack?.Header?.ClientId ?? IdLoopGenerator.INVALID;
                            _server.OnErrorEvent(clientId, err);
                        }
                    }
                    // 不论 got 与否，回到 loop 顶继续周期任务
                }
            }

            // 收尾：与 RunAsync 一致——把邮箱里残留的"跨 Processor 调用闭包"跑完，
            // 避免 TaskCompletionSource 永远悬挂导致发起方 await 永不返回。
            DrainRemainingInvokesSync();
        }

        /// <summary>
        /// 非阻塞 take：仅消费已经在队列里的消息，不阻塞等待新的。
        /// <see cref="BlockingCollection{T}.TryTake(out T)"/> 的 0 超时变体。
        /// </summary>
        private static bool TryTakeNonBlocking(BlockingCollection<RunnerWorkItem> queue, out RunnerWorkItem item)
        {
            try
            {
                return queue.TryTake(out item);
            }
            catch (InvalidOperationException)
            {
                item = default;
                return false;
            }
        }

        /// <summary>
        /// <see cref="DrainRemainingInvokesAsync"/> 的同步版本，专供 <see cref="RunSyncLoop"/> 收尾使用，
        /// 避免在独占线程关停路径上再开 async state machine。仅消费 Invoke 闭包，跳过 Package。
        /// </summary>
        private void DrainRemainingInvokesSync()
        {
            if (_incomingSync == null) return;
            while (TryTakeNonBlocking(_incomingSync, out var item))
            {
                if (!item.IsInvoke) continue;
                try
                {
                    item.Work().Wait();
                }
                catch (OperationCanceledException) { /* IGNORE */ }
                catch (AggregateException err) when (err.InnerException is OperationCanceledException || err.InnerException is TaskCanceledException) { /* IGNORE */ }
                catch (Exception err)
                {
                    _server.OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
            }
        }

        /// <summary>
        /// 触发重启/强制停机：取消当前主循环，等待结束。调用者负责再 Start。
        /// <para>
        /// 语义："立即"——Cancel token 打断主循环任何 await；<see cref="_incoming"/> 里
        /// 未消费的条目会丢失（只有 invoke 闭包会在主循环退出 tail 里被尝试 drain 一次，
        /// 保证发起方 TCS 不会永远悬挂）。
        /// 若希望让 Runner 先把残留吃完再停，请改用 <see cref="StopAsync(TimeSpan)"/>。
        /// </para>
        /// </summary>
        public async Task StopAsync()
        {
            try
            {
                _restartCts?.Cancel();
                CompleteMailbox();
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

        /// <summary>
        /// 关闭邮箱写入端：依据当前 Runner 用的是 Channel（多并发路径）还是 BlockingCollection（单并发路径）
        /// 选择正确的 "no more producers" 信号，避免空指针。
        /// </summary>
        private void CompleteMailbox()
        {
            if (_incomingSync != null)
            {
                try { _incomingSync.CompleteAdding(); }
                catch (ObjectDisposedException) { /* 已停 */ }
            }
            else
            {
                _incoming?.Writer.TryComplete();
            }
        }

        /// <summary>
        /// 优雅停机：先关闭 Writer（不再接受新 pack / 新闭包），
        /// 然后给主循环至多 <paramref name="gracefulDrainTimeout"/> 时间把残留吃干净：
        /// <list type="bullet">
        ///   <item><see cref="_incoming"/> 里已排队的 Package 和跨 Processor 调用闭包</item>
        ///   <item><see cref="_broadcastQueue"/> 里已排队的广播事件</item>
        /// </list>
        /// 超时到期仍未退出则硬 Cancel（取消 token），打断任何 in-flight 的 await。
        /// <para>
        /// 注意：<see cref="_broadcastQueue"/> 是 <see cref="ConcurrentQueue{T}"/>，没有 "complete" 标记；
        /// 如果调用期间仍有生产者往里 Enqueue，超时兜底是唯一保护。调用方应在 <see cref="StopAsync(TimeSpan)"/>
        /// 前保证 broadcast 的生产者已经静默（典型位置：Server.Stop 中 DrainActiveClients 之后）。
        /// </para>
        /// <para>
        /// <see cref="ProcessorBase.DelayCall"/> 注册的未到期任务**不会**被强制执行——它们绑定了未来的 DateTime，
        /// 优雅窗口内只有到期的会被 <see cref="ProcessorBase.DoDelayCalls"/> 正常消费。
        /// </para>
        /// </summary>
        public async Task StopAsync(TimeSpan gracefulDrainTimeout)
        {
            if (_runTask == null)
            {
                CompleteMailbox();
                _restartCts?.Dispose();
                _linkedCts?.Dispose();
                return;
            }

            try
            {
                // 1. 关闭输入端：主循环读到空后会自动通过 RunAsync 的 graceful-exit 分支退出。
                CompleteMailbox();

                // 2. 最多等 gracefulDrainTimeout，让主循环把邮箱和广播吃干净自然退出。
                var graceful = Task.WhenAny(_runTask, Task.Delay(gracefulDrainTimeout)).ConfigureAwait(false);
                var finished = await graceful;

                if (finished != _runTask)
                {
                    // 3. 窗口用完主循环还没退：硬 Cancel 打断 in-flight await，等它收尾。
                    _restartCts?.Cancel();
                    try { await _runTask.ConfigureAwait(false); }
                    catch (OperationCanceledException) { }
                    catch (AggregateException err) when (err.InnerException is OperationCanceledException) { }
                }
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
            // 进入主循环即把当前 AsyncLocal 设为本 Runner：
            // 回环 inline 检测（ProcessorRef.Request 判断 Current == Runner）在周期性任务、
            // 广播、DeferCall / DelayCall 里都能生效，语义和"消息处理"一致。
            _current.Value = this;

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

                if (ct.IsCancellationRequested) break;

                // 统一 drain：Package 和 Invoke 闭包都在 _incoming 里按 FIFO 排队，
                // 交由 DispatchWorkItem 按 item 类型分派到对应路径。
                var drained = 0;
                while (drained < _drainBatchSize && reader.TryRead(out var item))
                {
                    drained++;
                    await DispatchWorkItem(item, ct).ConfigureAwait(false);
                    if (ct.IsCancellationRequested) break;
                }

                if (ct.IsCancellationRequested) break;

                if (drained == 0)
                {
                    // 优雅停机：writer 已 TryComplete 且 channel 已读空
                    if (reader.Completion.IsCompleted)
                    {
                        // broadcast 也空 -> 邮箱与广播都 drained，干净退出
                        if (_broadcastQueue.IsEmpty) break;

                        // broadcast 还有残留：不能进 WaitForSignalAsync（reader 已完成，
                        // 它会立刻返回 false 误判为退出）。让主循环 continue 回顶部，
                        // 由下一轮 DoPeriodicWorkAsync 的 ResolveBroadCast 继续消费。
                        try { await Task.Delay(1, ct).ConfigureAwait(false); }
                        catch (OperationCanceledException) { break; }
                        continue;
                    }

                    if (_processor.IsOnlyUpdate)
                    {
                        // IsOnlyUpdate 的 Processor 正常情况下邮箱只会被跨 Processor 调用闭包填充。
                        // 此处使用定步长 tick，而不是 RecvTimeout：Update / Broadcast / DeferCalls
                        // 是主要工作，邮箱来消息时 WaitToReadAsync 没被等待，要等下一轮 tick 才处理。
                        // 需要更低延迟时，业务侧应通过 DeferCall 主动触发，或者不要用 IsOnlyUpdate 标志。
                        if (!await SafeDelay(_processor.UpdateDeltaTime, ct).ConfigureAwait(false)) break;
                        continue;
                    }

                    // 常规：等待邮箱来新消息或周期超时
                    if (!await WaitForSignalAsync(_processor.RecvTimeout, ct).ConfigureAwait(false))
                    {
                        // 返回 false 的场景：
                        //   (a) ct 被 Cancel（硬停机路径）
                        //   (b) 在 Wait 期间 StopAsync(TimeSpan) 关掉了 writer，让 WaitToReadAsync 返 false
                        // 对于 (b)，必须回到 loop 顶让上面的"优雅停机"分支来判定 broadcast 是否还需要 drain；
                        // 直接 break 会把 _broadcastQueue 里的残留丢掉。
                        if (!ct.IsCancellationRequested && reader.Completion.IsCompleted)
                        {
                            continue;
                        }
                        break;
                    }
                }
            }

            // 收尾：把邮箱里残留的"跨 Processor 调用闭包"跑完，避免 TaskCompletionSource 永远悬挂
            // 导致发起方 await 永不返回。Package 不再处理（客户端侧已经断开/或被上游重试覆盖）。
            // 不再受 ct 约束——关闭流程里发起方已经不关心 cancel 了。
            await DrainRemainingInvokesAsync().ConfigureAwait(false);

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

        /// <summary>
        /// 停机收尾：把邮箱里残留的 invoke 闭包顺序跑完，Package 直接跳过。
        /// 目的是让 <see cref="ProcessorRef{T}.Request"/> 返回的 <see cref="Task"/> 能够 resolve，
        /// 不至于让调用方 await 永久悬挂（TCS 没被 TrySetResult / TrySetException 会泄漏）。
        /// </summary>
        private async Task DrainRemainingInvokesAsync()
        {
            var reader = _incoming.Reader;
            while (reader.TryRead(out var item))
            {
                if (!item.IsInvoke) continue;
                try
                {
                    await item.Work().ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* IGNORE */ }
                catch (AggregateException err) when (err.InnerException is OperationCanceledException || err.InnerException is TaskCanceledException) { /* IGNORE */ }
                catch (Exception err)
                {
                    // ProcessorRef.Request 已经在闭包内部 try/catch 把异常写进 TCS；
                    // 能冒到这里的基本上是 Notify 闭包里 OnErrorEvent 再抛的极端情况，兜底。
                    _server.OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
            }
        }

        private async Task DoPeriodicWorkAsync(CancellationToken ct)
        {
            await _server.Update(_processor).ConfigureAwait(false);
            // 和 mailbox drain 用同一个 batch 上限：一次 tick 内最多消费 _drainBatchSize 条广播，
            // 剩余留给下一轮，避免广播洪峰独占 Runner 周期 tick 把其他周期任务（DeferCalls/DelayCalls）饿死。
            await _server.ResolveBroadCast(_processor, _broadcastQueue, _drainBatchSize).ConfigureAwait(false);
            // 在 drain 之后采样：残留深度反映"本轮消费不掉的部分"，是消费速度跟不上的直接信号。
            // 这里只做记录，不做决策——策略（是否告警 / 是否熔断）由外部根据 ProcessorStatus 自行判断。
            UpdateBroadcastPeak();
            await _processor.DoDeferCalls().ConfigureAwait(false);
            await _processor.DoDelayCalls().ConfigureAwait(false);
        }

        /// <summary>
        /// 采样一次广播队列深度，若超过当前记录的峰值则更新。
        /// <para>
        /// 这是"采样峰值"不是"真实峰值"：Runner 只在周期 tick 里采一次，
        /// 两次 tick 之间的瞬时尖峰可能被错过（要真实峰值就必须在生产者侧 <c>Server.Broadcast</c>
        /// 每次 Enqueue 后更新，代价是每次广播多一次原子操作，不划算）。
        /// </para>
        /// <para>
        /// 写入方是 Runner 主循环（单线程），读取/重置方是外部监控线程，
        /// 使用 <see cref="Interlocked.CompareExchange(ref int, int, int)"/> 做无锁更新。
        /// </para>
        /// </summary>
        private void UpdateBroadcastPeak()
        {
            var current = _broadcastQueue.Count;
            while (true)
            {
                var old = Volatile.Read(ref _broadcastPeakDepth);
                if (current <= old) return;
                if (Interlocked.CompareExchange(ref _broadcastPeakDepth, current, old) == old) return;
                // CAS 失败说明外部并发地执行了 Reset 或另一次 Update（理论上单写者不会发生，
                // 但 Reset 可能把 old 改小），重试直到稳定。
            }
        }

        /// <summary>
        /// 根据 WorkItem 类型分派：Package 走消息处理路径，Invoke 闭包走跨 Processor 调用路径。
        /// 两条路径在 _maxConcurrency &gt; 1 时共享同一把 <see cref="_concurrencyLimiter"/>
        /// 以及（若命中）同一把方法级 <see cref="SemaphoreSlim"/>，
        /// 保证"总 in-flight 数 ≤ MaxConcurrency"且"每方法 in-flight 数 ≤ method 级上限"。
        /// </summary>
        private Task DispatchWorkItem(RunnerWorkItem item, CancellationToken ct)
        {
            if (item.IsInvoke)
            {
                return DispatchInvoke(item.Work, item.MethodSem, ct);
            }

            if (_maxConcurrency == 1)
            {
                return ProcessOne(item.Pack, ct);
            }

            return DispatchPackageThrottled(item.Pack, ct);
        }

        private async Task DispatchPackageThrottled(Package pack, CancellationToken ct)
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

            _ = ProcessPackageAndRelease(pack, ct, methodSem);
        }

        private async Task ProcessPackageAndRelease(Package pack, CancellationToken ct, SemaphoreSlim methodSem)
        {
            try { await ProcessOne(pack, ct).ConfigureAwait(false); }
            finally
            {
                _concurrencyLimiter.Release();
                methodSem?.Release();
            }
        }

        /// <summary>
        /// 跨 Processor 调用闭包的分派：
        /// - <c>_maxConcurrency == 1</c>：直接在主循环里 <c>await</c>，严格串行，和客户端 Package 互斥。
        ///   此路径下 <paramref name="methodSem"/> 冗余（Runner 本身已串行），忽略。
        /// - <c>_maxConcurrency &gt; 1</c>：先 acquire <paramref name="methodSem"/>（若非 null），
        ///   再 acquire <see cref="_concurrencyLimiter"/>，fire-and-forget 执行。
        ///   方法级 sem 和客户端 Package 路径 (<see cref="DispatchPackageThrottled"/>) 共享——
        ///   跨 Processor 调用与客户端请求之间按方法级上限共同受限。
        /// </summary>
        private async Task DispatchInvoke(Func<Task> work, SemaphoreSlim methodSem, CancellationToken ct)
        {
            if (_maxConcurrency == 1)
            {
                try
                {
                    await work().ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* IGNORE */ }
                catch (AggregateException err) when (err.InnerException is OperationCanceledException || err.InnerException is TaskCanceledException) { /* IGNORE */ }
                catch (Exception err)
                {
                    // ProcessorRef.Request 已经在闭包内部把异常写进 TCS；
                    // 能冒到这里的是 Notify 闭包里 OnErrorEvent 再抛的极端情况。
                    _server.OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
                return;
            }

            // method 级先行：和 DispatchPackageThrottled 保持相同顺序（先 methodSem 再 limiter），
            // 避免占用 processor 并发名额去等方法槽位。
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

            _ = RunInvokeAndRelease(work, methodSem);
        }

        private async Task RunInvokeAndRelease(Func<Task> work, SemaphoreSlim methodSem)
        {
            try
            {
                await work().ConfigureAwait(false);
            }
            catch (OperationCanceledException) { /* IGNORE */ }
            catch (AggregateException err) when (err.InnerException is OperationCanceledException || err.InnerException is TaskCanceledException) { /* IGNORE */ }
            catch (Exception err)
            {
                _server.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
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

        /// <summary>
        /// 等邮箱来新消息，或 timeout 过期回去跑周期任务。
        /// </summary>
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
