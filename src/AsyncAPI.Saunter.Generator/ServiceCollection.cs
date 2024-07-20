using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace AsyncAPI.Saunter.Generator;

public static class GeneratorServiceCollection
{
    public static IServiceCollection Create()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddSimpleConsole(x => x.SingleLine = true).SetMinimumLevel(LogLevel.Trace));
        return services;
    }
}
