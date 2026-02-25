# Facette Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a .NET Roslyn source generator that generates DTO properties, mapping methods, projection expressions, and companion mapper classes from a single `[Facette]` attribute.

**Architecture:** Two NuGet packages — `Facette.Abstractions` (attributes, netstandard2.0+) and `Facette` (IIncrementalGenerator, netstandard2.0). The generator discovers `[Facette]`-annotated partial types, builds an intermediate model, and emits two source files per target: the partial record with properties/methods and a static mapper class.

**Tech Stack:** .NET 9 SDK, C# 13, Roslyn `Microsoft.CodeAnalysis.CSharp` 4.x, xUnit, Verify.SourceGenerators for snapshot tests.

---

### Task 1: Solution Scaffolding

**Files:**
- Create: `Facette.sln`
- Create: `Directory.Build.props`
- Create: `src/Facette.Abstractions/Facette.Abstractions.csproj`
- Create: `src/Facette/Facette.csproj`
- Create: `tests/Facette.Tests/Facette.Tests.csproj`
- Create: `tests/Facette.IntegrationTests/Facette.IntegrationTests.csproj`

**Step 1: Create solution and projects**

```bash
dotnet new sln -n Facette
mkdir -p src/Facette.Abstractions src/Facette tests/Facette.Tests tests/Facette.IntegrationTests
```

**Step 2: Create `Directory.Build.props`**

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>
</Project>
```

**Step 3: Create `src/Facette.Abstractions/Facette.Abstractions.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>netstandard2.0;net8.0;net9.0</TargetFrameworks>
  </PropertyGroup>
</Project>
```

**Step 4: Create `src/Facette/Facette.csproj`**

This is the source generator project. It must target `netstandard2.0`, reference Roslyn analyzers, and be packed into the `analyzers/dotnet/cs` folder. It must NOT ship its build output as a regular dependency.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>
    <IncludeBuildOutput>false</IncludeBuildOutput>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Facette.Abstractions\Facette.Abstractions.csproj" />
  </ItemGroup>
</Project>
```

**Step 5: Create `tests/Facette.Tests/Facette.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" />
    <PackageReference Include="Verify.Xunit" Version="28.*" />
    <PackageReference Include="Verify.SourceGenerators" Version="2.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Facette\Facette.csproj" />
    <ProjectReference Include="..\..\src\Facette.Abstractions\Facette.Abstractions.csproj" />
  </ItemGroup>
</Project>
```

**Step 6: Create `tests/Facette.IntegrationTests/Facette.IntegrationTests.csproj`**

This project references the generator as an actual analyzer (so generated code is available at compile time).

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Facette.Abstractions\Facette.Abstractions.csproj" />
    <ProjectReference Include="..\..\src\Facette\Facette.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

**Step 7: Add projects to solution**

```bash
dotnet sln add src/Facette.Abstractions/Facette.Abstractions.csproj
dotnet sln add src/Facette/Facette.csproj
dotnet sln add tests/Facette.Tests/Facette.Tests.csproj
dotnet sln add tests/Facette.IntegrationTests/Facette.IntegrationTests.csproj
```

**Step 8: Verify solution builds**

```bash
dotnet build
```

Expected: Build succeeds with no errors.

**Step 9: Commit**

```bash
git add -A
git commit -m "chore: scaffold Facette solution structure"
```

---

### Task 2: FacetteAttribute

**Files:**
- Create: `src/Facette.Abstractions/FacetteAttribute.cs`

**Step 1: Write the failing test**

Create `tests/Facette.Tests/FacetteAttributeTests.cs`:

```csharp
using Facette.Abstractions;

namespace Facette.Tests;

public class FacetteAttributeTests
{
    [Fact]
    public void Constructor_SetsSourceType()
    {
        var attr = new FacetteAttribute(typeof(string));
        Assert.Equal(typeof(string), attr.SourceType);
    }

    [Fact]
    public void Constructor_SetsExclude()
    {
        var attr = new FacetteAttribute(typeof(string), "A", "B");
        Assert.Equal(new[] { "A", "B" }, attr.Exclude);
    }

    [Fact]
    public void Constructor_DefaultExcludeIsEmpty()
    {
        var attr = new FacetteAttribute(typeof(string));
        Assert.Empty(attr.Exclude);
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var attr = new FacetteAttribute(typeof(string));
        Assert.Null(attr.Include);
        Assert.True(attr.GenerateToSource);
        Assert.True(attr.GenerateProjection);
        Assert.True(attr.GenerateMapper);
    }
}
```

**Step 2: Run test to verify it fails**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~FacetteAttributeTests" -v minimal
```

Expected: FAIL — `FacetteAttribute` does not exist.

**Step 3: Write minimal implementation**

Create `src/Facette.Abstractions/FacetteAttribute.cs`:

```csharp
namespace Facette.Abstractions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class FacetteAttribute : Attribute
{
    public FacetteAttribute(Type sourceType, params string[] exclude)
    {
        SourceType = sourceType;
        Exclude = exclude;
    }

    public Type SourceType { get; }
    public string[] Exclude { get; }
    public string[]? Include { get; set; }
    public bool GenerateToSource { get; set; } = true;
    public bool GenerateProjection { get; set; } = true;
    public bool GenerateMapper { get; set; } = true;
}
```

**Step 4: Run test to verify it passes**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~FacetteAttributeTests" -v minimal
```

