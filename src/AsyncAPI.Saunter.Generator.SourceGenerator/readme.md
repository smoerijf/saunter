# AsyncApi Generator.SourceGenerator Nuget Package
A nuget package to generate data classes and AsyncAPI interface(s) from AsyncAPI spec file(s). Add ```AdditionalFiles``` to your csproj-file 
to specify for which AsyncAPI specification file(s) source code needs to be generated and in which namespace. 

## Configuration
After adding the nuget package reference, the AsyncAPI code generation needs to be configured in the csproj-file:
```
  <ItemGroup>
    <!-- Instruct "AsyncAPI.Saunter.Generator.SourceGenerator" to generate classes for these AsyncAPI specifiction files -->
    <AdditionalFiles Include="specs/streetlights.json" Namespace="Saunter" />
    <AdditionalFiles Include="specs/streetlights.yml" Namespace="Saunter.Yml" />
  </ItemGroup>
```

## Debugging options
Configure rosyln to emit generated source code files on disk. Default location: 
```<Csproj-root>/obj/Debug/<TargetFramework>/generated/AsyncAPI.Saunter.Generator.SourceGenerator/AsyncAPI.Saunter.Generator.SourceGenerator.SpecFirstCodeGenerator```
```
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>

    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  </PropertyGroup>
```

Configure roslyn to emit generated source code in a custom location:
```
  <PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>
```

These files will not show up in Visual Studio by default, but these can be added:
```
  <ItemGroup>
    <!-- Exclude the output of source generators from the compilation, but include into the project -->
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.g.cs" />
    <None Include="$(CompilerGeneratedFilesOutputPath)/**/*.g.cs" />
  </ItemGroup>
```

Since these file are created on disk, most likely within a directory under git source control, these generated files 
can be excluded from git by adding this line to .gitignore file:
```
*g.cs
```