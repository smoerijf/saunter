// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Saunter.Generators.Model;

internal record ClassToGenerate(string Template, string Namespace, string ClassName, List<string> ImplementedAsyncApiInterfaces, List<MethodsToGenerate> Methods);

internal record ChannelParameterData(string ChannelParameterName, string ChannelParameterTypeName)
{
    public string ParameterNameNeedle { get; } = $"{{{ChannelParameterName}}}";
}
