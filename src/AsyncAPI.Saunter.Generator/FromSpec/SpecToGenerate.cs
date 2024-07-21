using CaseConverter;

namespace AsyncAPI.Saunter.Generator.FromSpec;

public record SpecToGenerate(string NamespaceName, string SpecFilePath)
{
    private string _fileContents;

    /// <summary> AsyncAPI spec file contents. </summary>
    public string FileContents => this._fileContents ??= File.ReadAllText(this.SpecFilePath);

    /// <summary> AsyncAPI spec file name, Pascal Cased, without file extensions. </summary>
    public string SpecName => Path.GetFileNameWithoutExtension(this.SpecFilePath).ToPascalCase();

    /// <summary> AsyncAPI spec file name, including file extension. </summary>
    public string SpecFileName => Path.GetFileName(this.SpecFilePath);
}
