using AsyncAPI.Saunter.Generator.Cli.FromSpec;
using AsyncAPI.Saunter.Generator.Cli.ToFile;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSimpleConsole(x => x.SingleLine = true).SetMinimumLevel(LogLevel.Trace));
services.AddToFileCommand();
services.AddFromSpecCommand();

using var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
ConsoleApp.LogError = msg => logger.LogError(msg);
ConsoleApp.ServiceProvider = serviceProvider;
logger.LogDebug($"Generator.Cli args: {string.Join(' ', args)}");

var app = ConsoleApp.Create();
app.Add<ToFileCommand>();
app.Add<FromSpecCommand>();
app.Run(args);

Environment.ExitCode = 0;
