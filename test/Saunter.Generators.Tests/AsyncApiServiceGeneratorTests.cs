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
                     """, output);
    }

    [Fact]
    public void AsyncApiInterface_FromImplementedInterfacesList_IsGenerated()
    {
        var source = """
                     namespace Foo
                     {
                         [Saunter.Generators.AsyncApiService("Templates\\WriteToConsoleTemplate.txt")]
                         internal partial class Commands : ICommands
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
                     """, output);
    }

    [Fact]
    public void AsyncApiInterface_FromAsyncApiServiceAttribute_IsGenerated()
    {
        var source = """
                     namespace Foo
                     {
                         [Saunter.Generators.AsyncApiService("Templates\\WriteToConsoleTemplate.txt", typeof(ICommands))]
                         internal partial class Commands
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
                     """, output);
    }

    [Fact]
    public void MultipleAsyncApiAttributes_IsGenerated()
    {
        var source = """
                     namespace Foo
                     {
                         [Saunter.Generators.AsyncApiService("Templates\\WriteToConsoleTemplate.txt", typeof(ICommands), typeof(IMoreCommands))]
                         internal partial class Commands
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
                         
                         [Saunter.Attributes.AsyncApi]
                         interface IMoreCommands
                         {
                             [Saunter.Attributes.Channel("command.more")]
                             [Saunter.Attributes.SubscribeOperation(typeof(SomethingCommand))]
                             void SomethingMore(SomethingCommand command);
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
                     
                         public void SomethingMore(Foo.SomethingCommand command)
                             => Console.WriteLine($"Invoking 'command.more' with command type <Foo.SomethingCommand>.");
                     }
                     """, output);
    }
}
