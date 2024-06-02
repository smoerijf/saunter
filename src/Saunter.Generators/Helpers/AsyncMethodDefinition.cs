// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Saunter.Generators.Helpers;

internal record AsyncAttributesDefinition(AttributeData Channel, List<AttributeData> ChannelParameters, AttributeData Subscribe, AttributeData Publish)
{
    public static AsyncAttributesDefinition FromSymbol(ISymbol symbol, WellKnownAttributes wellKnownAttributes)
    {
        var attributes = symbol.GetAttributes();

        return new AsyncAttributesDefinition(
            attributes.OfType(wellKnownAttributes.ChannelAttribute).SingleOrDefault(),
            attributes.OfType(wellKnownAttributes.ChannelParameterAttribute).ToList(),
            attributes.OfType(wellKnownAttributes.SubscribeOperationAttribute).SingleOrDefault(),
            attributes.OfType(wellKnownAttributes.PublishOperationAttribute).SingleOrDefault());
    }
}