Expected: All 4 tests PASS.

**Step 5: Commit**

```bash
git add src/Facette.Abstractions/FacetteAttribute.cs tests/Facette.Tests/FacetteAttributeTests.cs
git commit -m "feat: add FacetteAttribute with include/exclude and generation flags"
```

---

### Task 3: Generator Skeleton + Test Infrastructure

**Files:**
- Create: `src/Facette/FacetteGenerator.cs`
- Create: `tests/Facette.Tests/Helpers/GeneratorTestHelper.cs`
- Create: `tests/Facette.Tests/GeneratorTests.cs`

**Step 1: Create the test helper**

This helper compiles C# source with the Facette generator and returns the results. Create `tests/Facette.Tests/Helpers/GeneratorTestHelper.cs`:

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Facette.Tests.Helpers;

public static class GeneratorTestHelper
{
    public static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        // Add Facette.Abstractions reference
        references.Add(MetadataReference.CreateFromFile(
            typeof(Facette.Abstractions.FacetteAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new Facette.Generator.FacetteGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var diagnostics);

        return driver.GetRunResult();
    }

    public static (GeneratorDriverRunResult Result, IReadOnlyList<Diagnostic> Diagnostics) RunGeneratorWithDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        references.Add(MetadataReference.CreateFromFile(
            typeof(Facette.Abstractions.FacetteAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new Facette.Generator.FacetteGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var diagnostics);

        var result = driver.GetRunResult();
        var allDiagnostics = outputCompilation.GetDiagnostics()
            .Where(d => d.Id.StartsWith("FCT"))
            .ToList();

        return (result, allDiagnostics);
    }
}
```

**Step 2: Write the failing test**

Create `tests/Facette.Tests/GeneratorTests.cs`:

```csharp
using Facette.Tests.Helpers;

namespace Facette.Tests;

public class GeneratorTests
{
    [Fact]
    public void Generator_WithSimpleEntity_GeneratesOutput()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        Assert.NotEmpty(result.GeneratedTrees);
    }
}
```

**Step 3: Run test to verify it fails**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~GeneratorTests" -v minimal
```

Expected: FAIL — `FacetteGenerator` does not exist.

**Step 4: Write minimal generator skeleton**

Create `src/Facette/FacetteGenerator.cs`:

```csharp
using Microsoft.CodeAnalysis;

namespace Facette.Generator;

[Generator]
public sealed class FacetteGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Facette.Abstractions.FacetteAttribute",
            predicate: static (node, _) => true,
            transform: static (ctx, _) => ctx
        );

        context.RegisterSourceOutput(targets, static (spc, ctx) =>
        {
            var symbol = ctx.TargetSymbol as INamedTypeSymbol;
            if (symbol is null) return;

            var name = symbol.Name;
            var ns = symbol.ContainingNamespace.ToDisplayString();

            var code = $$"""
                // <auto-generated />
                #nullable enable

                namespace {{ns}};

                public partial record {{name}}
                {
                }
                """;

            spc.AddSource($"{name}.g.cs", code);
        });
    }
}
```

**Step 5: Run test to verify it passes**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~GeneratorTests" -v minimal
```

Expected: PASS — generator produces output (empty partial record for now).

**Step 6: Commit**

```bash
git add src/Facette/FacetteGenerator.cs tests/Facette.Tests/Helpers/GeneratorTestHelper.cs tests/Facette.Tests/GeneratorTests.cs
git commit -m "feat: add FacetteGenerator skeleton with test infrastructure"
```

---

### Task 4: Model Building (Attribute Parsing)

**Files:**
- Create: `src/Facette/Models/FacetteTargetModel.cs`
- Create: `src/Facette/Builders/ModelBuilder.cs`
- Create: `tests/Facette.Tests/ModelBuilderTests.cs`

**Step 1: Write the failing test**

Create `tests/Facette.Tests/ModelBuilderTests.cs`:

```csharp
using Facette.Tests.Helpers;

namespace Facette.Tests;

public class ModelBuilderTests
{
    [Fact]
    public void Generator_WithSimpleEntity_GeneratesProperties()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Email { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string Name { get; init; }", generatedCode);
        Assert.Contains("public string Email { get; init; }", generatedCode);
    }

    [Fact]
    public void Generator_WithExclude_OmitsExcludedProperties()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Password { get; set; }
            }

            [Facette(typeof(User), "Password")]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string Name { get; init; }", generatedCode);
        Assert.DoesNotContain("Password", generatedCode);
    }

    [Fact]
    public void Generator_WithInclude_OnlyIncludesSpecifiedProperties()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Email { get; set; }
                public string Password { get; set; }
            }

            [Facette(typeof(User), Include = new[] { "Id", "Name" })]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string Name { get; init; }", generatedCode);
        Assert.DoesNotContain("Email", generatedCode);
        Assert.DoesNotContain("Password", generatedCode);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~ModelBuilderTests" -v minimal
```

Expected: FAIL — generated code doesn't contain properties yet.

**Step 3: Create FacetteTargetModel**

Create `src/Facette/Models/FacetteTargetModel.cs`:

```csharp
using System.Collections.Immutable;

