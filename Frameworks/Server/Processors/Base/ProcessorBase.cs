using System.Collections.Concurrent;
using GoPlay.Core.Protocols;
using GoPlay.Core.Routers;
using GoPlay.Core.Utils;

namespace GoPlay.Core.Processors
{
    public abstract class ProcessorBase
    {
        public const uint UNKOWN_ROUTE_ID = uint.MaxValue;
        
        public static IdLoopGenerator IdGenerator = new IdLoopGenerator(uint.MaxValue);
        
        protected List<Route> m_routes;
        protected Dictionary<string, uint> m_pushDict;
        protected List<(string, uint, ServerTag)> m_routeIdDict;

        // 仅在 Runner 自己的线程上下文里被读写。外部提交 DelayCall 时走 Runner.Post 进入本 Runner
        // 线程后再 Add，保持单线程访问，避免 List 结构撕裂。
        protected List<(DateTime, Func<Task>)> m_delayTasks;
        
        internal DateTime LastUpdate = DateTime.UtcNow;
        public virtual TimeSpan UpdateDeltaTime => Consts.TimeOut.Update;
        public virtual TimeSpan RecvTimeout => Consts.TimeOut.Recv;
        public virtual bool IsOnlyUpdate => false;

        // 注：Processor 并发上限改由 [MaxConcurrency(N)] class 级 attribute 配置，
        // 不标则使用 Server 构造时的 defaultConcurrency。
        // ProcessorRunner 在构造期解析；业务代码不再直接覆写 virtual 属性。

        public Server Server;
        public ISessionManager SessionManager => Server.SessionManager;

        /// <summary>
        /// 关联的 Runner。由 <see cref="ProcessorRunner"/> 构造期反向注入，
        /// <see cref="DeferCall"/> / <see cref="DelayCall"/> 用它把闭包 Post 到本 Runner 邮箱，
        /// 保证跨线程调用这两个 API 时队列结构的单线程访问。
        /// </summary>
        internal ProcessorRunner _runner;
        
        public abstract string[] Pushes { get; }

        public virtual string GetName()
        {
            return GetType().GetProcessorName().ToLower();
        }
        
        public virtual List<Route> GetRoutes()
        {
            if (m_routes?.Count > 0) return m_routes;
            
            m_routes = new List<Route>();
            var methods = GetType().GetMethods().Where(m => m.IsRequest() || m.IsNotify());
            foreach (var method in methods)
            {
                var route = new Route(this, method, IdGenerator.Next());
                m_routes.Add(route);
            }
            
            return m_routes;
        }

        public Route GetRoute(uint routeId)
        {
            var routes = GetRoutes();
            return routes.FirstOrDefault(o => o.RouteId == routeId);
        }
        
        public Route GetRoute(Header header)
        {
            var routes = GetRoutes();
            return routes.FirstOrDefault(o => o.RouteId == header.PackageInfo.Route);
        }
        
        public Route GetRoute(Package pack)
        {
            var routes = GetRoutes();
            return routes.FirstOrDefault(o => o.RouteId == pack.Header.PackageInfo.Route);
        }
        
        public virtual Dictionary<string, uint> GetPushDict()
        {
            if (m_pushDict?.Count > 0) return m_pushDict;
            
            m_pushDict = new Dictionary<string, uint>();
            if (Pushes == null) return m_pushDict;
            
            foreach (var pushStr in Pushes)
            {
                var id = IdGenerator.Next();
                m_pushDict[pushStr] = id;
            }

            return m_pushDict;
        }

        public virtual List<(string, uint, ServerTag)> GetRouteIdDict()
        {
            if (m_routeIdDict?.Count > 0) return m_routeIdDict;

            m_routeIdDict = new List<(string, uint, ServerTag)>();
            foreach (var route in GetRoutes())
            {
                m_routeIdDict.Add((route.RouteString, route.RouteId, route.ServerTag));
            }

            foreach (var pair in GetPushDict())
            {
                m_routeIdDict.Add((pair.Key, pair.Value, ServerTag.All));
            }

            return m_routeIdDict;
        }

        public virtual bool IsRecognizeRoute(uint routeId)
        {
            var routeIdDict = GetRouteIdDict();
            return routeIdDict.Any(o => o.Item2 == routeId);
        }

