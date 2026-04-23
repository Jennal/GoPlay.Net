using System.Linq.Expressions;
using System.Reflection;
using GoPlay.Core.Processors;
using GoPlay.Core.Protocols;
using GoPlay.Exceptions;

namespace GoPlay.Core.Routers
{
    /// <summary>
    /// 启动期把业务方法编译成强类型委托，避免请求时走反射。
    ///
    /// 委托签名：
    ///   ValueTask&lt;Package&gt; Invoker(ProcessorBase processor, Package pack, Header responseHeader)
    ///
    /// responseHeader 是调用前已经 Clone 好、Type=Response 的头，编译器内部直接用它构造返回包，
    /// 减少一次 try/catch 外的 header 处理分支。
    /// </summary>
    internal static class RouteCompiler
    {
        public delegate ValueTask<Package> CompiledInvoker(ProcessorBase processor, Package pack, Header responseHeader);

        public static CompiledInvoker TryCompile(ProcessorBase processor, MethodInfo method)
        {
            try
            {
                return Compile(processor, method);
            }
            catch
            {
                // 编译失败时返回 null，由 Route.Invoke 走反射 fallback
                return null;
            }
        }

        private static CompiledInvoker Compile(ProcessorBase processor, MethodInfo method)
        {
            var processorParam = Expression.Parameter(typeof(ProcessorBase), "processor");
            var packParam = Expression.Parameter(typeof(Package), "pack");
            var headerParam = Expression.Parameter(typeof(Header), "header");

            // 1. 准备业务方法的参数表
            var methodParams = method.GetParameters();
            var callArgs = new Expression[methodParams.Length];
            for (var i = 0; i < methodParams.Length; i++)
            {
                var pInfo = methodParams[i];
                if (pInfo.ParameterType == typeof(Header))
                {
                    // 业务方法参数是 Header，直接用 pack.Header
                    callArgs[i] = Expression.Field(packParam, nameof(Package.Header));
                }
                else
                {
                    // 业务方法参数是 Pb 类型，调 Package.ParseFromRaw<T>(pack).Data
                    var parseMethod = typeof(Package)
                        .GetMethod(nameof(Package.ParseFromRaw), BindingFlags.Static | BindingFlags.Public)!
                        .MakeGenericMethod(pInfo.ParameterType);

                    var parseCall = Expression.Call(parseMethod, packParam); // Package<T>
                    var dataField = typeof(Package<>).MakeGenericType(pInfo.ParameterType).GetField(nameof(Package<object>.Data));
                    callArgs[i] = Expression.Field(parseCall, dataField!);
                }
            }

            // 2. 调业务方法
            Expression instanceExpr = method.IsStatic
                ? null
                : Expression.Convert(processorParam, method.DeclaringType!);
            Expression invokeExpr = Expression.Call(instanceExpr, method, callArgs);

            // 3. 把返回值统一转成 ValueTask<Package>
            var returnType = method.ReturnType;
            Expression bodyExpr;

            if (returnType == typeof(void))
            {
                // void: 调用后返回 null
                bodyExpr = Expression.Block(
                    invokeExpr,
                    Expression.Constant(new ValueTask<Package>((Package)null))
                );
            }
            else if (returnType == typeof(Task))
            {
                // 非泛型 Task: await 后返回 null
                var awaitNullMethod = typeof(RouteCompilerHelpers)
                    .GetMethod(nameof(RouteCompilerHelpers.AwaitTaskAsNullPackage), BindingFlags.Static | BindingFlags.NonPublic)!;
                bodyExpr = Expression.Call(awaitNullMethod, invokeExpr);
            }
            else if (returnType == typeof(Package))
            {
                // 直接返回 Package
                bodyExpr = Expression.New(
                    typeof(ValueTask<Package>).GetConstructor(new[] { typeof(Package) })!,
                    invokeExpr);
            }
            else if (typeof(Package).IsAssignableFrom(returnType))
            {
                // 返回 Package<T> 的具体子类
                var convertExpr = Expression.Convert(invokeExpr, typeof(Package));
                bodyExpr = Expression.New(
                    typeof(ValueTask<Package>).GetConstructor(new[] { typeof(Package) })!,
                    convertExpr);
            }
            else if (returnType == typeof(Status))
            {
                // 返回 Status: 包装成只有 Header 的 Package，并把 Status 写入 header
                var helperMethod = typeof(RouteCompilerHelpers)
                    .GetMethod(nameof(RouteCompilerHelpers.WrapStatus), BindingFlags.Static | BindingFlags.NonPublic)!;
                bodyExpr = Expression.Call(helperMethod, invokeExpr, headerParam);
            }
            else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // Task<T>: 用 helper 解包并构造响应
                var inner = returnType.GetGenericArguments()[0];
                var helperMethod = typeof(RouteCompilerHelpers)
                    .GetMethod(nameof(RouteCompilerHelpers.AwaitTaskAndWrap), BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(inner);
                bodyExpr = Expression.Call(helperMethod, invokeExpr, headerParam);
            }
            else if (returnType == typeof(object))
            {
                // 业务返回 object（例子里有 [Request("err")] 返回 object 时是 Status 或 Pb）
                var helperMethod = typeof(RouteCompilerHelpers)
                    .GetMethod(nameof(RouteCompilerHelpers.WrapObject), BindingFlags.Static | BindingFlags.NonPublic)!;
                bodyExpr = Expression.Call(helperMethod, invokeExpr, headerParam);
            }
            else
            {
                // 同步返回具体 Pb 类型
                var helperMethod = typeof(RouteCompilerHelpers)
                    .GetMethod(nameof(RouteCompilerHelpers.WrapData), BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(returnType);
                bodyExpr = Expression.Call(helperMethod, Expression.Convert(invokeExpr, returnType), headerParam);
            }

            var lambda = Expression.Lambda<CompiledInvoker>(bodyExpr, processorParam, packParam, headerParam);
            return lambda.Compile();
        }
    }