namespace Facette.Generator.Models;

public sealed record FacetteTargetModel(
    string Namespace,
    string TypeName,
    string SourceTypeFullName,
    ImmutableArray<PropertyModel> Properties,
    bool GenerateToSource,
    bool GenerateProjection,
    bool GenerateMapper
);

public sealed record PropertyModel(
    string Name,
    string TypeFullName,
    bool IsValueType,
    bool IsNullableReferenceType
);
```

**Step 4: Create ModelBuilder**

Create `src/Facette/Builders/ModelBuilder.cs`:

```csharp
using System.Collections.Immutable;
using Facette.Generator.Models;
using Microsoft.CodeAnalysis;

namespace Facette.Generator.Builders;

public static class ModelBuilder
{
    public static FacetteTargetModel? Build(GeneratorAttributeSyntaxContext context)
    {
        var targetSymbol = context.TargetSymbol as INamedTypeSymbol;
        if (targetSymbol is null) return null;

        var attribute = context.Attributes
            .FirstOrDefault(a => a.AttributeClass?.Name == "FacetteAttribute");
        if (attribute is null) return null;

        // Extract source type
        var sourceType = attribute.ConstructorArguments[0].Value as INamedTypeSymbol;
        if (sourceType is null) return null;

        // Extract exclude list from params
        var excludeArg = attribute.ConstructorArguments.Length > 1
            ? attribute.ConstructorArguments[1]
            : default;
        var exclude = excludeArg.Kind == TypedConstantKind.Array
            ? excludeArg.Values.Select(v => v.Value?.ToString() ?? "").ToHashSet()
            : new HashSet<string>();

        // Extract Include named argument
        var includeArg = attribute.NamedArguments
            .FirstOrDefault(a => a.Key == "Include");
        HashSet<string>? include = null;
        if (includeArg.Key == "Include" && includeArg.Value.Kind == TypedConstantKind.Array)
        {
            include = includeArg.Value.Values
                .Select(v => v.Value?.ToString() ?? "")
                .ToHashSet();
        }

        // Extract generation flags
        bool generateToSource = GetNamedBoolArg(attribute, "GenerateToSource", true);
        bool generateProjection = GetNamedBoolArg(attribute, "GenerateProjection", true);
        bool generateMapper = GetNamedBoolArg(attribute, "GenerateMapper", true);

        // Collect properties from source type
        var properties = GetSourceProperties(sourceType, exclude, include);

        var ns = targetSymbol.ContainingNamespace.IsGlobalNamespace
            ? ""
            : targetSymbol.ContainingNamespace.ToDisplayString();

        return new FacetteTargetModel(
            Namespace: ns,
            TypeName: targetSymbol.Name,
            SourceTypeFullName: sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Properties: properties,
            GenerateToSource: generateToSource,
            GenerateProjection: generateProjection,
            GenerateMapper: generateMapper
        );
    }

    private static ImmutableArray<PropertyModel> GetSourceProperties(
        INamedTypeSymbol sourceType,
        HashSet<string> exclude,
        HashSet<string>? include)
    {
        var builder = ImmutableArray.CreateBuilder<PropertyModel>();

        foreach (var member in sourceType.GetMembers())
        {
            if (member is not IPropertySymbol prop) continue;
            if (prop.DeclaredAccessibility != Accessibility.Public) continue;
            if (prop.IsStatic || prop.IsIndexer) continue;
            if (prop.GetMethod is null) continue;

            if (include is not null && !include.Contains(prop.Name)) continue;
            if (exclude.Contains(prop.Name)) continue;

            var typeDisplay = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isValueType = prop.Type.IsValueType;
            var isNullableRef = prop.NullableAnnotation == NullableAnnotation.Annotated && !isValueType;

            builder.Add(new PropertyModel(
                Name: prop.Name,
                TypeFullName: typeDisplay,
                IsValueType: isValueType,
                IsNullableReferenceType: isNullableRef
            ));
        }

        return builder.ToImmutable();
    }

    private static bool GetNamedBoolArg(AttributeData attribute, string name, bool defaultValue)
    {
        var arg = attribute.NamedArguments.FirstOrDefault(a => a.Key == name);
        if (arg.Key == name && arg.Value.Value is bool val)
            return val;
        return defaultValue;
    }
}
```

**Step 5: Update FacetteGenerator to use ModelBuilder and PropertyBuilder**

Update `src/Facette/FacetteGenerator.cs`:

```csharp
using Facette.Generator.Builders;
using Facette.Generator.Models;
using Microsoft.CodeAnalysis;

namespace Facette.Generator;

[Generator]
public sealed class FacetteGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Facette.Abstractions.FacetteAttribute",
            predicate: static (node, _) => true,
            transform: static (ctx, _) => ModelBuilder.Build(ctx)
        );

        context.RegisterSourceOutput(targets, static (spc, model) =>
        {
            if (model is null) return;
            EmitDtoSource(spc, model);
        });
    }

    private static void EmitDtoSource(SourceProductionContext spc, FacetteTargetModel model)
    {
        var properties = PropertyBuilder.Build(model.Properties);

        var nsOpen = string.IsNullOrEmpty(model.Namespace) ? "" : $"namespace {model.Namespace};\n\n";

        var code = $$"""
            // <auto-generated />
            #nullable enable

            {{nsOpen}}public partial record {{model.TypeName}}
            {
            {{properties}}
            }
            """;

        spc.AddSource($"{model.TypeName}.g.cs", code);
    }
}
```

**Step 6: Create PropertyBuilder**

Create `src/Facette/Builders/PropertyBuilder.cs`:

```csharp
using System.Collections.Immutable;
using System.Text;
using Facette.Generator.Models;

