namespace AsyncAPI.Saunter.Generator.FromSpec.DataTypes;

internal class DataTypesGeneratorState
{
    public List<string> AlreadyGeneratedDataTypes { get; } = new();

    public List<string> AlreadyGeneratedNamespaces { get; } = new();
}
