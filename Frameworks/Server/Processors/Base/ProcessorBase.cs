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
        protected Queue<Func<Task>> m_deferTasks;
        protected List<(DateTime, Func<Task>)> m_delayTasks;
        
        internal DateTime LastUpdate = DateTime.UtcNow;
        public virtual TimeSpan UpdateDeltaTime => Consts.TimeOut.Update;
        public virtual TimeSpan RecvTimeout => Consts.TimeOut.Recv;
        public virtual bool IsOnlyUpdate => false;
        
        public Server Server;
        public ISessionManager SessionManager => Server.SessionManager;
        
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

        public virtual void DeferCall(Func<Task> func)
        {
            if (m_deferTasks == null) m_deferTasks = new Queue<Func<Task>>();
            m_deferTasks.Enqueue(func);
        }
        
        public virtual async Task DoDeferCalls()
        {
            if (m_deferTasks == null || m_deferTasks.Count <= 0) return;

            while (m_deferTasks.Count > 0)
            {
                try
                {
                    var func = m_deferTasks.Dequeue();
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
        
        public virtual void DelayCall(TimeSpan delay, Func<Task> func)
        {
            if (m_delayTasks == null) m_delayTasks = new ();

            var time = DateTime.UtcNow.Add(delay);
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
                    await func();
                    m_delayTasks.RemoveAt(i);
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
        
        public virtual void OnHandShake(Header header, ServerTag data)
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