using System.Diagnostics;
using System.Text;
using AsyncAPI.Saunter.Generator.Cli.FromSpec.AsyncApiInterface;
using AsyncAPI.Saunter.Generator.Cli.FromSpec.DataTypes;
using CaseConverter;
using ConsoleAppFramework;
using Microsoft.Extensions.Logging;

namespace AsyncAPI.Saunter.Generator.Cli.FromSpec;

internal class FromSpecCommand(ILogger<FromSpecCommand> logger, IAsyncApiGenerator asyncApiGenerator, IDataTypesGenerator dataTypesGenerator)
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
        var output = specsToGenerate.ToDictionary(x => x, _ => new StringBuilder());

        // Common
        var topicsClassName = "Topics";
        foreach (var (spec, _) in output)
        {
            Directory.CreateDirectory(spec.OutputDirectory);

            if (!File.Exists(spec.SpecFile))
            {
                throw new ArgumentException($"Provided spec does not exist: {Path.GetFullPath(spec.SpecFile)}.");
            }
        }

        // AsyncAPI Interface
        var aaState = new AsyncApiState();
        foreach (var (spec, sb) in output)
        {
            var options = new GeneratorOptions($"{spec.NamespaceName}.{spec.SpecName}.Api", spec.SpecName, $"{topicsClassName}");
            var contents = asyncApiGenerator.GenerateAsyncApiInterfaces(options, spec.Contents, aaState);
            sb.Append(contents);
        }

        // DataTypes
        var nsState = new DataTypesGeneratorState();
        foreach (var (spec, sb) in output)
        {
            var options = new GeneratorOptions($"{spec.NamespaceName}.{spec.SpecName}.Api", spec.SpecName, $"{topicsClassName}");
            var contents = await dataTypesGenerator.GenerateDataTypesAsync(options, spec.Contents, nsState).ConfigureAwait(false);
            sb.Append(contents);
        }

        // Write to file
        foreach (var (spec, sb) in output)
        {
            var contents = sb.ToString();
            var outputFile = spec.OutputFileName;
            await File.WriteAllTextAsync(outputFile, contents, Encoding.UTF8);
            logger.LogInformation($"Created {outputFile} (size: {contents.Length:N0} chars)");
        }

        return 0;
    }

    private record SpecToGenerate(string NamespaceName, string OutputDirectory, string SpecFile)
    {
        private string _contents;

        public string Contents => this._contents ??= File.ReadAllText(this.SpecFile);

        public string SpecName => Path.GetFileNameWithoutExtension(this.SpecFile).ToPascalCase();

        public string OutputFileName => Path.GetFullPath(Path.Combine(this.OutputDirectory, $"{this.SpecName}.g.cs"));
    }

    private static IEnumerable<SpecToGenerate> Split(IEnumerable<string> input)
    {
        foreach (var spec in input)
        {
            var split = spec.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
            if (split.Count == 3 && !split.Any(string.IsNullOrWhiteSpace))
            {
                yield return new SpecToGenerate(NamespaceName: split[0], OutputDirectory: split[1], SpecFile: split[2]);
            }
        }
    }
}
