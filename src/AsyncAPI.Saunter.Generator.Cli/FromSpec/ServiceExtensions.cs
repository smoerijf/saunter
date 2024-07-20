using AsyncAPI.Saunter.Generator.Cli.FromSpec.AsyncApiInterface;
using AsyncAPI.Saunter.Generator.Cli.FromSpec.DataTypes;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncAPI.Saunter.Generator.Cli.FromSpec;

internal static class ServiceExtensions
{
    public static IServiceCollection AddFromSpecCommand(this IServiceCollection services)
    {
        services.AddTransient<IAsyncApiGenerator, AsyncApiGenerator>();
        services.AddTransient<IDataTypesGenerator, NSwagGenerator>();
        return services;
    }
}