    internal static class RouteCompilerHelpers
    {
        internal static ValueTask<Package> WrapData<T>(T data, Header header)
        {
            if (data == null) return new ValueTask<Package>((Package)null);

            var pack = new Package<T>
            {
                Header = header,
                Data = data,
            };
            EnsureSuccessStatus(pack);
            return new ValueTask<Package>(pack);
        }

        internal static ValueTask<Package> WrapStatus(Status status, Header header)
        {
            header.Status = status ?? new Status { Code = StatusCode.Success };
            return new ValueTask<Package>(new Package { Header = header });
        }

        internal static ValueTask<Package> WrapObject(object data, Header header)
        {
            if (data == null) return new ValueTask<Package>((Package)null);

            switch (data)
            {
                case Package p:
                    return new ValueTask<Package>(p);
                case Status s:
                    return WrapStatus(s, header);
                case Task t:
                    return AwaitObjectTask(t, header);
            }

            return new ValueTask<Package>(WrapDynamic(data, header));
        }

        internal static async ValueTask<Package> AwaitTaskAsNullPackage(Task task)
        {
            await task.ConfigureAwait(false);
            return null;
        }

        internal static async ValueTask<Package> AwaitTaskAndWrap<T>(Task<T> task, Header header)
        {
            var data = await task.ConfigureAwait(false);
            if (data == null) return null;

            // 处理 Task<Package> / Task<Package<T>> / Task<Status> / Task<object>
            if (data is Package p) return p;
            if (data is Status s)
            {
                header.Status = s;
                return new Package { Header = header };
            }

            return WrapDynamic(data, header);
        }

        private static async ValueTask<Package> AwaitObjectTask(Task task, Header header)
        {
            await task.ConfigureAwait(false);
            var taskType = task.GetType();
            if (!taskType.IsGenericType) return null;

            var resultProp = taskType.GetProperty("Result");
            if (resultProp == null) return null;

            var data = resultProp.GetValue(task);
            if (data == null) return null;

            if (data is Package p) return p;
            if (data is Status s)
            {
                header.Status = s;
                return new Package { Header = header };
            }

            return WrapDynamic(data, header);
        }

        private static Package WrapDynamic(object data, Header header)
        {
            var dataType = data.GetType();
            var packType = typeof(Package<>).MakeGenericType(dataType);
            var pack = (Package)Activator.CreateInstance(packType)!;
            pack.Header = header;
            packType.GetField("Data")!.SetValue(pack, data);
            EnsureSuccessStatus(pack);
            return pack;
        }

        private static void EnsureSuccessStatus(Package pack)
        {
            if (pack.Header.Status == null)
            {
                pack.Header.Status = new Status { Code = StatusCode.Success };
            }
        }
    }
}
