using System.Net;
using System.Text;
using System.Text.Json;

namespace GoPlayProj.Utils;

public static class HttpUtils
{
    /// <summary>
    /// 默认用户代理字符串
    /// </summary>
    public static string DEFAULT_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.66";
    
    /// <summary>
    /// Google用户信息地址
    /// </summary>
    public static string GoogleUserInfoUrl => "https://www.googleapis.com/userinfo/v2/me";
    
    /// <summary>
    /// 日志开关
    /// </summary>
    public static bool EnableDebugLog = false;

    /// <summary>
    /// 内部记录日志
    /// </summary>
    /// <param name="msg"></param>
    private static void DebugLog(string msg)
    {
        if (EnableDebugLog)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss zz}] {msg}");
        }
    }
    
    public static HttpClient CreateHttpClient()
    {
        return new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ClientCertificateOptions = ClientCertificateOption.Automatic,
            ServerCertificateCustomValidationCallback = (message, cert, chain, error) => true
        });
    }
    
    public static async Task<T?> GetAsync<T>(string api, Dictionary<string, string>? query = null, Dictionary<string, string>? header = null)
    {
        var json = await GetStringAsync(api, query, header);
        return JsonSerializer.Deserialize<T>(json);
    }
    
    public static async Task<string> GetStringAsync(string url, Dictionary<string, string>? query = null, Dictionary<string, string>? header = null)
    {
        using var httpClient = CreateHttpClient();
        if (query == null) query = new Dictionary<string, string>();
        query = query.RemoveEmptyValueItems();
        url = $"{url}{(url.Contains("?") ? "&" : "?")}{query.ToQueryString()}";
        DebugLog($"GET [{url}]");
        
        if (header == null) header = new Dictionary<string, string>();
        if (!header.ContainsKey("accept")) header.Add("accept", "application/json");
        if (!header.ContainsKey("User-Agent")) header.Add("User-Agent", DEFAULT_USER_AGENT);

        foreach (var headerItem in header)
        {
            httpClient.DefaultRequestHeaders.Add(headerItem.Key, headerItem.Value);
            DebugLog($"GET Header [{headerItem.Key}]=[{headerItem.Value}]");
        }

        var response = await httpClient.GetAsync(url);
        var responseText = await response.Content.ReadAsStringAsync();
        DebugLog($"GET [{url}], reponse=[{responseText}]");

        return responseText;
    }

    private static Dictionary<string, string> RemoveEmptyValueItems(this Dictionary<string, string> dict)
    {
        return dict.Where(o => !string.IsNullOrEmpty(o.Value))
            .ToDictionary(o => o.Key, o => o.Value);
    }

    private static string ToQueryString(this Dictionary<string, string> dict, bool urlEncode = true)
    {
        return string.Join("&", dict.Select(p => $"{(urlEncode ? p.Key?.UrlEncode() : "")}={(urlEncode ? p.Value?.UrlEncode() : "")}"));
    }

    private static string UrlEncode(this string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return "";
        }
        return System.Web.HttpUtility.UrlEncode(str, Encoding.UTF8);
    }
}