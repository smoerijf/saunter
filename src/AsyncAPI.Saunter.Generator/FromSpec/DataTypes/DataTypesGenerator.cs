using NJsonSchema;
using NSwag.CodeGeneration.CSharp;
using NSwag;
using NJsonSchema.CodeGeneration.CSharp;


namespace AsyncAPI.Saunter.Generator.FromSpec.DataTypes;

internal interface IDataTypesGenerator
{
    Task<string> GenerateDataTypesAsync(GeneratorOptions options, string spec, DataTypesGeneratorState state);
}

internal class NSwagGenerator : IDataTypesGenerator
{
    public async Task<string> GenerateDataTypesAsync(GeneratorOptions options, string spec, DataTypesGeneratorState state)
    {
        spec = OpenApiCompatibility.PrepareSpecFile(spec);

        var document = await OpenApiDocument.FromJsonAsync(spec).ConfigureAwait(false);
        var settings = new CSharpClientGeneratorSettings
        {
            CSharpGeneratorSettings =
            {
                Namespace = options.Namespace,
                SchemaType = SchemaType.OpenApi3,
                ClassStyle = CSharpClassStyle.Record,
                ExcludedTypeNames = state.AlreadyGeneratedDataTypes.ToArray(),
            },
            GenerateClientClasses = false,
            AdditionalNamespaceUsages = [],
        };

        var generator = new CSharpClientGenerator(document, settings);
        var contents = generator.GenerateFile();
        state.AlreadyGeneratedDataTypes.AddRange(document.Definitions.Keys);
        state.AlreadyGeneratedNamespaces.Add(options.Namespace);

        return contents;
    }
}
