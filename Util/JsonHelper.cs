using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SHAW.Util;

public static class JsonHelper
{
    public static string? GetJsonSecret(string key)
    {
        using(StreamReader reader = new StreamReader("./connection.json"))
        {
            string json = reader.ReadToEnd();
            JObject? obj = JsonConvert.DeserializeObject<JObject>(json);
            return obj?.GetValue(key)?.ToString();
        }
    }
}