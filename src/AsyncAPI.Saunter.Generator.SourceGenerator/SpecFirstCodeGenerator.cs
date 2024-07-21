using System.Collections.Immutable;
using System.Reflection;
using AsyncAPI.Saunter.Generator.FromSpec;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncAPI.Saunter.Generator.SourceGenerator;

[Generator]
public class SpecFirstCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var asyncApiSpecInputs = context.AdditionalTextsProvider.Where(x => x.Path.EndsWith(".json") || x.Path.EndsWith(".yml") || x.Path.EndsWith(".yaml")).Collect();

        context.RegisterSourceOutput(context.AnalyzerConfigOptionsProvider.Combine(asyncApiSpecInputs), GenerateCode);
    }

    private static async void GenerateCode(SourceProductionContext context, (AnalyzerConfigOptionsProvider options, ImmutableArray<AdditionalText> specs) args)
    {
        var services = GeneratorServiceCollection.Create();
        services.AddFromSpecCodeGenerator();
        using var provider = services.BuildServiceProvider();
        var codeGen = provider.GetRequiredService<IAsyncApiCodeGenerator>();

        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        DependencyResolver.Init(basePath);
        var output = await codeGen.FromSpecs(args.specs.Select(specFile =>
        {
            var fileOptions = args.options.GetOptions(specFile);
            if (!fileOptions.TryGetValue("build_metadata.AdditionalFiles.Namespace", out var namespaceName))
            {
                context.ReportDiagnostic(Diagnostic.Create(MissingNamespace, null, Path.GetFileName(specFile.Path)));
            }

            return new SpecToGenerate(namespaceName, specFile.Path);
        }));

        foreach (var (spec, contents) in output)
        {
            context.AddSource($"{spec.SpecFileName}.g.cs", contents);
        }
    }

    public static readonly DiagnosticDescriptor MissingNamespace = new(
        "AA0001",
        "No namespace provided for AsyncAPI code generator",
        "Missing 'Namespace' for {0}",
        "SourceGenerator",
        DiagnosticSeverity.Error,
        true);
}
