// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Saunter.Generators.Model;
using System.Collections.Generic;
using System.Linq;
using System;
using Microsoft.CodeAnalysis;

namespace Saunter.Generators.Internal;

internal static class ChannelParametersHelper
{
    public static string ParametrizeChannel(string channel, List<AttributeData> channelParameterAttributes, List<MethodParameterData> parameters)
    {
        var channelParameters = channelParameterAttributes.Select(a => new ChannelParameterData(a.ConstructorArguments[0].Value?.ToString(), a.ConstructorArguments[1].Value?.ToString()));
        channel = ReplaceChannelParameters(channel, channelParameters, parameters);
        return channel;
    }

    private static string ReplaceChannelParameters(string channel, IEnumerable<ChannelParameterData> channelParameters, List<MethodParameterData> parameters)
    {
        var result = channel;
        foreach (var channelParameter in channelParameters)
        {
            var parameter = parameters.SingleOrDefault(x => x.ParameterTypeName == channelParameter.ChannelParameterTypeName)
                            ?? throw new InvalidOperationException($"Unknown [ChannelParameter] defined: '{channelParameter.ChannelParameterName}' of type '{channelParameter.ChannelParameterTypeName}'.");
            result = result.Replace(channelParameter.ParameterNameNeedle, parameter.ParameterNameNeedle);
        }
        if (result.Contains('{'))
        {
            var splitted = result.Split(['.', '/']);
            var invalidParameter = splitted.FirstOrDefault(x => x.Contains('{') && parameters.All(cp => cp.ParameterNameNeedle != x));
            if (invalidParameter != default)
            {
                throw new InvalidOperationException($"Channel '{channel}' contains unknown channel parameter: '{invalidParameter}'.");
            }
        }
        return result;
    }
}