namespace Facette.Generator.Builders;

public static class PropertyBuilder
{
    public static string Build(ImmutableArray<PropertyModel> properties)
    {
        var sb = new StringBuilder();

        foreach (var prop in properties)
        {
            var typeName = prop.TypeFullName;
            var defaultValue = prop.IsValueType ? "" : " = default!;";

            sb.AppendLine($"    public {typeName} {prop.Name} {{ get; init; }}{defaultValue}");
        }

        return sb.ToString().TrimEnd();
    }
}
```

**Step 7: Run tests to verify they pass**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~ModelBuilderTests" -v minimal
```

Expected: All 3 tests PASS.

**Step 8: Commit**

```bash
git add src/Facette/Models/ src/Facette/Builders/ tests/Facette.Tests/ModelBuilderTests.cs
git commit -m "feat: add model building and property generation with include/exclude"
```

---

### Task 5: FromSource Mapping

**Files:**
- Create: `src/Facette/Builders/MappingBuilder.cs`
- Create: `tests/Facette.Tests/MappingTests.cs`

**Step 1: Write the failing test**

Create `tests/Facette.Tests/MappingTests.cs`:

```csharp
using Facette.Tests.Helpers;

namespace Facette.Tests;

public class MappingTests
{
    [Fact]
    public void Generator_GeneratesFromSourceMethod()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public decimal Price { get; set; }
            }

            [Facette(typeof(Product))]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("public static ProductDto FromSource(", generatedCode);
        Assert.Contains("Id = source.Id", generatedCode);
        Assert.Contains("Name = source.Name", generatedCode);
        Assert.Contains("Price = source.Price", generatedCode);
    }

    [Fact]
    public void Generator_GeneratesToSourceMethod()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Product))]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("public global::TestApp.Product ToSource()", generatedCode);
        Assert.Contains("Id = this.Id", generatedCode);
        Assert.Contains("Name = this.Name", generatedCode);
    }

    [Fact]
    public void Generator_WithGenerateToSourceFalse_OmitsToSource()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Product), GenerateToSource = false)]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("FromSource", generatedCode);
        Assert.DoesNotContain("ToSource", generatedCode);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~MappingTests" -v minimal
```

Expected: FAIL — no `FromSource` or `ToSource` in generated code.

**Step 3: Create MappingBuilder**

Create `src/Facette/Builders/MappingBuilder.cs`:

```csharp
using System.Collections.Immutable;
using System.Text;
using Facette.Generator.Models;

namespace Facette.Generator.Builders;

public static class MappingBuilder
{
    public static string BuildFromSource(
        string typeName,
        string sourceTypeFullName,
        ImmutableArray<PropertyModel> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    public static {typeName} FromSource({sourceTypeFullName} source)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new {typeName}");
        sb.AppendLine("        {");

        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            var comma = i < properties.Length - 1 ? "," : "";
            sb.AppendLine($"            {prop.Name} = source.{prop.Name}{comma}");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");

        return sb.ToString();
    }

    public static string BuildToSource(
        string sourceTypeFullName,
        ImmutableArray<PropertyModel> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    public {sourceTypeFullName} ToSource()");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new {sourceTypeFullName}");
        sb.AppendLine("        {");

        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            var comma = i < properties.Length - 1 ? "," : "";
            sb.AppendLine($"            {prop.Name} = this.{prop.Name}{comma}");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");

        return sb.ToString();
    }
}
```

**Step 4: Update FacetteGenerator to emit mapping methods**

Update `EmitDtoSource` in `src/Facette/FacetteGenerator.cs` to include mapping methods:

```csharp
    private static void EmitDtoSource(SourceProductionContext spc, FacetteTargetModel model)
    {
        var properties = PropertyBuilder.Build(model.Properties);
        var fromSource = MappingBuilder.BuildFromSource(model.TypeName, model.SourceTypeFullName, model.Properties);

        var toSource = model.GenerateToSource
            ? MappingBuilder.BuildToSource(model.SourceTypeFullName, model.Properties)
            : "";

        var nsOpen = string.IsNullOrEmpty(model.Namespace) ? "" : $"namespace {model.Namespace};\n\n";

        var code = $$"""
            // <auto-generated />
            #nullable enable

            {{nsOpen}}public partial record {{model.TypeName}}
            {
            {{properties}}

            {{fromSource}}
            {{toSource}}
            }
            """;

        spc.AddSource($"{model.TypeName}.g.cs", code);
    }
```

**Step 5: Run tests to verify they pass**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~MappingTests" -v minimal
```

Expected: All 3 tests PASS.

**Step 6: Commit**

```bash
git add src/Facette/Builders/MappingBuilder.cs tests/Facette.Tests/MappingTests.cs
git commit -m "feat: add FromSource and ToSource mapping generation"
```

---

### Task 6: Projection Expression

**Files:**
- Create: `src/Facette/Builders/ProjectionBuilder.cs`
- Create: `tests/Facette.Tests/ProjectionTests.cs`

**Step 1: Write the failing test**

Create `tests/Facette.Tests/ProjectionTests.cs`:

```csharp
using Facette.Tests.Helpers;

