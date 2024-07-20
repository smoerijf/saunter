using ConsoleAppFramework;
using LEGO.AsyncAPI;
using LEGO.AsyncAPI.Models;
using Microsoft.Extensions.Logging;

namespace AsyncAPI.Saunter.Generator.Cli.FromSpec;

internal class FromSpecCommand(ILogger<FromSpecCommand> logger)
{
    /// <summary>
    /// Retrieves AsyncAPI spec from a startup assembly and writes to file.
    /// </summary>
    /// <param name="specs">the AsyncAPI specification to generate code for. Parameter should include the namespace: namespace,outputPath,asyncapi.json</param>
    [Command("fromspec")]
    public int FromSpec(params string[] specs)
    {
        logger.LogInformation($"FromSpec(#{specs.Length}): --specs {string.Join(';', specs)}");

        foreach (var (namespaceName, output, specName) in Split(specs))
        {
            Directory.CreateDirectory(output);
            var outputFile = Path.Combine(output, $"{Path.GetFileNameWithoutExtension(specName)}.g.cs");
            File.Create(outputFile);
            logger.LogInformation($"Created {Path.GetFullPath(outputFile)}");
        }
        return 0;
    }

    private static IEnumerable<(string namespaceName, string output, string specName)> Split(IEnumerable<string> input)
    {
        foreach (var spec in input)
        {
            var split = spec.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            if (split.Count == 3 && !split.Any(string.IsNullOrWhiteSpace))
            {
                yield return (split[0], split[1], split[2]);
            }
        }
    }
}
