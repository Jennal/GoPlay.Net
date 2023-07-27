using System.Reflection;
using GoPlay.Core.Attributes;
using GoPlay.Core.Protocols;
using GoPlay.Core.Utils;
using GoPlay.Core.Processors;
using GoPlay.Exceptions;
#if DEBUG && PROFILER
using GoPlay.Core.Debug;
#endif

namespace GoPlay.Core.Routers
{
    public partial class Route
    {
        public ProcessorBase Processor;
        public MethodInfo Method;
        public PackageType PackageType;
        public string RouteString;
        public uint RouteId;
        public ServerTag ServerTag;

        public Route(ProcessorBase processor, MethodInfo method, uint routeId)
        {
            Processor = processor;
            Method = method;
            PackageType = GetPackageType();
            RouteString = GetRoute();
            RouteId = routeId;
            ServerTag = GetServerTag(processor, method);
        }

        private ServerTag GetServerTag(ProcessorBase processor, MethodInfo method)
        {
            var attr = method.GetCustomAttribute<ServerTagAttribute>();
            if (attr == null) attr = processor.GetType().GetCustomAttribute<ServerTagAttribute>();
            if (attr == null) return ServerTag.All;

            return attr.Tag;
        }

        private string GetRoute()
        {
            var className = GetAttrClassName();
            var methodName = GetAttrMethodName();

            return $"{className}.{methodName}".ToLower();
        }

        private string GetAttrClassName()
        {
            return Processor.GetType().GetProcessorName();
        }

        private string GetAttrMethodName()
        {
            switch (PackageType)
            {
                case PackageType.Notify:
                    return Method.GetNotifyName();
                case PackageType.Request:
                    return Method.GetRequestName();
            }

            throw new ArgumentOutOfRangeException();
        }

        private PackageType GetPackageType()
        {
            if (Method.IsNotify()) return PackageType.Notify;
            if (Method.IsRequest()) return PackageType.Request;

            throw new Exception("Can't reach here!");
        }

        public async Task<Package> Invoke(Package package)
        {
#if DEBUG && PROFILER
            Profiler.Begin("Prepare Param");
#endif
            var paramList = new List<object>();
            foreach (var paramInfo in Method.GetParameters())
            {
                if (paramInfo.ParameterType == typeof(Header))
                {
                    paramList.Add(package.Header);
                }
                else
                {
                    //result = Package.ParseFromRaw<paramInfo.ParameterType>(package);
                    var method = GetParseFromRawMethod(paramInfo.ParameterType);
                    var result = method.Invoke(null, new object[] {package});

                    //field = result.Data;
                    var fieldInfo = GetDataField(paramInfo.ParameterType);
                    var field = fieldInfo.GetValue(result);

                    paramList.Add(field);
                }
            }
#if DEBUG && PROFILER
            Profiler.End("Prepare Param");
#endif

#if DEBUG && PROFILER
            Profiler.Begin("Clone Header");
#endif
            var header = package.Header.Clone();
            header.PackageInfo.Type = PackageType.Response;
            header.ClientId = package.Header.ClientId;
#if DEBUG && PROFILER
            Profiler.End("Clone Header");
#endif

            try
            {
#if DEBUG && PROFILER
                Profiler.Begin("Method Invoke");
#endif
                var retData = Method.Invoke(Processor, paramList.ToArray());
#if DEBUG && PROFILER
                Profiler.End("Method Invoke");
#endif

                if (retData is Package data)
                {
                    return data;
                }

                if (retData is Status status)
                {
                    header.Status = status;
                    return new Package
                    {
                        Header = header
                    };
                }

                if (retData is Task task)
                {
#if DEBUG && PROFILER
                    Profiler.Begin("Get Data From Task");
#endif
                    retData = await Package.GetDataFromTask(task);
#if DEBUG && PROFILER
                    Profiler.End("Get Data From Task");
#endif
                }

                if (retData == null)
                {
                    return null;
                }
                
                //retPack.Header = package.Header.Clone();
                //retPack.Data = retData;
#if DEBUG && PROFILER
                Profiler.Begin("Set Result");
#endif
                var retType = GetReturnType(retData.GetType());
                var headerField = retType.GetField("Header");
                var dataField = retType.GetField("Data");

                var retPack = Activator.CreateInstance(retType);
                headerField.SetValue(retPack, header);
                dataField.SetValue(retPack, retData);

                var resultPack = (Package) retPack;
                if (resultPack != null && resultPack.Header.Status == null)
                {
                    resultPack.Header.Status = new Status
                    {
                        Code = StatusCode.Success
                    };
                }
#if DEBUG && PROFILER
                Profiler.End("Set Result");
#endif

                return resultPack;
            }
            catch (Exception err)
            {
                while (err is ProcessorMethodException == false && err.InnerException != null) err = err.InnerException;
                if (err is ProcessorMethodException pme)
                {
                    header.Status = new Status
                    {
                        Code = pme.Code,
                        Message = pme.Msg,
                    };
                    return new Package
                    {
                        Header = header
                    };
                }
                
                header.Status = new Status
                {
                    Code = StatusCode.Error,
                    Message = $"Internal Error: \n{err.Message}\n\n{err.StackTrace}", //TODO: 上线前去掉
                };
                return new Package
                {
                    Header = header
                };
            }
        }
    }
}