namespace Facette.Tests;

public class ProjectionTests
{
    [Fact]
    public void Generator_GeneratesProjectionExpression()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Order
            {
                public int Id { get; set; }
                public string Description { get; set; }
                public decimal Total { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("public static System.Linq.Expressions.Expression<System.Func<", generatedCode);
        Assert.Contains("Projection =>", generatedCode);
        Assert.Contains("Id = source.Id", generatedCode);
    }

    [Fact]
    public void Generator_WithGenerateProjectionFalse_OmitsProjection()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Order
            {
                public int Id { get; set; }
            }

            [Facette(typeof(Order), GenerateProjection = false)]
            public partial record OrderDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        Assert.DoesNotContain("Projection", generatedCode);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~ProjectionTests" -v minimal
```

Expected: FAIL — no `Projection` in generated code.

**Step 3: Create ProjectionBuilder**

Create `src/Facette/Builders/ProjectionBuilder.cs`:

```csharp
using System.Collections.Immutable;
using System.Text;
using Facette.Generator.Models;

namespace Facette.Generator.Builders;

public static class ProjectionBuilder
{
    public static string Build(
        string typeName,
        string sourceTypeFullName,
        ImmutableArray<PropertyModel> properties)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    public static System.Linq.Expressions.Expression<System.Func<{sourceTypeFullName}, {typeName}>> Projection =>");
        sb.AppendLine($"        source => new {typeName}");
        sb.AppendLine("        {");

        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            var comma = i < properties.Length - 1 ? "," : "";
            sb.AppendLine($"            {prop.Name} = source.{prop.Name}{comma}");
        }

        sb.AppendLine("        };");

        return sb.ToString();
    }
}
```

**Step 4: Update FacetteGenerator to emit projection**

Update `EmitDtoSource` in `src/Facette/FacetteGenerator.cs` to include projection:

```csharp
    private static void EmitDtoSource(SourceProductionContext spc, FacetteTargetModel model)
    {
        var properties = PropertyBuilder.Build(model.Properties);
        var fromSource = MappingBuilder.BuildFromSource(model.TypeName, model.SourceTypeFullName, model.Properties);

        var toSource = model.GenerateToSource
            ? MappingBuilder.BuildToSource(model.SourceTypeFullName, model.Properties)
            : "";

        var projection = model.GenerateProjection
            ? ProjectionBuilder.Build(model.TypeName, model.SourceTypeFullName, model.Properties)
            : "";

        var nsOpen = string.IsNullOrEmpty(model.Namespace) ? "" : $"namespace {model.Namespace};\n\n";

        var code = $$"""
            // <auto-generated />
            #nullable enable

            {{nsOpen}}public partial record {{model.TypeName}}
            {
            {{properties}}

            {{fromSource}}
            {{toSource}}
            {{projection}}
            }
            """;

        spc.AddSource($"{model.TypeName}.g.cs", code);
    }
```

**Step 5: Run tests to verify they pass**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~ProjectionTests" -v minimal
```

Expected: All 2 tests PASS.

**Step 6: Commit**

```bash
git add src/Facette/Builders/ProjectionBuilder.cs tests/Facette.Tests/ProjectionTests.cs
git commit -m "feat: add Projection expression generation"
```

---

### Task 7: Companion Mapper Class

**Files:**
- Create: `src/Facette/Builders/MapperClassBuilder.cs`
- Create: `tests/Facette.Tests/MapperClassTests.cs`

**Step 1: Write the failing test**

Create `tests/Facette.Tests/MapperClassTests.cs`:

```csharp
using Facette.Tests.Helpers;

namespace Facette.Tests;

public class MapperClassTests
{
    [Fact]
    public void Generator_GeneratesMapperClass()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Customer))]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomerDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("public static partial class CustomerDtoMapper", mapperCode);
        Assert.Contains("public static CustomerDto ToDto(this", mapperCode);
        Assert.Contains("CustomerDto.FromSource(source)", mapperCode);
    }

    [Fact]
    public void Generator_MapperClass_IncludesToSourceExtension()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Customer))]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomerDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("public static global::TestApp.Customer ToSource(this CustomerDto dto)", mapperCode);
    }

    [Fact]
    public void Generator_MapperClass_IncludesQueryableProjection()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Customer))]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomerDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("IQueryable<CustomerDto> ProjectToDto", mapperCode);
        Assert.Contains("query.Select(CustomerDto.Projection)", mapperCode);
    }

    [Fact]
    public void Generator_WithGenerateMapperFalse_OmitsMapperClass()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
            }

            [Facette(typeof(Customer), GenerateMapper = false)]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var hasMapper = result.GeneratedTrees
            .Any(t => t.FilePath.Contains("Mapper.g.cs"));

        Assert.False(hasMapper);
    }
}
```

**Step 2: Run tests to verify they fail**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~MapperClassTests" -v minimal
```

Expected: FAIL — no mapper class generated.

**Step 3: Create MapperClassBuilder**

Create `src/Facette/Builders/MapperClassBuilder.cs`:

```csharp
using System.Text;
using Facette.Generator.Models;

