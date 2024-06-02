// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;

namespace Saunter.Generators.Helpers;

internal static class EmbeddedResource
{
    public static string GetContent(string relativePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        string baseName = assembly.GetName().Name;
        string resourceName = relativePath
            .TrimStart('.')
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.');
        string fullName = baseName + "." + resourceName;

        using Stream stream = assembly.GetManifestResourceStream(fullName);

        if (stream == null)
        {
            throw new FileNotFoundException($"Resource '{fullName}' doesn't exist.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
