// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit.Abstractions;

namespace Saunter.Generators.Tests;

public class AsyncApiServiceGeneratorTests(ITestOutputHelper output) : SourceGeneratorTests(output)
{
    [Fact]
    public void AsyncApiServiceAttribute_IsGenerated()
    {
        var source = """
                     namespace Foo
                     {
                     }
                     """;
        var output = this.Run<AsyncApiServiceGenerator>(source);

        Assert.Equal("""
                     namespace Saunter.Generators;
                     
                     [AttributeUsage(AttributeTargets.Class)]
                     internal class AsyncApiServiceAttribute(string templateFileName) : System.Attribute
                     {
                         public string TemplateFileName { get; } = templateFileName;
                     }
                     """, output.Trim());
    }

    [Fact]
    public void PartialClassWithAsyncApiServiceAttribute_ChannelAndSubscribeOnMethod_IsGenerated()
    {
        var source = """
                     namespace Foo
                     {
                         [Saunter.Generators.AsyncApiService("Templates\\WriteToConsoleTemplate.txt")]
                         internal partial class Commands(IExternalNotificationSender sender) : ICommands
                         {
                         }
                     
                         [Saunter.Attributes.AsyncApi]
                         interface ICommands
                         {
                             [Saunter.Attributes.Channel("command.do")]
                             [Saunter.Attributes.SubscribeOperation(typeof(SomethingCommand))]
                             void Something(SomethingCommand command);
                         }
                         class SomethingCommand { }
                     }
                     """;

        var output = this.Run<AsyncApiServiceGenerator>(source);

        Assert.Equal("""
                     namespace Foo;

                     partial class Commands
                     {
                         public void Something(Foo.SomethingCommand command)
                             => Console.WriteLine($"Invoking 'command.do' with command type <Foo.SomethingCommand>.");
                     }
                     """, output.Trim());
    }
}
