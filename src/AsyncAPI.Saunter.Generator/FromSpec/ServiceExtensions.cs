using AsyncAPI.Saunter.Generator.FromSpec.AsyncApiInterface;
using AsyncAPI.Saunter.Generator.FromSpec.DataTypes;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncAPI.Saunter.Generator.FromSpec;

public static class ServiceExtensions
{
    public static IServiceCollection AddFromSpecCodeGenerator(this IServiceCollection services)
    {
        services.AddTransient<IAsyncApiGenerator, AsyncApiGenerator>();
        services.AddTransient<IDataTypesGenerator, NSwagGenerator>();
        services.AddTransient<IAsyncApiCodeGenerator, CodeGenerator>();
        return services;
    }
}