        public virtual uint ToRouteId(string route)
        {
            var routeIdDict = GetRouteIdDict();
            var id = routeIdDict.FirstOrDefault(o => o.Item1 == route);
            if (id != default) return id.Item2;

            var pushDict = GetPushDict();
            if (pushDict.ContainsKey(route)) return pushDict[route];

            return UNKOWN_ROUTE_ID;
        }
        
        public virtual async Task<Package> Invoke(Package pack)
        {
            var routes = GetRoutes();
            var route = routes.FirstOrDefault(o => o.RouteId == pack.Header.PackageInfo.Route);
            return await route!.Invoke(pack);
        }

        [Obsolete("已被 ProcessorRunner 取代。仅当自定义子类显式重写时才会被调用。新代码请通过 ProcessorRunner 与 Channel<Package> 处理消息。")]
        public virtual bool PackageLoopFrame(BlockingCollection<Package> packQueue, ConcurrentQueue<(uint, int, object)> broadcastQueue, CancellationToken cancelToken)
        {
            Server.Update(this).Wait(cancelToken);
            Server.ResolveBroadCast(this, broadcastQueue).Wait(cancelToken);
            DoDeferCalls().Wait(cancelToken);
            DoDelayCalls().Wait(cancelToken);

            //Only for update
            if (IsOnlyUpdate)
            {
                Task.Delay(UpdateDeltaTime, cancelToken).Wait(cancelToken);
                return true;
            }
                
            return ResolvePackageQueue(packQueue, cancelToken);
        }

        [Obsolete("已被 ProcessorRunner.ProcessOne 取代。新代码不应再依赖该方法。")]
        protected virtual bool ResolvePackageQueue(BlockingCollection<Package> packQueue, CancellationToken cancelToken)
        {
            if (!packQueue.TryTake(out var pack, (int)RecvTimeout.TotalMilliseconds, cancelToken)) return true;
            try
            {
                var result = OnPreRecv(pack!);
                if (result != null)
                {
                    Server.Send(result);
                    OnPostSendResult(result);
                    return true;
                }

                var task = Invoke(pack!);
                task.Wait(cancelToken);
                if (task.IsCanceled) return false;
                    
                result = task.Result;
                if (result != null)
                {
                    Server.Send(result);
                    OnPostSendResult(result);
                }
            }
            catch (OperationCanceledException)
            {
                //IGNORE ERR
            }
            catch (AggregateException err)
            {
                if (err.InnerException is OperationCanceledException) return true;
                if (err.InnerException is TaskCanceledException) return true;
                    
                Server.OnErrorEvent(pack.Header.ClientId, err);
            }
            catch (Exception err)
            {
                Server.OnErrorEvent(pack.Header.ClientId, err);
            }

            return true;
        }

        /// <summary>
        /// 把一段异步工作投递到本 Processor 的 Runner 邮箱，稍后串行执行（fire-and-forget）。
        /// 
        /// 语义变更（历史兼容）：旧实现用 <c>Queue&lt;Func&lt;Task&gt;&gt;</c>，跨线程调用会撕裂队列；
        /// 现在统一走 <see cref="ProcessorRunner.Post"/> 的 Channel，线程安全。
        /// 任务会和本 Runner 处理的消息、跨 Processor 调用交错但保持串行。
        /// </summary>
        public virtual void DeferCall(Func<Task> func)
        {
            if (func == null) return;

            var runner = _runner;
            if (runner != null)
            {
                runner.Post(func);
                return;
            }

            // Server 启动前 Runner 还没建好：此时必然在主线程单线程上下文里，
            // 退化为"启动时同步执行"，等价于历史行为中首次 tick 立即 drain。
            // 生产路径不会命中这里；仅为极端早期注册期兜底。
            try
            {
                func().GetAwaiter().GetResult();
            }
            catch (Exception err)
            {
                Server?.OnErrorEvent(IdLoopGenerator.INVALID, err);
            }
        }
        
