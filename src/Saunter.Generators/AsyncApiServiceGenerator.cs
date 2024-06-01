// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Saunter.Generators.Model;
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
        return node is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.AttributeLists.Count > 0 && classDeclarationSyntax.BaseList?.Types.Count > 0;
    }

    private static ClassToGenerate GetSemanticTarget(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        var generatorAttributeSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("Saunter.Generators.AsyncApiServiceAttribute");
        var asyncApiMarkerSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("Saunter.Attributes.AsyncApiAttribute");
        var asyncApiChannelMarkerSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("Saunter.Attributes.ChannelAttribute");
        var asyncApiChannelParameterMarkerSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("Saunter.Attributes.ChannelParameterAttribute");
        var asyncApiSubscribeOperationMarkerSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("Saunter.Attributes.SubscribeOperationAttribute");
        var asyncApiPublishOperationMarkerSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("Saunter.Attributes.PublishOperationAttribute");

        if (classSymbol is not null && generatorAttributeSymbol is not null && asyncApiMarkerSymbol is not null && asyncApiChannelMarkerSymbol is not null &&
            asyncApiChannelParameterMarkerSymbol is not null && asyncApiSubscribeOperationMarkerSymbol is not null && asyncApiPublishOperationMarkerSymbol is not null)
        {
            foreach (AttributeData generatorAttributeData in classSymbol.GetAttributes())
            {
                if (generatorAttributeSymbol.Equals(generatorAttributeData.AttributeClass, SymbolEqualityComparer.Default))
                {
                    var className = classSymbol.Name;
                    var fileName = classDeclarationSyntax.GetLocation().SourceTree.FilePath;

                    var methods = new List<MethodsToGenerate>();
                    var interfaces = new List<string>();
                    foreach (SimpleBaseTypeSyntax interfaceSyntax in classDeclarationSyntax.BaseList.Types.OfType<SimpleBaseTypeSyntax>())
                    {
                        if (interfaceSyntax.Type is IdentifierNameSyntax identifierNameSyntax)
                        {
                            var typeInfo = context.SemanticModel.GetTypeInfo(identifierNameSyntax).Type;
                            var interfaceAttributes = typeInfo.GetAttributes();
                            var asyncApiAttributes = interfaceAttributes.Where(a => asyncApiMarkerSymbol.Equals(a.AttributeClass, SymbolEqualityComparer.Default)).ToList();
                            foreach (AttributeData asyncApiAttr in asyncApiAttributes)
                            {
                                var documentName = asyncApiAttr.ConstructorArguments.FirstOrDefault().Value?.ToString();
                            }

                            if (asyncApiAttributes.Count > 0)
                            {
                                foreach (IMethodSymbol method in typeInfo.GetMembers().OfType<IMethodSymbol>())
                                {
                                    var methodName = method.Name;
                                    var methodAttributes = method.GetAttributes();
                                    var channelAttribute = methodAttributes.SingleOrDefault(a => asyncApiChannelMarkerSymbol.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
                                    var channelParametersAttributes = methodAttributes.Where(a => asyncApiChannelParameterMarkerSymbol.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
                                    var subscribeAttribute = methodAttributes.SingleOrDefault(a => asyncApiSubscribeOperationMarkerSymbol.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
                                    var publishAttribute = methodAttributes.SingleOrDefault(a => asyncApiPublishOperationMarkerSymbol.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
                                    var parameters = method.Parameters.Select(p => new MethodParameterData(p.Type.ToString(), p.Name)).ToList();

                                    if (subscribeAttribute == null && publishAttribute == null)
                                    {
                                        throw new InvalidOperationException($"Method '{methodName}' does not define a [PublishOperation] nor a [SubscribeOperation] attribute in {fileName}.");
                                    }
                                    var topic = channelAttribute?.ConstructorArguments.FirstOrDefault().Value?.ToString();
                                    if (topic != null)
                                    {
                                        var channelParameters = channelParametersAttributes.Select(a => new ChannelParameterData(a.ConstructorArguments[0].Value?.ToString(), a.ConstructorArguments[1].Value?.ToString())).ToList();
                                        topic = ReplaceChannelParameters(topic, channelParameters, parameters, fileName);
                                        methods.Add(new MethodsToGenerate(methodName, topic, parameters));
                                    }
                                }
                                var interfaceName = identifierNameSyntax.Identifier.ValueText;
                                interfaces.Add(interfaceName);
                            }
                        }
                    }

                    if (interfaces.Count == 0)
                    {
                        throw new InvalidOperationException($"Class '{className}' (file: {fileName}) was marked with [AsyncApiServiceAttribute] but does not implement any interface marked with [AsyncApiAttribute].");
                    }

                    if (methods.Count > 0)
                    {
                        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                        var template = generatorAttributeData.ConstructorArguments[0].Value?.ToString();

                        // Prepend the path of the generating class if not empty to allow paths relative to the generating class' location.
                        if (!string.IsNullOrWhiteSpace(fileName))
                        {
                            template = Path.Combine(Path.GetDirectoryName(fileName), template);
                        }

                        return new ClassToGenerate(template, namespaceName, className, interfaces, methods);
                    }
                }
            }
        }

        return null;
    }

    private static string ReplaceChannelParameters(string topic, ICollection<ChannelParameterData> channelParameters, List<MethodParameterData> parameters, string fileName)
    {
        var result = topic;
        foreach (var channelParameter in channelParameters)
        {
            var parameter = parameters.SingleOrDefault(x => x.ParameterTypeName == channelParameter.ChannelParameterTypeName)
                            ?? throw new InvalidOperationException($"Unknown [ChannelParameter] defined: '{channelParameter.ChannelParameterName}' of type '{channelParameter.ChannelParameterTypeName}' in {fileName}.");
            result = result.Replace(channelParameter.ParameterNameNeedle, parameter.ParameterNameNeedle);
        }
        if (result.Contains('{'))
        {
            var splitted = result.Split(['.', '/']);
            var invalidParameter = splitted.FirstOrDefault(x => x.Contains('{') && parameters.All(cp => cp.ParameterNameNeedle != x));
            if (invalidParameter != default)
            {
                throw new InvalidOperationException($"Channel '{topic}' contains unknown channel parameter: '{invalidParameter}' in {fileName}.");
            }
        }
        return result;
    }

    private static void Execute(SourceProductionContext context, (ClassToGenerate classToGenerate, ImmutableArray<AdditionalTemplate> templates) source)
    {
        var classToGenerate = source.classToGenerate;
        if (classToGenerate is null)
        {
            return;
        }

        var fileName = $"{classToGenerate.Namespace}.{classToGenerate.ClassName}.g.cs";
        var text = source.templates.SingleOrDefault(x => x.Path.Equals(classToGenerate.Template)) ?? throw new FileNotFoundException($"Template '{classToGenerate.Template}' not found for class '{classToGenerate.ClassName}'.");
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
        context.AddSource("Saunter.AsyncApiService.g.cs",
            """
            namespace Saunter.Generators;

            [AttributeUsage(AttributeTargets.Class)]
            internal class AsyncApiServiceAttribute(string templateFileName) : System.Attribute
            {
                public string TemplateFileName { get; } = templateFileName;
            }
            """);
    }
}
