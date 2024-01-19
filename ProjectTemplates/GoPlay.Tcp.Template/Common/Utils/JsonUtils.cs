using Newtonsoft.Json;

namespace GoPlayProj.Utils;

public static class JsonUtils
{
    public static string Tojson<T>(T obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
    }

    public static T? FromJson<T>(string json)
    {
        return JsonConvert.DeserializeObject<T>(json);
    }
}