// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Saunter.Attributes;
using System.Collections.Immutable;
using Xunit.Abstractions;

namespace Saunter.Generators.Tests;

public class SourceGeneratorTests(ITestOutputHelper output)
{
    public string Run<T>(string source, IEnumerable<string> additionalTextPaths = null) where T : IIncrementalGenerator, new()
    {
        additionalTextPaths ??= ["Templates"];

        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new List<MetadataReference>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Concat([typeof(AsyncApiAttribute).Assembly]);
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        var additionalTexts = new List<AdditionalText>();
        foreach (var files in additionalTextPaths.Select(Directory.EnumerateFiles))
        {
            additionalTexts.AddRange(files.Select(file => new CustomAdditionalText(file)));
        }

        var compilation = CSharpCompilation.Create("foo", [syntaxTree], references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // TODO: Uncomment this line if you want to fail tests when the injected program isn't valid _before_ running generators
        //var compileDiagnostics = compilation.GetDiagnostics();
        //Assert.False(compileDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), "Failed: " + compileDiagnostics.FirstOrDefault()?.GetMessage());

        IIncrementalGenerator generator = new T();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.AddAdditionalTexts(ImmutableArray.CreateRange(additionalTexts));
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);
        Assert.False(generateDiagnostics.Any(), "Failed: " + generateDiagnostics.FirstOrDefault()?.GetMessage());

        var result = outputCompilation.SyntaxTrees.Last().ToString();
        output.WriteLine(result);
        return result.Trim();
    }
}
