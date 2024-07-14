using ConsoleAppFramework;
using LEGO.AsyncAPI;
using LEGO.AsyncAPI.Models;
using Microsoft.Extensions.Logging;

namespace AsyncAPI.Saunter.Generator.Cli.FromSpecCommand;

internal class FromSpecCommand(ILogger<FromSpecCommand> logger)
{
    /// <summary>
    /// Retrieves AsyncAPI spec from a startup assembly and writes to file.
    /// </summary>
    /// <param name="specs">The AsyncAPI specification to generate code for. Parameter should include the namespace: namespace,asyncapi.json</param>
    [Command("fromspec")]
    public int FromSpec(params string[] specs)
    {
        logger.LogInformation($"FromSpec(#{specs.Length}): {string.Join(';', specs)}");
        return 0;
    }
}
