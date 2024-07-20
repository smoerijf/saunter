using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.RepresentationModel;
using Yaml2JsonNode;

namespace AsyncAPI.Saunter.Generator.FromSpec.DataTypes;

internal static class OpenApiCompatibility
{
    internal static string PrepareSpecFile(string spec)
    {
        var reader = new StringReader(spec);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);

        if (yamlStream.Documents[0].ToJsonNode() is JsonObject json)
        {
            // the type is important for NSwag
            if (!json.ContainsKey("openapi"))
            {
                json.Add("openapi", "3.0.1");
            }

            //// NSwag doesn't understand the servers format of AsyncApi, not needed.
            if (json.ContainsKey("servers"))
            {
                json.Remove("servers");
            }
            return JsonSerializer.Serialize(json);
        }

        return spec;
    }
}