namespace Facette.Generator.Builders;

public static class MapperClassBuilder
{
    public static string Build(FacetteTargetModel model)
    {
        var sb = new StringBuilder();

        var nsOpen = string.IsNullOrEmpty(model.Namespace) ? "" : $"namespace {model.Namespace};\n\n";

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.Append(nsOpen);
        sb.AppendLine($"public static partial class {model.TypeName}Mapper");
        sb.AppendLine("{");

        // ToDto extension
        sb.AppendLine($"    public static {model.TypeName} ToDto(this {model.SourceTypeFullName} source)");
        sb.AppendLine($"        => {model.TypeName}.FromSource(source);");

        // ToSource extension
        if (model.GenerateToSource)
        {
            sb.AppendLine();
            sb.AppendLine($"    public static {model.SourceTypeFullName} ToSource(this {model.TypeName} dto)");
            sb.AppendLine("        => dto.ToSource();");
        }

        // IQueryable projection
        if (model.GenerateProjection)
        {
            sb.AppendLine();
            sb.AppendLine($"    public static System.Linq.IQueryable<{model.TypeName}> ProjectToDto(this System.Linq.IQueryable<{model.SourceTypeFullName}> query)");
            sb.AppendLine($"        => query.Select({model.TypeName}.Projection);");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }
}
```

**Step 4: Update FacetteGenerator to emit mapper class**

Add to `FacetteGenerator.Initialize`, inside `RegisterSourceOutput`, after `EmitDtoSource`:

```csharp
        context.RegisterSourceOutput(targets, static (spc, model) =>
        {
            if (model is null) return;
            EmitDtoSource(spc, model);

            if (model.GenerateMapper)
            {
                var mapperCode = MapperClassBuilder.Build(model);
                spc.AddSource($"{model.TypeName}Mapper.g.cs", mapperCode);
            }
        });
```

**Step 5: Run tests to verify they pass**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~MapperClassTests" -v minimal
```

Expected: All 4 tests PASS.

**Step 6: Run all tests**

```bash
dotnet test tests/Facette.Tests -v minimal
```

Expected: All tests PASS (attribute tests + generator tests + model tests + mapping tests + projection tests + mapper class tests).

**Step 7: Commit**

```bash
git add src/Facette/Builders/MapperClassBuilder.cs tests/Facette.Tests/MapperClassTests.cs
git commit -m "feat: add companion mapper class with extension methods"
```

---

### Task 8: Diagnostics

**Files:**
- Create: `src/Facette/Diagnostics/DiagnosticDescriptors.cs`
- Create: `tests/Facette.Tests/DiagnosticTests.cs`

**Step 1: Write the failing tests**

Create `tests/Facette.Tests/DiagnosticTests.cs`:

```csharp
using Facette.Tests.Helpers;

namespace Facette.Tests;

public class DiagnosticTests
{
    [Fact]
    public void Diagnostic_FCT002_IncludeAndExcludeBothSpecified()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Password { get; set; }
            }

            [Facette(typeof(User), "Password", Include = new[] { "Id" })]
            public partial record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT002");
    }
}
```

**Step 2: Run test to verify it fails**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~DiagnosticTests" -v minimal
```

Expected: FAIL — no FCT002 diagnostic emitted.

**Step 3: Create DiagnosticDescriptors**

Create `src/Facette/Diagnostics/DiagnosticDescriptors.cs`:

```csharp
using Microsoft.CodeAnalysis;

namespace Facette.Generator.Diagnostics;

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor FCT001_TypeMustBePartial = new(
        id: "FCT001",
        title: "Type must be partial",
        messageFormat: "Type '{0}' must be declared as partial to use the [Facette] attribute",
        category: "Facette",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FCT002_IncludeExcludeConflict = new(
        id: "FCT002",
        title: "Include and Exclude cannot both be specified",
        messageFormat: "Type '{0}' specifies both Include and Exclude, which is not allowed",
        category: "Facette",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FCT003_SourceTypeNotFound = new(
        id: "FCT003",
        title: "Source type not found",
        messageFormat: "Source type specified in [Facette] on '{0}' could not be found or is inaccessible",
        category: "Facette",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FCT004_PropertyNotFound = new(
        id: "FCT004",
        title: "Property not found on source type",
        messageFormat: "Property '{0}' specified in Include/Exclude was not found on source type '{1}'",
        category: "Facette",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
```

**Step 4: Add diagnostic emission to ModelBuilder and FacetteGenerator**

Update `ModelBuilder.Build` to return diagnostics alongside the model. The simplest approach: have the generator itself check for conflicts after building the model.

Update `FacetteGenerator.cs` — change `RegisterSourceOutput` to validate and emit diagnostics:

```csharp
using Facette.Generator.Builders;
using Facette.Generator.Diagnostics;
using Facette.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Facette.Generator;

[Generator]
public sealed class FacetteGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Facette.Abstractions.FacetteAttribute",
            predicate: static (node, _) => true,
            transform: static (ctx, _) => (Context: ctx, Model: ModelBuilder.Build(ctx))
        );

        context.RegisterSourceOutput(targets, static (spc, pair) =>
        {
            var (ctx, model) = pair;
            var targetSymbol = ctx.TargetSymbol as INamedTypeSymbol;
            if (targetSymbol is null) return;

            // FCT001: Check partial
            var syntaxNode = ctx.TargetNode;
            if (syntaxNode is TypeDeclarationSyntax typeDecl &&
                !typeDecl.Modifiers.Any(m => m.Text == "partial"))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FCT001_TypeMustBePartial,
                    syntaxNode.GetLocation(),
                    targetSymbol.Name));
                return;
            }

            // FCT002: Check include/exclude conflict
            var attribute = ctx.Attributes
                .FirstOrDefault(a => a.AttributeClass?.Name == "FacetteAttribute");
            if (attribute is not null)
            {
                var hasExclude = attribute.ConstructorArguments.Length > 1 &&
                    attribute.ConstructorArguments[1].Kind == TypedConstantKind.Array &&
                    attribute.ConstructorArguments[1].Values.Length > 0;

                var hasInclude = attribute.NamedArguments
                    .Any(a => a.Key == "Include" &&
                         a.Value.Kind == TypedConstantKind.Array &&
                         a.Value.Values.Length > 0);

                if (hasExclude && hasInclude)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.FCT002_IncludeExcludeConflict,
                        syntaxNode.GetLocation(),
                        targetSymbol.Name));
                    return;
                }
            }

            if (model is null) return;

            EmitDtoSource(spc, model);

            if (model.GenerateMapper)
            {
                var mapperCode = MapperClassBuilder.Build(model);
                spc.AddSource($"{model.TypeName}Mapper.g.cs", mapperCode);
            }
        });
    }

    private static void EmitDtoSource(SourceProductionContext spc, FacetteTargetModel model)
    {
        var properties = PropertyBuilder.Build(model.Properties);
        var fromSource = MappingBuilder.BuildFromSource(model.TypeName, model.SourceTypeFullName, model.Properties);

        var toSource = model.GenerateToSource
            ? MappingBuilder.BuildToSource(model.SourceTypeFullName, model.Properties)
            : "";

        var projection = model.GenerateProjection
            ? ProjectionBuilder.Build(model.TypeName, model.SourceTypeFullName, model.Properties)
            : "";

        var nsOpen = string.IsNullOrEmpty(model.Namespace) ? "" : $"namespace {model.Namespace};\n\n";

        var code = $$"""
            // <auto-generated />
            #nullable enable

            {{nsOpen}}public partial record {{model.TypeName}}
            {
            {{properties}}

            {{fromSource}}
            {{toSource}}
            {{projection}}
            }
            """;

        spc.AddSource($"{model.TypeName}.g.cs", code);
    }
}
```

Note: The `transform` lambda now returns a tuple with the original context alongside the model. This is needed because diagnostic emission requires the syntax node location and attribute data.

**Important**: Returning `GeneratorAttributeSyntaxContext` from the transform is not ideal for incremental caching (it's not equatable). For v1 this is acceptable. A future optimization would extract only the needed location info into the model.

**Step 5: Run tests to verify they pass**

```bash
dotnet test tests/Facette.Tests --filter "FullyQualifiedName~DiagnosticTests" -v minimal
```

Expected: PASS.

**Step 6: Run all tests**

```bash
dotnet test tests/Facette.Tests -v minimal
```

Expected: All tests PASS.

**Step 7: Commit**

```bash
git add src/Facette/Diagnostics/ tests/Facette.Tests/DiagnosticTests.cs
git commit -m "feat: add FCT001-FCT004 diagnostics for invalid usage"
```

---

### Task 9: Integration Tests

**Files:**
- Create: `tests/Facette.IntegrationTests/Models/User.cs`
- Create: `tests/Facette.IntegrationTests/Dtos/UserDto.cs`
- Create: `tests/Facette.IntegrationTests/MappingIntegrationTests.cs`

**Step 1: Create the domain entity**

Create `tests/Facette.IntegrationTests/Models/User.cs`:

```csharp
namespace Facette.IntegrationTests.Models;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
```

**Step 2: Create the DTO declaration**

Create `tests/Facette.IntegrationTests/Dtos/UserDto.cs`:

```csharp
using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(User), "PasswordHash")]
public partial record UserDto;
```

**Step 3: Write integration tests**

Create `tests/Facette.IntegrationTests/MappingIntegrationTests.cs`:

```csharp
using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests;

