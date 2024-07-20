using System.Text;
using AsyncAPI.Saunter.Generator.FromSpec.AsyncApiInterface;
using AsyncAPI.Saunter.Generator.FromSpec.DataTypes;
using Microsoft.Extensions.Logging;

namespace AsyncAPI.Saunter.Generator.FromSpec;

public interface IAsyncApiCodeGenerator
{
    Task<List<(TSpecToGenerate spec, string contents)>> FromSpecs<TSpecToGenerate>(IEnumerable<TSpecToGenerate> specsToGenerate) where TSpecToGenerate : SpecToGenerate;
}

internal class CodeGenerator(ILogger<CodeGenerator> logger, IAsyncApiGenerator asyncApiGenerator, IDataTypesGenerator dataTypesGenerator) : IAsyncApiCodeGenerator
{
    public async Task<List<(TSpecToGenerate spec, string contents)>> FromSpecs<TSpecToGenerate>(IEnumerable<TSpecToGenerate> specsToGenerate) where TSpecToGenerate : SpecToGenerate
    {
        var output = specsToGenerate.Select(x => (x, new StringBuilder())).ToList();

        // Common
        var topicsClassName = "Topics";
        foreach (var (spec, _) in output)
        {
            if (!File.Exists(spec.SpecFilePath))
            {
                throw new ArgumentException($"Provided spec does not exist: {Path.GetFullPath(spec.SpecFilePath)}.");
            }
        }

        // AsyncAPI Interface
        var aaState = new AsyncApiState();
        foreach (var (spec, sb) in output)
        {
            var options = new GeneratorOptions($"{spec.NamespaceName}.{spec.SpecName}.Api".TrimStart('.'), spec.SpecName, $"{topicsClassName}");
            var contents = asyncApiGenerator.GenerateAsyncApiInterfaces(options, spec.FileContents, aaState);
            sb.Append(contents);
        }

        // DataTypes
        var nsState = new DataTypesGeneratorState();
        foreach (var (spec, sb) in output)
        {
            var options = new GeneratorOptions($"{spec.NamespaceName}.{spec.SpecName}.Api".TrimStart('.'), spec.SpecName, $"{topicsClassName}");
            var contents = await dataTypesGenerator.GenerateDataTypesAsync(options, spec.FileContents, nsState).ConfigureAwait(false);
            sb.Append(contents);
        }

        return output.Select(x => (x.Item1, x.Item2.ToString())).ToList();
    }
}