        /// <summary>
        /// 过渡期保留：旧语义里由 <see cref="ProcessorRunner"/> 的周期性任务循环调用，
        /// 现在 <see cref="DeferCall"/> 已经把闭包塞进 Runner.control channel，周期性 drain 不再需要。
        /// 保留为 no-op，避免老代码 override 失效；子类如果覆写了本方法，也仍然会被周期调用。
        /// </summary>
        public virtual Task DoDeferCalls()
        {
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// 延迟若干时间后在本 Runner 上执行一次闭包。
        /// 
        /// 线程安全：<c>m_delayTasks</c> 本身只在 Runner 自己的线程上下文读写。
        /// 若从外部 Runner 调用，先通过 <see cref="ProcessorRunner.Post"/> 进入本 Runner 再 Add，
        /// 保持 List 操作单线程。
        /// </summary>
        public virtual void DelayCall(TimeSpan delay, Func<Task> func)
        {
            if (func == null) return;

            var time = DateTime.UtcNow.Add(delay);
            var runner = _runner;

            if (runner != null && ProcessorRunner.Current != runner)
            {
                // 跨 Runner 调用：投递闭包到本 Runner 的控制队列里，让"添加到 m_delayTasks"也串行
                runner.Post(() =>
                {
                    if (m_delayTasks == null) m_delayTasks = new();
                    m_delayTasks.Add((time, func));
                    return Task.CompletedTask;
                });
                return;
            }

            // 同一 Runner 上下文（或尚未绑定 Runner 的启动期）：直接 Add
            if (m_delayTasks == null) m_delayTasks = new();
            m_delayTasks.Add((time, func));
        }
        
        public virtual async Task DoDelayCalls()
        {
            if (m_delayTasks == null || m_delayTasks.Count <= 0) return;

            for (int i = m_delayTasks.Count-1; i >= 0; i--)
            {
                var (time, func) = m_delayTasks[i];
                if (time > DateTime.UtcNow) continue;
                
                try
                {
                    m_delayTasks.RemoveAt(i);
                    await func();
                }
                catch (OperationCanceledException)
                {
                    //IGNORE ERR
                }
                catch (AggregateException err)
                {
                    if (err.InnerException is OperationCanceledException) continue;
                    if (err.InnerException is TaskCanceledException) continue;
                    
                    Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
                catch (Exception err)
                {
                    Server.OnErrorEvent(IdLoopGenerator.INVALID, err);
                }
            }
        }
        
        public virtual void Push<T>(string route, Header header, T data)
        {
            Push(route, header.ClientId, data);
        }
        
        public virtual void Push<T>(string route, uint clientId, T data)
        {
            var pushDict = GetPushDict();
            if (!pushDict.ContainsKey(route)) throw new Exception($"Processor: push route not registered: {route}");
            
            var id = pushDict[route];
            var package = Package.Create(id, data, PackageType.Push, Server.EncodingType);
            package.Header.ClientId = clientId;
            Server.Send(package);
        }

        public void Return<T>(Header header, T data)
        {
            var h = header.Clone();
            h.PackageInfo.Type = PackageType.Response;
            h.ClientId = header.ClientId;
            var pack = new Package<T>
            {
                Header = h,
                Data = data
            };
            Server.Send(pack);
            OnPostSendResult(pack);
        }
        
        public void Send<T>(uint clientId, string route, PackageType type, T data)
        {
            var id = ToRouteId(route);
            if (id == UNKOWN_ROUTE_ID) throw new Exception($"Processor: unknown route: {route}");

            var pack = Package.Create(id, data, type, Server.EncodingType);
            pack.Header.PackageInfo.Route = id;
            pack.Header.ClientId = clientId;
            
            Server.Send(pack);
        }

        public virtual void OnClientConnected(uint clientId)
        {            
        }

        public virtual void OnClientDisconnected(uint clientId)
        {
        }
        
        public virtual void OnHandShake(Package<ReqHankShake> pack, ServerTag data)
        {
        }

        public virtual Package OnPreRecv(Package pack)
        {
            return null;
        }

        public virtual void OnPostSendResult(Package pack)
        {
        }

        public virtual bool IsRecognizeBroadcastEvent(int eventId)
        {
            return true;
        }
        
        public virtual Task OnBroadcast(uint clientId, int eventId, object data)
        {
            return Task.CompletedTask;
        }
    }
}