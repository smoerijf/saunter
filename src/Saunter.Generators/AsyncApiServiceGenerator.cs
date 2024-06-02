// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Saunter.Generators.Model;
using Saunter.Generators.Helpers;
using Scriban;
using InvalidOperationException = System.InvalidOperationException;

namespace Saunter.Generators;

[Generator]
public class AsyncApiServiceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sw = Stopwatch.StartNew();
        var classes = context.SyntaxProvider.CreateSyntaxProvider(static (node, _) => IsSyntaxTarget(node), static (ctx, _) => GetSemanticTarget(ctx)).Where(static target => target is not null);

        var templates = context.AdditionalTextsProvider.Where(IsTemplateFile).Select(TemplateFile).Collect();
        context.RegisterSourceOutput(classes.Combine(templates), Execute);

        context.RegisterPostInitializationOutput(PostInitializationOutput);
        sw.Stop();
        Console.WriteLine($"AsyncApiServiceGenerator took {sw.Elapsed}s");
    }

    private static bool IsTemplateFile(AdditionalText file) => Path.GetExtension(file.Path).Equals(".txt", StringComparison.OrdinalIgnoreCase);

    private static AdditionalTemplate TemplateFile(AdditionalText file, CancellationToken token) => new(file.Path, file.GetText(token)!.ToString());

    private static bool IsSyntaxTarget(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.AttributeLists.Count > 0;// && classDeclarationSyntax.BaseList?.Types.Count > 0;
    }

    private static ClassToGenerate GetSemanticTarget(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        var wellKnownAttributes = WellKnownAttributes.GetWellKnownAttributes(context.SemanticModel.Compilation);

        if (classSymbol is not null && wellKnownAttributes.IsValid())
        {
            foreach (var generatorAttributeData in classSymbol.GetAttributes(wellKnownAttributes.GeneratorAttribute))
            {
                var className = classSymbol.Name;
                var fileName = classDeclarationSyntax.GetLocation().SourceTree.FilePath;
                var methodsToGenerate = new List<MethodsToGenerate>();
                var interfaces = new List<string>();

                // Check if the generator attributes defines an AsyncApi interface?
                if (generatorAttributeData.ConstructorArguments.Length >= 2)
                {
                    foreach (var asyncApiInterfaceName in generatorAttributeData.ConstructorArguments[1].Values.Select(x => x.Value?.ToString()).Where(x => x != null))
                    {
                        var asyncApiInterface = context.SemanticModel.Compilation.GetTypeByMetadataName(asyncApiInterfaceName);
                        if (asyncApiInterface != null)
                        {
                            if (AddInterfaceMethods(asyncApiInterface, wellKnownAttributes, fileName, out var methods))
                            {
                                interfaces.Add(asyncApiInterface.ToString());
                                methodsToGenerate.AddRange(methods);
                            }
                        }
                    }
                }

                // Check if the class implements an interface that defines an AsyncApi interface?
                if (classDeclarationSyntax.BaseList != null)
                {
                    foreach (var interfaceSyntax in classDeclarationSyntax.BaseList.Types.OfType<SimpleBaseTypeSyntax>())
                    {
                        if (interfaceSyntax.Type is IdentifierNameSyntax identifierNameSyntax)
                        {
                            var asyncApiInterface = context.SemanticModel.GetTypeInfo(identifierNameSyntax).Type;
                            if (AddInterfaceMethods(asyncApiInterface, wellKnownAttributes, fileName, out var methods))
                            {
                                interfaces.Add(asyncApiInterface.ToString());
                                methodsToGenerate.AddRange(methods);
                            }
                        }
                    }
                }

                if (interfaces.Count == 0)
                {
                    throw new InvalidOperationException($"Class '{className}' (file: {fileName}) was marked with [AsyncApiServiceAttribute] but does not implement any interface marked with [AsyncApiAttribute].");
                }

                if (methodsToGenerate.Count > 0)
                {
                    var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                    var template = generatorAttributeData.ConstructorArguments[0].Value?.ToString() ?? throw new InvalidOperationException($"Template parameter was missing for class '{className}' in {fileName}.");

                    // Prepend the path of the generating class if not empty to allow paths relative to the generating class' location.
                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        template = Path.Combine(Path.GetDirectoryName(fileName), template);
                    }

                    return new ClassToGenerate(template, namespaceName, className, interfaces, methodsToGenerate);
                }
            }
        }

        return null;
    }

    private static bool AddInterfaceMethods(INamespaceOrTypeSymbol asyncApiInterface, WellKnownAttributes wellKnownAttributes, string fileName, out List<MethodsToGenerate> methods)
    {
        methods = new();

        var asyncApiAttributes = asyncApiInterface.GetAttributes(wellKnownAttributes.AsyncApiAttribute);
        //foreach (AttributeData asyncApiAttr in asyncApiAttributes)
        //{
        //    var documentName = asyncApiAttr.ConstructorArguments.FirstOrDefault().Value?.ToString();
        //}

        if (!asyncApiAttributes.Any())
        {
            return false;
        }

        foreach (var methodSymbol in asyncApiInterface.GetMembers().OfType<IMethodSymbol>())
        {
            var methodName = methodSymbol.Name;
            var methodAttributes = AsyncAttributesDefinition.FromSymbol(methodSymbol, wellKnownAttributes);

            if (methodAttributes.Subscribe == null && methodAttributes.Publish == null)
            {
                throw new InvalidOperationException($"Method '{methodName}' does not define a [PublishOperation] nor a [SubscribeOperation] attribute in {fileName}.");
            }

            var topic = methodAttributes.Channel?.ConstructorArguments.FirstOrDefault().Value?.ToString();
            if (topic != null)
            {
                var parameters = methodSymbol.Parameters.Select(p => new MethodParameterData(p.Type.ToString(), p.Name)).ToList();

                topic = ChannelParametersHelper.ParametrizeChannel(topic, methodAttributes.ChannelParameters, parameters);
                methods.Add(new MethodsToGenerate(methodName, topic, parameters));
            }
        }
        return true;
    }

    private static void Execute(SourceProductionContext context, (ClassToGenerate classToGenerate, ImmutableArray<AdditionalTemplate> templates) source)
    {
        var classToGenerate = source.classToGenerate;
        if (classToGenerate is null)
        {
            return;
        }

        var fileName = $"{classToGenerate.Namespace}.{classToGenerate.ClassName}.g.cs";
        var text = source.templates.SingleOrDefault(x => x.Path.Equals(classToGenerate.Template));
        if (text == null)
        {
            throw new InvalidOperationException($"Template '{classToGenerate.Template}' not found for class '{classToGenerate.ClassName}'.");
        }

        var template = Template.Parse(text.Text);
        if (template.HasErrors)
        {
            throw new InvalidOperationException($"Template '{classToGenerate.Template}' contains errors.\r\n{template.Messages}");
        }

        var renderContext = new ClassContext(classToGenerate.Namespace, classToGenerate.ClassName, classToGenerate.ImplementedAsyncApiInterfaces, classToGenerate.Methods);
        var rendered = template.Render(renderContext, memberRenamer: m => m.Name);
        context.AddSource(fileName, rendered);
    }

    private static void PostInitializationOutput(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("Saunter.AsyncApiService.g.cs", SourceText.From(EmbeddedResource.GetContent("Attributes.cs"), Encoding.UTF8));
    }
}
