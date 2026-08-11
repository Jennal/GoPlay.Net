using System.Collections.Concurrent;
using System.Reflection;
using GoPlay.Core.Protocols;

namespace GoPlay.Core.Routers
{
    public partial class Route
    {
        private static ConcurrentDictionary<Type, MethodInfo> s_dictParseFromRawMethods = new ConcurrentDictionary<Type, MethodInfo>();
        private static ConcurrentDictionary<Type, FieldInfo> s_dictDataFields = new ConcurrentDictionary<Type, FieldInfo>();
        private static ConcurrentDictionary<Type, Type> s_dictReturnTypes = new ConcurrentDictionary<Type, Type>();

        public static MethodInfo GetParseFromRawMethod(Type type)
        {
            if (s_dictParseFromRawMethods.TryGetValue(type, out MethodInfo rawMethod)) return rawMethod;

            var method = typeof(Package).GetMethod("ParseFromRaw", BindingFlags.Static | BindingFlags.Public);
            method = method!.MakeGenericMethod(type);
            s_dictParseFromRawMethods[type] = method;

            return method;
        }

        public static FieldInfo GetDataField(Type type)
        {
            if (s_dictDataFields.TryGetValue(type, out FieldInfo field)) return field;

            var fieldType = GetReturnType(type);
            var fieldInfo = fieldType.GetField("Data");
            s_dictDataFields[type] = fieldInfo!;

            return fieldInfo!;
        }

        public static Type GetReturnType(Type type)
        {
            if (s_dictReturnTypes.TryGetValue(type, out Type returnType)) return returnType;

            var fieldType = typeof(Package<>).MakeGenericType(type);
            s_dictReturnTypes[type] = fieldType!;

            return fieldType!;
        }
    }
}
