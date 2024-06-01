// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

namespace Saunter.Generators.Tests;

public class CustomAdditionalText(string path) : AdditionalText
{
    public override string Path { get; } = path;

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        var text = File.ReadAllText(Path, Encoding.UTF8);
        return SourceText.From(text);
    }
}