public class MappingIntegrationTests
{
    [Fact]
    public void FromSource_MapsAllIncludedProperties()
    {
        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            PasswordHash = "secret_hash",
            CreatedAt = new DateTime(2024, 1, 15)
        };

        var dto = UserDto.FromSource(user);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);
        Assert.Equal("alice@example.com", dto.Email);
        Assert.Equal(new DateTime(2024, 1, 15), dto.CreatedAt);
    }

    [Fact]
    public void ToSource_MapsBack()
    {
        var dto = new UserDto
        {
            Id = 2,
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob@example.com",
            CreatedAt = new DateTime(2024, 6, 1)
        };

        var user = dto.ToSource();

        Assert.Equal(2, user.Id);
        Assert.Equal("Bob", user.FirstName);
        Assert.Equal("Jones", user.LastName);
        Assert.Equal("bob@example.com", user.Email);
    }

    [Fact]
    public void ExtensionMethod_ToDto_Works()
    {
        var user = new User
        {
            Id = 3,
            FirstName = "Carol",
            LastName = "Lee",
            Email = "carol@example.com",
            PasswordHash = "hash123",
            CreatedAt = DateTime.UtcNow
        };

        var dto = user.ToDto();

        Assert.Equal(3, dto.Id);
        Assert.Equal("Carol", dto.FirstName);
    }

    [Fact]
    public void Projection_CanBeCompiled()
    {
        var projection = UserDto.Projection;
        var compiled = projection.Compile();

        var user = new User
        {
            Id = 4,
            FirstName = "Dave",
            LastName = "Kim",
            Email = "dave@example.com",
            PasswordHash = "hash456",
            CreatedAt = DateTime.UtcNow
        };

        var dto = compiled(user);

        Assert.Equal(4, dto.Id);
        Assert.Equal("Dave", dto.FirstName);
    }

    [Fact]
    public void ProjectToDto_WorksWithQueryable()
    {
        var users = new List<User>
        {
            new() { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", PasswordHash = "h", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, FirstName = "C", LastName = "D", Email = "c@d.com", PasswordHash = "h", CreatedAt = DateTime.UtcNow }
        };

        var dtos = users.AsQueryable().ProjectToDto().ToList();

        Assert.Equal(2, dtos.Count);
        Assert.Equal("A", dtos[0].FirstName);
        Assert.Equal("C", dtos[1].FirstName);
    }

    [Fact]
    public void ExcludedProperty_NotOnDto()
    {
        // PasswordHash should not be a property on UserDto
        var props = typeof(UserDto).GetProperties();
        Assert.DoesNotContain(props, p => p.Name == "PasswordHash");
    }
}
```

**Step 4: Build and run integration tests**

```bash
dotnet test tests/Facette.IntegrationTests -v minimal
```

Expected: All 6 tests PASS. If they fail, debug and fix the generator output — integration tests are the ultimate correctness check.

**Step 5: Commit**

```bash
git add tests/Facette.IntegrationTests/
git commit -m "test: add integration tests for end-to-end mapping"
```

---

### Task 10: Sample Project

**Files:**
- Create: `samples/Facette.Sample/Facette.Sample.csproj`
- Create: `samples/Facette.Sample/Program.cs`
- Create: `samples/Facette.Sample/Models/Product.cs`
- Create: `samples/Facette.Sample/Dtos/ProductDto.cs`

**Step 1: Create sample project**

Create `samples/Facette.Sample/Facette.Sample.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Facette.Abstractions\Facette.Abstractions.csproj" />
    <ProjectReference Include="..\..\src\Facette\Facette.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  </ItemGroup>
