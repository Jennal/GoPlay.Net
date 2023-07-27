using System;
using System.Linq;
using System.Reflection;
using GoPlay.Core.Attributes;
using GoPlay.Core.Protocols;

namespace GoPlay.Core.Utils
{
    public static class ReflectUtil
    {   
        public static bool HasAttribute<TA>(this Type type, bool inherit)
            where TA : Attribute
        {
            return type.GetCustomAttributes(typeof(TA), inherit).Any();
        }
        
        public static bool HasAttribute<TA>(this MethodInfo method, bool inherit)
            where TA : Attribute
        {
            return method.GetCustomAttributes(typeof(TA), inherit).Any();
        }

        public static bool IsValidProcessorMethod(this MethodInfo method)
        {
            var param = method.GetParameters();

            if (param.Length == 0 || param.Length == 1) return true;
            if (param.Length == 2 && param.Any(o => o.ParameterType == typeof(Header))) return true;

            return false;
        }
        
        public static bool IsNotify(this MethodInfo method)
        {
            return method.IsValidProcessorMethod() && method.HasAttribute<NotifyAttribute>(true);
        }
        
        public static bool IsRequest(this MethodInfo method)
        {
            return method.IsValidProcessorMethod() && method.HasAttribute<RequestAttribute>(true);
        }

        public static string GetProcessorName(this Type type)
        {
            var attr = type.GetCustomAttribute<ProcessorAttribute>();
            if (!string.IsNullOrEmpty(attr?.Name))
            {
                return attr.Name;
            }
            return type.Name;
        }
        
        public static string GetNotifyName(this MethodInfo method)
        {
            var attr = method.GetCustomAttribute<NotifyAttribute>();
            if (!string.IsNullOrEmpty(attr?.Name))
            {
                return attr.Name;
            }
            return method.Name;
        }
        
        public static string GetRequestName(this MethodInfo method)
        {
            var attr = method.GetCustomAttribute<RequestAttribute>();
            if (!string.IsNullOrEmpty(attr?.Name))
            {
                return attr.Name;
            }
            return method.Name;
        }
    }
}