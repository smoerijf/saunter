// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Saunter.Generators.Internal;

internal record WellKnownAttributes(
    INamedTypeSymbol GeneratorAttribute,
    INamedTypeSymbol AsyncApiAttribute,
    INamedTypeSymbol ChannelAttribute,
    INamedTypeSymbol ChannelParameterAttribute,
    INamedTypeSymbol SubscribeOperationAttribute,
    INamedTypeSymbol PublishOperationAttribute)
{
    private const string SaunterAttributesNamespace = "Saunter.Attributes.";
    private const string SaunterGeneratorsNamespace = "Saunter.Generators.";

    internal static WellKnownAttributes GetWellKnownAttributes(Compilation compilation) => new(
            compilation.GetTypeByMetadataName($"{SaunterGeneratorsNamespace}AsyncApiServiceAttribute"),
            compilation.GetTypeByMetadataName($"{SaunterAttributesNamespace}AsyncApiAttribute"),
            compilation.GetTypeByMetadataName($"{SaunterAttributesNamespace}ChannelAttribute"),
            compilation.GetTypeByMetadataName($"{SaunterAttributesNamespace}ChannelParameterAttribute"),
            compilation.GetTypeByMetadataName($"{SaunterAttributesNamespace}SubscribeOperationAttribute"),
            compilation.GetTypeByMetadataName($"{SaunterAttributesNamespace}PublishOperationAttribute"));

    internal bool IsValid() => this.GeneratorAttribute is not null &&
                               this.AsyncApiAttribute is not null &&
                               this.ChannelAttribute is not null &&
                               this.ChannelParameterAttribute is not null &&
                               this.SubscribeOperationAttribute is not null &&
                               this.PublishOperationAttribute is not null;
}
