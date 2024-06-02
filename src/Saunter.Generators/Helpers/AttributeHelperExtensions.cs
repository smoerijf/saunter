// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Saunter.Generators.Helpers;

internal static class AttributeHelperExtensions
{
    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attributeSymbol)
    {
        return symbol.GetAttributes().Any(a => attributeSymbol.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
    }

    public static IEnumerable<AttributeData> GetAttributes(this ISymbol symbol, INamedTypeSymbol attributeSymbol)
    {
        return symbol.GetAttributes().Where(a => attributeSymbol.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
    }

    public static IEnumerable<AttributeData> OfType(this IEnumerable<AttributeData> attributes, INamedTypeSymbol attributeSymbol)
    {
        return attributes.Where(a => attributeSymbol.Equals(a.AttributeClass, SymbolEqualityComparer.Default));
    }
}
