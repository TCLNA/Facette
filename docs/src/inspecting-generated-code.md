# Inspecting Generated Code

Source generators produce code at compile time, but by default the generated files only live in memory. You can configure your project to write them to disk for inspection, debugging, or version control.

## Enabling file output

Add these properties to your `.csproj`:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

After building, generated files will appear under the `Generated/` directory in your project:

```
Generated/
└── Facette/
    └── Facette.Generator/
        ├── ProductDto.g.cs
        ├── EmployeeDto.g.cs
        ├── ProductFacetteMapperExtensions.g.cs
        └── ...
```

## Excluding from compilation

When `EmitCompilerGeneratedFiles` is enabled, the generated `.cs` files are written to disk **and** included in compilation by default — which causes duplicate symbol errors since the generator already provides them in memory. Exclude the output directory from compilation:

```xml
<ItemGroup>
  <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
</ItemGroup>
```

## Full example

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Facette.Abstractions" Version="*" />
    <PackageReference Include="Facette" Version="*" />
  </ItemGroup>
</Project>
```

## What you'll find

Each `[Facette]`-annotated DTO produces a `.g.cs` file containing:

- The generated `partial record` with all mapped properties
- `FromSource()`, `ToSource()`, and `Projection` members
- Partial method declarations for [hooks](./hooks.md) (`OnAfterFromSource`, etc.)

If `GenerateMapper = true` (the default), a separate mapper extensions file is also generated.

## Tips

- Add `Generated/` to your `.gitignore` — these files are reproducible from source and don't need to be committed
- Use the generated files to understand exactly what Facette produces, which is helpful for debugging mapping issues
- Your IDE may already show generated files under **Dependencies > Analyzers > Facette.Generator** without needing `EmitCompilerGeneratedFiles`
