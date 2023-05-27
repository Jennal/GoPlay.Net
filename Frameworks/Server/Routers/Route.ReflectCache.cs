using System.Reflection;
using GoPlay.Services.Core.Protocols;

namespace GoPlay.Services.Core.Routers
{
    public partial class Route
    {
        private static Dictionary<Type, MethodInfo> s_dictParseFromRawMethods = new Dictionary<Type, MethodInfo>();
        private static Dictionary<Type, FieldInfo> s_dictDataFields = new Dictionary<Type, FieldInfo>();
        private static Dictionary<Type, Type> s_dictReturnTypes = new Dictionary<Type, Type>();

        public static MethodInfo GetParseFromRawMethod(Type type)
        {
            if (s_dictParseFromRawMethods.ContainsKey(type)) return s_dictParseFromRawMethods[type];
            
            var method = typeof(Package).GetMethod("ParseFromRaw", BindingFlags.Static | BindingFlags.Public);
            method = method!.MakeGenericMethod(type);
            s_dictParseFromRawMethods[type] = method;
            
            return method;
        }

        public static FieldInfo GetDataField(Type type)
        {
            if (s_dictDataFields.ContainsKey(type)) return s_dictDataFields[type];
            
            var fieldType = GetReturnType(type);
            var fieldInfo = fieldType.GetField("Data");
            s_dictDataFields[type] = fieldInfo!;
            
            return fieldInfo!;
        }
        
        public static Type GetReturnType(Type type)
        {
            if (s_dictReturnTypes.ContainsKey(type)) return s_dictReturnTypes[type];
            
            var fieldType = typeof(Package<>).MakeGenericType(type);
            s_dictReturnTypes[type] = fieldType!;
            
            return fieldType!;
        }
    }
}