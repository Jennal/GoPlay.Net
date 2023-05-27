using Newtonsoft.Json;

namespace GoPlay.Services.Core.Debug
{
    public static class DebugExtension
    {
        public static string Dump(this byte[] bytes)
        {
            if (bytes == null) return "null";
            if (bytes.Length == 0) return "[]";
            
            return $"({bytes.Length})[{string.Join(" ", bytes)}]";
        }

        public static string Dump(this object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}