</Project>
```

**Step 2: Create sample models and DTOs**

Create `samples/Facette.Sample/Models/Product.cs`:

```csharp
namespace Facette.Sample.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string InternalSku { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
```

Create `samples/Facette.Sample/Dtos/ProductDto.cs`:

```csharp
using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

[Facette(typeof(Product), "InternalSku")]
public partial record ProductDto;
```

Create `samples/Facette.Sample/Program.cs`:

```csharp
using Facette.Sample.Dtos;
using Facette.Sample.Models;

var product = new Product
{
    Id = 1,
    Name = "Widget",
    Description = "A fine widget",
    Price = 9.99m,
    InternalSku = "WDG-001",
    CreatedAt = DateTime.UtcNow
};

// Using inline method
var dto = ProductDto.FromSource(product);
Console.WriteLine($"DTO: {dto.Id} - {dto.Name} - {dto.Price:C}");

// Using extension method
var dto2 = product.ToDto();
Console.WriteLine($"Extension: {dto2.Id} - {dto2.Name}");

// Round-trip
var backToProduct = dto.ToSource();
Console.WriteLine($"Round-trip: {backToProduct.Id} - {backToProduct.Name} - {backToProduct.Price:C}");

// Queryable projection
var products = new List<Product> { product }.AsQueryable();
var projected = products.ProjectToDto().ToList();
Console.WriteLine($"Projected: {projected.Count} items");

Console.WriteLine("\nFacette is working!");
```

**Step 3: Add to solution and build**

```bash
dotnet sln add samples/Facette.Sample/Facette.Sample.csproj
dotnet run --project samples/Facette.Sample
```

Expected output:
```
DTO: 1 - Widget - $9.99
Extension: 1 - Widget
Round-trip: 1 - Widget - $9.99
Projected: 1 items

Facette is working!
```

**Step 4: Commit**

```bash
git add samples/ Facette.sln
git commit -m "feat: add sample project demonstrating Facette usage"
```

---

### Task 11: Final Cleanup and Verification

**Step 1: Run full test suite**

```bash
dotnet test -v minimal
```

Expected: All tests PASS across both test projects.

**Step 2: Run sample**

```bash
dotnet run --project samples/Facette.Sample
```

Expected: Runs successfully with correct output.

**Step 3: Verify solution builds clean**

```bash
dotnet build -c Release --no-incremental 2>&1
```

Expected: No errors, no warnings (except suppressed ones).

**Step 4: Commit any final fixes**

If any fixes were needed, commit them:

```bash
git add -A
git commit -m "chore: final cleanup and verification"
```
