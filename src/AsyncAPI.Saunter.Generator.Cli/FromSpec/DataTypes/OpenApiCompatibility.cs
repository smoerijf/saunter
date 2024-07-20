using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace AsyncAPI.Saunter.Generator.Cli.FromSpec.DataTypes;

internal static class OpenApiCompatibility
{
    internal static string PrepareSpecFile(string spec)
    {
        var json = (JObject)JsonConvert.DeserializeObject(spec);
        // the type is important for NSwag
        if (!json.ContainsKey("openapi"))
        {
            json.Add("openapi", "3.0.1");
        }
        // NSwag doesn't understand the servers format of AsyncApi, and it is not needed anyway.
        if (json.ContainsKey("servers"))
        {
            json.Remove("servers");
        }
        return JsonConvert.SerializeObject(json);
    }
}
