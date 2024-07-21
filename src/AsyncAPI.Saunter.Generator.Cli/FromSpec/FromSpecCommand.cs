using System.Text;
using AsyncAPI.Saunter.Generator.FromSpec;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;

namespace AsyncAPI.Saunter.Generator.Cli.FromSpec;

internal class FromSpecCommand(ILogger<FromSpecCommand> logger, IAsyncApiCodeGenerator codeGenerator)
{
    /// <summary>
    /// Retrieves AsyncAPI spec from a startup assembly and writes to file.
    /// </summary>
    /// <param name="specs">the AsyncAPI specification to generate code for. Parameter should include 3 parts: namespace,outputDirectory,asyncapiSpec</param>
    [Command("fromspec")]
    public async Task<int> FromSpec(params string[] specs)
    {
        logger.LogInformation($"FromSpec(#{specs.Length}): --specs {string.Join(';', specs)}");

        var specsToGenerate = Split(specs);
        var output = await codeGenerator.FromSpecs(specsToGenerate).ConfigureAwait(false);

        // Write to file
        foreach (var (spec, contents) in output)
        {
            Directory.CreateDirectory(spec.OutputDirectory);

            var outputFile = spec.OutputFileName;
            await File.WriteAllTextAsync(outputFile, contents, Encoding.UTF8).ConfigureAwait(false);
            logger.LogInformation($"Created {outputFile} (size: {contents.Length:N0} chars)");
        }

        return 0;
    }

    private record SpecToGenerate(string NamespaceName, string OutputDirectory, string SpecFilePath) : Generator.FromSpec.SpecToGenerate(NamespaceName, SpecFilePath)
    {
        public string OutputFileName => Path.GetFullPath(Path.Combine(this.OutputDirectory, $"{this.SpecName}.g.cs"));
    }

    private static IEnumerable<SpecToGenerate> Split(IEnumerable<string> input)
    {
        foreach (var spec in input)
        {
            var split = spec.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            if (split.Count == 3 && !split.Any(string.IsNullOrWhiteSpace))
            {
                yield return new SpecToGenerate(NamespaceName: split[0], OutputDirectory: split[1], SpecFilePath: split[2]);
            }
        }
    }
}
