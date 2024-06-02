// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Saunter.Generators
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AsyncApiServiceAttribute : System.Attribute
    {
        public string TemplateFileName { get; }

        public System.Type[] AsyncApiInterfaceTypes { get; }

        public AsyncApiServiceAttribute(string templateFileName, params System.Type[] asyncApiInterfaceTypes)
        {
            this.TemplateFileName = templateFileName;
            this.AsyncApiInterfaceTypes = asyncApiInterfaceTypes;
        }
    }
}
