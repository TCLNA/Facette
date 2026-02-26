# Facette v2 Tier 1 Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add custom property mapping (`[MapFrom]`), nested object mapping (auto-detect), and collection mapping (`List<T>`, `T[]`) to Facette.

**Architecture:** Enrich `PropertyModel` with a `MappingKind` enum and additional metadata fields. `ModelBuilder` detects user-declared properties, discovers nested Facette DTOs via compilation scan, and identifies collection types. Each builder (`PropertyBuilder`, `MappingBuilder`, `ProjectionBuilder`, `MapperClassBuilder`) switches on `MappingKind` to emit the appropriate code.

**Tech Stack:** C# / Roslyn IIncrementalGenerator / netstandard2.0 (generator) / net10.0 (tests)

**Important constraints:**
- Generator code targets **netstandard2.0** — no records, no raw string literals, no collection expressions, no `init`, no `is not`, no file-scoped namespaces.
- All model types must implement `IEquatable<T>` for incremental generator caching.
- Use fully qualified type names (`global::` prefix via `SymbolDisplayFormat.FullyQualifiedFormat`) in all generated code.

---

### Task 1: Add `MapFromAttribute` to Facette.Abstractions

**Files:**
- Create: `src/Facette.Abstractions/MapFromAttribute.cs`
- Test: `tests/Facette.Tests/MapFromAttributeTests.cs`

**Step 1: Write the failing test**

```csharp
// tests/Facette.Tests/MapFromAttributeTests.cs
using Facette.Abstractions;

namespace Facette.Tests;

public class MapFromAttributeTests
{
    [Fact]
    public void MapFromAttribute_StoresSourcePropertyName()
    {
        var attr = new MapFromAttribute("FirstName");
        Assert.Equal("FirstName", attr.SourcePropertyName);
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/Facette.Tests --filter "MapFromAttribute_StoresSourcePropertyName" --verbosity quiet`
Expected: Build error — `MapFromAttribute` does not exist.

**Step 3: Write minimal implementation**

```csharp
// src/Facette.Abstractions/MapFromAttribute.cs
namespace Facette.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class MapFromAttribute : Attribute
{
    public MapFromAttribute(string sourcePropertyName)
    {
        SourcePropertyName = sourcePropertyName;
    }

    public string SourcePropertyName { get; }
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/Facette.Tests --filter "MapFromAttribute_StoresSourcePropertyName" --verbosity quiet`
Expected: PASS

**Step 5: Commit**

```bash
git add src/Facette.Abstractions/MapFromAttribute.cs tests/Facette.Tests/MapFromAttributeTests.cs
git commit -m "feat: add MapFromAttribute to Facette.Abstractions"
```

---

### Task 2: Enrich PropertyModel with MappingKind and new fields

**Files:**
- Modify: `src/Facette/Models/FacetteTargetModel.cs` (the `PropertyModel` class, lines 134-171)

**Step 1: Add `MappingKind` enum and update `PropertyModel`**

Add the `MappingKind` enum inside the `Facette.Generator.Models` namespace (same file, before `PropertyModel`):

```csharp
public enum MappingKind
{
    Direct,
    Custom,
    Nested,
    Collection
}
```

Update `PropertyModel` to add new fields. The constructor changes to:

```csharp
public PropertyModel(
    string name,
    string typeFullName,
    bool isValueType,
    MappingKind mappingKind,
    string sourcePropertyName,
    string nestedDtoTypeName,
    string nestedDtoTypeFullName,
    string collectionElementTypeFullName,
    bool isNullable,
    bool isArray)
{
    Name = name;
    TypeFullName = typeFullName;
    IsValueType = isValueType;
    MappingKind = mappingKind;
    SourcePropertyName = sourcePropertyName;
    NestedDtoTypeName = nestedDtoTypeName;
    NestedDtoTypeFullName = nestedDtoTypeFullName;
    CollectionElementTypeFullName = collectionElementTypeFullName;
    IsNullable = isNullable;
    IsArray = isArray;
}
```

New properties:

```csharp
public MappingKind MappingKind { get; }
public string SourcePropertyName { get; }
public string NestedDtoTypeName { get; }
public string NestedDtoTypeFullName { get; }
public string CollectionElementTypeFullName { get; }
public bool IsNullable { get; }
public bool IsArray { get; }
```

Update `Equals`:

```csharp
public bool Equals(PropertyModel other)
{
    if (other == null) return false;
    return Name == other.Name
        && TypeFullName == other.TypeFullName
        && IsValueType == other.IsValueType
        && MappingKind == other.MappingKind
        && SourcePropertyName == other.SourcePropertyName
        && NestedDtoTypeName == other.NestedDtoTypeName
        && NestedDtoTypeFullName == other.NestedDtoTypeFullName
        && CollectionElementTypeFullName == other.CollectionElementTypeFullName
        && IsNullable == other.IsNullable
        && IsArray == other.IsArray;
}
```

Update `GetHashCode` to include all new fields.

**Step 2: Add a static factory for backward-compatible Direct mappings**

Add a static factory method so existing call sites don't break:

```csharp
public static PropertyModel Direct(string name, string typeFullName, bool isValueType)
{
    return new PropertyModel(
        name, typeFullName, isValueType,
        MappingKind.Direct, name, "", "", "", false, false);
}
```

**Step 3: Update all existing call sites**

In `ModelBuilder.cs`, replace the existing `new PropertyModel(prop.Name, typeDisplay, isValueType)` with `PropertyModel.Direct(prop.Name, typeDisplay, isValueType)`.

**Step 4: Build and run all tests**

Run: `dotnet test --verbosity quiet`
Expected: All 29 existing tests pass. Zero warnings.

**Step 5: Commit**

```bash
git add src/Facette/Models/FacetteTargetModel.cs src/Facette/Builders/ModelBuilder.cs
git commit -m "feat: enrich PropertyModel with MappingKind and mapping metadata"
```

---

### Task 3: Add FCT005 diagnostic descriptor

**Files:**
- Modify: `src/Facette/Diagnostics/DiagnosticDescriptors.cs`

**Step 1: Add FCT005 descriptor**

Add after `FCT004_PropertyNotFound`:

```csharp
public static readonly DiagnosticDescriptor FCT005_MapFromPropertyNotFound = new DiagnosticDescriptor(
    id: "FCT005",
    title: "MapFrom source property not found",
    messageFormat: "[MapFrom] on '{0}' references property '{1}' which was not found on source type '{2}'",
    category: "Facette",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true);
```

**Step 2: Build**

Run: `dotnet build src/Facette --verbosity quiet`
Expected: Build succeeds.

**Step 3: Commit**

```bash
git add src/Facette/Diagnostics/DiagnosticDescriptors.cs
git commit -m "feat: add FCT005 diagnostic for invalid MapFrom references"
```

---

### Task 4: Implement custom property mapping ([MapFrom]) in ModelBuilder

**Files:**
- Modify: `src/Facette/Builders/ModelBuilder.cs`
- Test: `tests/Facette.Tests/CustomMappingTests.cs`

**Step 1: Write the failing tests**

Create `tests/Facette.Tests/CustomMappingTests.cs`:

```csharp
using Facette.Tests.Helpers;

namespace Facette.Tests;

public class CustomMappingTests
{
    [Fact]
    public void Generator_WithMapFrom_MapsToSourceProperty()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("FirstName")]
                public string DisplayName { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should map DisplayName from FirstName
        Assert.Contains("DisplayName = source.FirstName", generatedCode);
        // Should NOT re-generate DisplayName as a property (user declared it)
        Assert.DoesNotContain("public string DisplayName { get; init; }", generatedCode);
        // Should still generate other properties
        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string LastName { get; init; }", generatedCode);
    }

    [Fact]
    public void Generator_WithMapFrom_GeneratesToSourceMapping()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string FirstName { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("FirstName")]
                public string DisplayName { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // ToSource should map back: FirstName = this.DisplayName
        Assert.Contains("FirstName = this.DisplayName", generatedCode);
    }

    [Fact]
    public void Generator_UserDeclaredPropertyWithoutMapFrom_IsSkipped()
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
            public partial record UserDto
            {
                public string ComputedField { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should NOT contain ComputedField in mappings
        Assert.DoesNotContain("ComputedField", generatedCode);
        // Should still generate source properties
        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string Name { get; init; }", generatedCode);
    }

    [Fact]
    public void Diagnostic_FCT005_MapFromPropertyNotFound()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("NonExistent")]
                public string DisplayName { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT005");
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Facette.Tests --filter "CustomMappingTests" --verbosity quiet`
Expected: FAIL (generator doesn't know about `[MapFrom]` yet).

**Step 3: Implement ModelBuilder changes**

In `ModelBuilder.Build`, after the existing `GetSourceProperties` call (line ~146), add logic to:

1. **Collect user-declared property names** from `targetSymbol`:
   ```csharp
   var userDeclaredProps = new HashSet<string>();
   var customMappings = new List<PropertyModel>();
   foreach (var member in targetSymbol.GetMembers())
   {
       if (!(member is IPropertySymbol userProp)) continue;
       if (userProp.DeclaredAccessibility != Accessibility.Public) continue;
       userDeclaredProps.Add(userProp.Name);

       // Check for [MapFrom] attribute
       foreach (var attr in userProp.GetAttributes())
       {
           if (attr.AttributeClass != null
               && attr.AttributeClass.ToDisplayString() == "Facette.Abstractions.MapFromAttribute"
               && attr.ConstructorArguments.Length > 0
               && attr.ConstructorArguments[0].Value is string sourcePropName)
           {
               // Validate the source property exists
               if (!sourcePropertyNames.Contains(sourcePropName))
               {
                   diagnosticsBuilder.Add(new DiagnosticInfo(
                       DiagnosticDescriptors.FCT005_MapFromPropertyNotFound,
                       filePath, textSpan, lineSpan,
                       new object[] { userProp.Name, sourcePropName, sourceType.Name }));
                   continue;
               }

               // Find the source property to get its type info
               var sourceProp = FindSourceProperty(sourceType, sourcePropName);
               if (sourceProp != null)
               {
                   customMappings.Add(new PropertyModel(
                       userProp.Name,
                       userProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                       userProp.Type.IsValueType,
                       MappingKind.Custom,
                       sourcePropName,
                       "", "", "", false, false));
               }
           }
       }
   }
   ```

2. **Filter out user-declared names from auto-generated properties**, then merge with custom mappings:
   ```csharp
   // Remove user-declared properties from the auto-generated list
   var filteredProperties = properties.Where(p => !userDeclaredProps.Contains(p.Name)).ToList();
   // Add custom mappings
   filteredProperties.AddRange(customMappings);
   var finalProperties = ImmutableArray.CreateRange(filteredProperties);
   ```

3. Add a `FindSourceProperty` helper that walks the type hierarchy (similar to `GetSourcePropertyNames` but returns the `IPropertySymbol`).

4. Use `finalProperties` instead of `properties` when constructing `FacetteTargetModel`.

**Step 4: Run tests to verify they pass**

Run: `dotnet test tests/Facette.Tests --verbosity quiet`
Expected: All tests pass (existing + new).

**Step 5: Commit**

```bash
git add src/Facette/Builders/ModelBuilder.cs tests/Facette.Tests/CustomMappingTests.cs
git commit -m "feat: implement custom property mapping with [MapFrom]"
```

---

### Task 5: Update builders for Custom mapping kind

**Files:**
- Modify: `src/Facette/Builders/MappingBuilder.cs`
- Modify: `src/Facette/Builders/ProjectionBuilder.cs`

The `PropertyBuilder` does NOT need changes — custom-mapped properties are user-declared and not generated.

**Step 1: Update MappingBuilder**

In `BuildFromSource`, change the property assignment line from:
```csharp
sb.AppendLine("            " + prop.Name + " = source." + prop.Name + comma);
```
to:
```csharp
var sourceName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
sb.AppendLine("            " + prop.Name + " = source." + sourceName + comma);
```

In `BuildToSource`, the reverse — use `SourcePropertyName` as the target and `Name` as the source:
```csharp
var targetName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
sb.AppendLine("            " + targetName + " = this." + prop.Name + comma);
```

**Step 2: Update ProjectionBuilder**

Same change as `BuildFromSource`:
```csharp
var sourceName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
sb.AppendLine("            " + prop.Name + " = source." + sourceName + comma);
```

**Step 3: Run tests to verify**

Run: `dotnet test tests/Facette.Tests --verbosity quiet`
Expected: All tests pass.

**Step 4: Commit**

```bash
git add src/Facette/Builders/MappingBuilder.cs src/Facette/Builders/ProjectionBuilder.cs
git commit -m "feat: update mapping and projection builders for Custom mapping kind"
```

---

### Task 6: Implement nested DTO discovery in ModelBuilder

**Files:**
- Modify: `src/Facette/Builders/ModelBuilder.cs`
- Test: `tests/Facette.Tests/NestedMappingTests.cs`

**Step 1: Write the failing tests**

Create `tests/Facette.Tests/NestedMappingTests.cs`:

```csharp
using Facette.Tests.Helpers;

namespace Facette.Tests;

public class NestedMappingTests
{
    [Fact]
    public void Generator_WithNestedDto_GeneratesNestedProperty()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            public class User
            {
                public int Id { get; set; }
                public Address HomeAddress { get; set; }
            }

            [Facette(typeof(Address))]
            public partial record AddressDto;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should generate AddressDto property instead of Address
        Assert.Contains("AddressDto", generatedCode);
        // Should map via FromSource
        Assert.Contains("AddressDto.FromSource(source.HomeAddress)", generatedCode);
    }

    [Fact]
    public void Generator_WithNullableNestedDto_GeneratesNullCheck()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            public class User
            {
                public int Id { get; set; }
                public Address? HomeAddress { get; set; }
            }

            [Facette(typeof(Address))]
            public partial record AddressDto;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should generate nullable AddressDto? property
        Assert.Contains("AddressDto?", generatedCode);
        // Should include null check in mapping
        Assert.Contains("source.HomeAddress != null", generatedCode);
    }

    [Fact]
    public void Generator_WithNestedDto_InlinesProjection()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            public class User
            {
                public int Id { get; set; }
                public Address HomeAddress { get; set; }
            }

            [Facette(typeof(Address))]
            public partial record AddressDto;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Projection should inline the nested object initializer, not call FromSource
        Assert.Contains("new", generatedCode);
        Assert.Contains("source.HomeAddress.Street", generatedCode);
        Assert.Contains("source.HomeAddress.City", generatedCode);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Facette.Tests --filter "NestedMappingTests" --verbosity quiet`
Expected: FAIL

**Step 3: Implement nested DTO discovery**

In `ModelBuilder.Build`, add a method to scan the compilation for all Facette DTOs and build a lookup:

```csharp
private static Dictionary<string, (string DtoTypeName, string DtoTypeFullName, ImmutableArray<PropertyModel> Properties)> BuildFacetteLookup(
    Compilation compilation,
    INamedTypeSymbol currentSourceType)
{
    var lookup = new Dictionary<string, (string, string, ImmutableArray<PropertyModel>)>();
    var facetteAttrName = "Facette.Abstractions.FacetteAttribute";

    foreach (var tree in compilation.SyntaxTrees)
    {
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var symbol = model.GetDeclaredSymbol(typeDecl) as INamedTypeSymbol;
            if (symbol == null) continue;

            foreach (var attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass != null
                    && attr.AttributeClass.ToDisplayString() == facetteAttrName
                    && attr.ConstructorArguments.Length > 0
                    && attr.ConstructorArguments[0].Value is INamedTypeSymbol nestedSourceType)
                {
                    var sourceKey = nestedSourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    // Don't include the current target (avoid self-reference)
                    if (!SymbolEqualityComparer.Default.Equals(nestedSourceType, currentSourceType))
                    {
                        var nestedProps = GetSourceProperties(nestedSourceType, new HashSet<string>(), null);
                        lookup[sourceKey] = (
                            symbol.Name,
                            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            nestedProps);
                    }
                }
            }
        }
    }

    return lookup;
}
```

Note: This method needs `context.SemanticModel.Compilation` passed from `Build`. Add it as a parameter.

Then in `GetSourceProperties`, after determining a property is public and readable, check if its type matches a key in the Facette lookup:

```csharp
var sourceTypeKey = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
// Strip nullable wrapper for lookup
var unwrappedType = prop.Type;
bool isNullable = false;
if (prop.Type is INamedTypeSymbol namedType
    && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
{
    unwrappedType = namedType.TypeArguments[0];
    isNullable = true;
}
else if (prop.NullableAnnotation == NullableAnnotation.Annotated)
{
    isNullable = true;
}

var lookupKey = unwrappedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

if (facetteLookup.ContainsKey(lookupKey))
{
    var dto = facetteLookup[lookupKey];
    builder.Add(new PropertyModel(
        prop.Name,
        isNullable ? dto.DtoTypeFullName + "?" : dto.DtoTypeFullName,
        false,  // DTOs (records) are reference types
        MappingKind.Nested,
        prop.Name,
        dto.DtoTypeName,
        dto.DtoTypeFullName,
        "",
        isNullable,
        false));
}
else
{
    builder.Add(PropertyModel.Direct(prop.Name, typeDisplay, isValueType));
}
```

The Facette lookup also needs to be passed through to `GetSourceProperties`.

**Step 4: Run tests**

Run: `dotnet test tests/Facette.Tests --verbosity quiet`
Expected: All tests pass.

**Step 5: Commit**

```bash
git add src/Facette/Builders/ModelBuilder.cs tests/Facette.Tests/NestedMappingTests.cs
git commit -m "feat: implement nested DTO discovery and property mapping"
```

---

### Task 7: Update builders for Nested mapping kind

**Files:**
- Modify: `src/Facette/Builders/PropertyBuilder.cs`
- Modify: `src/Facette/Builders/MappingBuilder.cs`
- Modify: `src/Facette/Builders/ProjectionBuilder.cs`

**Step 1: Update PropertyBuilder**

Nested properties use the DTO type (already set in `TypeFullName`). The `= default!` logic applies — nested DTOs are reference types. No change needed if the existing logic handles it.

However, nullable nested properties need `= default!` suppressed (they're already nullable). Check the existing logic:
```csharp
var defaultValue = prop.IsValueType ? "" : " = default!;";
```
For nullable reference types (nested DTO with `IsNullable = true`), we don't want `= default!`. Update to:
```csharp
var defaultValue = prop.IsValueType || prop.IsNullable ? "" : " = default!;";
```

**Step 2: Update MappingBuilder.BuildFromSource**

Replace the simple assignment with a switch on `MappingKind`:

```csharp
string assignment;
var sourceName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;

switch (prop.MappingKind)
{
    case MappingKind.Nested:
        if (prop.IsNullable)
        {
            assignment = prop.Name + " = source." + sourceName + " != null ? "
                + prop.NestedDtoTypeFullName + ".FromSource(source." + sourceName + ") : null";
        }
        else
        {
            assignment = prop.Name + " = " + prop.NestedDtoTypeFullName + ".FromSource(source." + sourceName + ")";
        }
        break;
    default:
        assignment = prop.Name + " = source." + sourceName;
        break;
}
sb.AppendLine("            " + assignment + comma);
```

**Step 3: Update MappingBuilder.BuildToSource**

Similar logic for `ToSource`:

```csharp
string assignment;
var targetName = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;

switch (prop.MappingKind)
{
    case MappingKind.Nested:
        if (prop.IsNullable)
        {
            assignment = targetName + " = this." + prop.Name + " != null ? this." + prop.Name + ".ToSource() : null";
        }
        else
        {
            assignment = targetName + " = this." + prop.Name + ".ToSource()";
        }
        break;
    default:
        assignment = targetName + " = this." + prop.Name;
        break;
}
sb.AppendLine("            " + assignment + comma);
```

**Step 4: Update ProjectionBuilder**

For nested mapping in projections, inline the nested DTO's properties as an object initializer. This requires the `PropertyModel` to carry the nested DTO's properties. The Facette lookup already stores them.

Add a `NestedProperties` field to `PropertyModel`:

```csharp
public ImmutableArray<PropertyModel> NestedProperties { get; }
```

(Add to constructor, Equals, GetHashCode. For non-nested kinds, pass `ImmutableArray<PropertyModel>.Empty`.)

Then in `ProjectionBuilder.Build`, when emitting a nested property:

```csharp
switch (prop.MappingKind)
{
    case MappingKind.Nested:
        var sourceName = prop.SourcePropertyName;
        if (prop.IsNullable)
        {
            sb.AppendLine("            " + prop.Name + " = source." + sourceName + " != null ? new " + prop.NestedDtoTypeFullName);
        }
        else
        {
            sb.AppendLine("            " + prop.Name + " = new " + prop.NestedDtoTypeFullName);
        }
        sb.AppendLine("            {");
        for (int j = 0; j < prop.NestedProperties.Length; j++)
        {
            var np = prop.NestedProperties[j];
            var nComma = j < prop.NestedProperties.Length - 1 ? "," : "";
            var nSourceName = np.MappingKind == MappingKind.Custom ? np.SourcePropertyName : np.Name;
            sb.AppendLine("                " + np.Name + " = source." + sourceName + "." + nSourceName + nComma);
        }
        if (prop.IsNullable)
        {
            sb.AppendLine("            } : null" + comma);
        }
        else
        {
            sb.AppendLine("            }" + comma);
        }
        break;
    default:
        var sn = prop.MappingKind == MappingKind.Custom ? prop.SourcePropertyName : prop.Name;
        sb.AppendLine("            " + prop.Name + " = source." + sn + comma);
        break;
}
```

**Step 5: Run tests**

Run: `dotnet test tests/Facette.Tests --verbosity quiet`
Expected: All tests pass (including nested mapping tests from Task 6).

**Step 6: Commit**

```bash
git add src/Facette/Models/FacetteTargetModel.cs src/Facette/Builders/PropertyBuilder.cs src/Facette/Builders/MappingBuilder.cs src/Facette/Builders/ProjectionBuilder.cs
git commit -m "feat: update builders for nested mapping kind with inlined projections"
```

---

### Task 8: Implement collection detection in ModelBuilder

**Files:**
- Modify: `src/Facette/Builders/ModelBuilder.cs`
- Test: `tests/Facette.Tests/CollectionMappingTests.cs`

**Step 1: Write the failing tests**

Create `tests/Facette.Tests/CollectionMappingTests.cs`:

```csharp
using Facette.Tests.Helpers;

namespace Facette.Tests;

public class CollectionMappingTests
{
    [Fact]
    public void Generator_WithListOfNestedDto_GeneratesSelectMapping()
    {
        var source = """
            using Facette.Abstractions;
            using System.Collections.Generic;

            namespace TestApp;

            public class OrderItem
            {
                public int ProductId { get; set; }
                public int Quantity { get; set; }
            }

            public class Order
            {
                public int Id { get; set; }
                public List<OrderItem> Items { get; set; }
            }

            [Facette(typeof(OrderItem))]
            public partial record OrderItemDto;

            [Facette(typeof(Order))]
            public partial record OrderDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        // Should map via Select + ToList
        Assert.Contains("OrderItemDto.FromSource(", generatedCode);
        Assert.Contains(".ToList()", generatedCode);
    }

    [Fact]
    public void Generator_WithArrayOfNestedDto_GeneratesToArrayMapping()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Tag
            {
                public string Name { get; set; }
            }

            public class Article
            {
                public int Id { get; set; }
                public Tag[] Tags { get; set; }
            }

            [Facette(typeof(Tag))]
            public partial record TagDto;

            [Facette(typeof(Article))]
            public partial record ArticleDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ArticleDto.g.cs"))
            .GetText().ToString();

        // Should map via Select + ToArray
        Assert.Contains("TagDto.FromSource(", generatedCode);
        Assert.Contains(".ToArray()", generatedCode);
    }

    [Fact]
    public void Generator_WithSimpleCollection_GeneratesDirectCopy()
    {
        var source = """
            using Facette.Abstractions;
            using System.Collections.Generic;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public List<string> Tags { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Simple collection should use ToList()
        Assert.Contains(".ToList()", generatedCode);
        // Should not reference any DTO type for simple elements
        Assert.DoesNotContain("FromSource", generatedCode);
    }

    [Fact]
    public void Generator_WithNullableCollection_GeneratesNullCheck()
    {
        var source = """
            using Facette.Abstractions;
            using System.Collections.Generic;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public List<string>? Tags { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("source.Tags != null", generatedCode);
    }
}
```

**Step 2: Run tests to verify they fail**

Run: `dotnet test tests/Facette.Tests --filter "CollectionMappingTests" --verbosity quiet`
Expected: FAIL

**Step 3: Implement collection detection in ModelBuilder**

In `GetSourceProperties`, after checking the Facette lookup for nested types, add collection detection:

```csharp
// Check if the property is a collection
bool isArray = prop.Type.TypeKind == TypeKind.Array;
bool isCollection = false;
ITypeSymbol elementType = null;

if (isArray)
{
    elementType = ((IArrayTypeSymbol)prop.Type).ElementType;
    isCollection = true;
}
else if (prop.Type.SpecialType != SpecialType.System_String)
{
    // Check for IEnumerable<T>
    foreach (var iface in prop.Type.AllInterfaces)
    {
        if (iface.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>"
            && iface.TypeArguments.Length == 1)
        {
            elementType = iface.TypeArguments[0];
            isCollection = true;
            break;
        }
    }
}

if (isCollection && elementType != null)
{
    var elementTypeKey = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    bool elementHasDto = facetteLookup.ContainsKey(elementTypeKey);

    string dtoTypeName = "";
    string dtoTypeFullName = "";
    var nestedProps = ImmutableArray<PropertyModel>.Empty;
    string dtoElementType;

    if (elementHasDto)
    {
        var dto = facetteLookup[elementTypeKey];
        dtoTypeName = dto.DtoTypeName;
        dtoTypeFullName = dto.DtoTypeFullName;
        nestedProps = dto.Properties;
        dtoElementType = dto.DtoTypeFullName;
    }
    else
    {
        dtoElementType = elementTypeKey;
    }

    // Determine output type
    string outputType;
    if (isArray)
    {
        outputType = dtoElementType + "[]";
    }
    else
    {
        outputType = "System.Collections.Generic.List<" + dtoElementType + ">";
    }

    builder.Add(new PropertyModel(
        prop.Name,
        outputType,
        false,
        MappingKind.Collection,
        prop.Name,
        dtoTypeName,
        dtoTypeFullName,
        elementTypeKey,
        isNullable,
        isArray,
        nestedProps));

    continue; // Skip the Direct/Nested check below
}
```

**Step 4: Run tests**

Run: `dotnet test tests/Facette.Tests --verbosity quiet`
Expected: Tests may still fail (builders not updated yet for Collection kind). That's OK — continue to Task 9.

**Step 5: Commit**

```bash
git add src/Facette/Builders/ModelBuilder.cs tests/Facette.Tests/CollectionMappingTests.cs
git commit -m "feat: implement collection type detection in ModelBuilder"
```

---

### Task 9: Update builders for Collection mapping kind

**Files:**
- Modify: `src/Facette/Builders/MappingBuilder.cs`
- Modify: `src/Facette/Builders/ProjectionBuilder.cs`
- Modify: `src/Facette/Builders/PropertyBuilder.cs`

**Step 1: Update MappingBuilder.BuildFromSource for Collection**

Add a `Collection` case in the switch:

```csharp
case MappingKind.Collection:
    var collSourceName = prop.SourcePropertyName;
    var toMethod = prop.IsArray ? ".ToArray()" : ".ToList()";
    string collExpr;

    if (!string.IsNullOrEmpty(prop.NestedDtoTypeFullName))
    {
        // Collection with nested DTO elements
        collExpr = "source." + collSourceName + ".Select(x => " + prop.NestedDtoTypeFullName + ".FromSource(x))" + toMethod;
    }
    else
    {
        // Simple collection (copy elements)
        collExpr = "source." + collSourceName + toMethod;
    }

    if (prop.IsNullable)
    {
        assignment = prop.Name + " = source." + collSourceName + " != null ? " + collExpr + " : null";
    }
    else
    {
        assignment = prop.Name + " = " + collExpr;
    }
    break;
```

**Step 2: Update MappingBuilder.BuildToSource for Collection**

```csharp
case MappingKind.Collection:
    var collTarget = prop.SourcePropertyName;
    var toMethodR = prop.IsArray ? ".ToArray()" : ".ToList()";
    string collExprR;

    if (!string.IsNullOrEmpty(prop.NestedDtoTypeFullName))
    {
        collExprR = "this." + prop.Name + ".Select(x => x.ToSource())" + toMethodR;
    }
    else
    {
        collExprR = "this." + prop.Name + toMethodR;
    }

    if (prop.IsNullable)
    {
        assignment = collTarget + " = this." + prop.Name + " != null ? " + collExprR + " : null";
    }
    else
    {
        assignment = collTarget + " = " + collExprR;
    }
    break;
```

**Step 3: Update ProjectionBuilder for Collection**

```csharp
case MappingKind.Collection:
    var cSourceName = prop.SourcePropertyName;
    var cToMethod = prop.IsArray ? ".ToArray()" : ".ToList()";
    string cExpr;

    if (prop.NestedProperties.Length > 0)
    {
        // Inline nested object initializer in Select
        var selectBody = new StringBuilder();
        selectBody.Append("new " + prop.NestedDtoTypeFullName + " { ");
        for (int j = 0; j < prop.NestedProperties.Length; j++)
        {
            var np = prop.NestedProperties[j];
            var nSourceName = np.MappingKind == MappingKind.Custom ? np.SourcePropertyName : np.Name;
            var nComma = j < prop.NestedProperties.Length - 1 ? ", " : "";
            selectBody.Append(np.Name + " = x." + nSourceName + nComma);
        }
        selectBody.Append(" }");
        cExpr = "source." + cSourceName + ".Select(x => " + selectBody + ")" + cToMethod;
    }
    else
    {
        cExpr = "source." + cSourceName + cToMethod;
    }

    if (prop.IsNullable)
    {
        sb.AppendLine("            " + prop.Name + " = source." + cSourceName + " != null ? " + cExpr + " : null" + comma);
    }
    else
    {
        sb.AppendLine("            " + prop.Name + " = " + cExpr + comma);
    }
    break;
```

**Step 4: Add `using System.Linq;` to generated output**

The generated code uses `.Select()`, `.ToList()`, `.ToArray()`. These need `System.Linq` to be available. Add `using System.Linq;\n` after `#nullable enable` in the generated code output in `FacetteGenerator.EmitDtoSource` and `MapperClassBuilder.Build`, but only when there are collection properties.

Alternatively, use fully qualified calls: `System.Linq.Enumerable.Select(...)` — but that's very ugly. Better to add the using.

In `FacetteGenerator.EmitDtoSource`, check if any property is a collection and add the using:

```csharp
var hasCollections = model.Properties.Any(p => p.MappingKind == MappingKind.Collection);
var usings = hasCollections ? "using System.Linq;\n" : "";

var code = "// <auto-generated />\n"
    + "#nullable enable\n"
    + usings + "\n"
    + nsLine
    + ...
```

**Step 5: Run tests**

Run: `dotnet test tests/Facette.Tests --verbosity quiet`
Expected: All tests pass.

**Step 6: Commit**

```bash
git add src/Facette/Builders/MappingBuilder.cs src/Facette/Builders/ProjectionBuilder.cs src/Facette/Builders/PropertyBuilder.cs src/Facette/FacetteGenerator.cs
git commit -m "feat: update builders for collection mapping with Select/ToList"
```

---

### Task 10: Update MapperClassBuilder for nested/collection ToSource extension

**Files:**
- Modify: `src/Facette/Builders/MapperClassBuilder.cs`

**Step 1: Verify MapperClassBuilder still works**

The `MapperClassBuilder` delegates to `FromSource`/`ToSource`/`Projection` on the DTO itself. Since those methods already handle nested and collection mappings, the mapper class extension methods should work unchanged. However, the `ToSource` extension method for collections in `BuildToSource` needs the LINQ `using`.

Add the `using System.Linq;` to the mapper class output when there are collection properties:

```csharp
var hasCollections = model.Properties.Any(p => p.MappingKind == MappingKind.Collection);
if (hasCollections)
{
    sb.AppendLine("using System.Linq;");
}
```

**Step 2: Run tests**

Run: `dotnet test --verbosity quiet`
Expected: All tests pass.

**Step 3: Commit**

```bash
git add src/Facette/Builders/MapperClassBuilder.cs
git commit -m "feat: add System.Linq using to mapper class for collection support"
```

---

### Task 11: Integration tests for Tier 1 features

**Files:**
- Create: `tests/Facette.IntegrationTests/Models/Address.cs`
- Create: `tests/Facette.IntegrationTests/Models/Order.cs`
- Create: `tests/Facette.IntegrationTests/Models/OrderItem.cs`
- Modify: `tests/Facette.IntegrationTests/Models/User.cs`
- Create: `tests/Facette.IntegrationTests/Dtos/AddressDto.cs`
- Create: `tests/Facette.IntegrationTests/Dtos/OrderDto.cs`
- Create: `tests/Facette.IntegrationTests/Dtos/OrderItemDto.cs`
- Create: `tests/Facette.IntegrationTests/NestedMappingIntegrationTests.cs`

**Step 1: Create domain models**

```csharp
// tests/Facette.IntegrationTests/Models/Address.cs
namespace Facette.IntegrationTests.Models;

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string ZipCode { get; set; } = "";
}
```

```csharp
// tests/Facette.IntegrationTests/Models/OrderItem.cs
namespace Facette.IntegrationTests.Models;

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
```

```csharp
// tests/Facette.IntegrationTests/Models/Order.cs
namespace Facette.IntegrationTests.Models;

public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public Address ShippingAddress { get; set; } = new();
    public List<OrderItem> Items { get; set; } = new();
}
```

**Step 2: Update User to include nested types**

Add to the existing `User.cs`:
```csharp
public Address? HomeAddress { get; set; }
public List<Order> Orders { get; set; } = new();
public string[] Tags { get; set; } = Array.Empty<string>();
```

**Step 3: Create DTOs**

```csharp
// tests/Facette.IntegrationTests/Dtos/AddressDto.cs
using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(Address))]
public partial record AddressDto;
```

```csharp
// tests/Facette.IntegrationTests/Dtos/OrderItemDto.cs
using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(OrderItem))]
public partial record OrderItemDto;
```

```csharp
// tests/Facette.IntegrationTests/Dtos/OrderDto.cs
using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(Order))]
public partial record OrderDto;
```

**Step 4: Write integration tests**

```csharp
// tests/Facette.IntegrationTests/NestedMappingIntegrationTests.cs
using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests;

public class NestedMappingIntegrationTests
{
    [Fact]
    public void NestedDto_FromSource_MapsNestedObject()
    {
        var order = new Order
        {
            Id = 1,
            OrderDate = new DateTime(2026, 1, 1),
            ShippingAddress = new Address { Street = "123 Main", City = "Portland", ZipCode = "97201" },
            Items = new List<OrderItem>
            {
                new() { ProductId = 10, ProductName = "Widget", Price = 9.99m, Quantity = 2 },
                new() { ProductId = 20, ProductName = "Gadget", Price = 19.99m, Quantity = 1 }
            }
        };

        var dto = OrderDto.FromSource(order);

        Assert.Equal(1, dto.Id);
        Assert.Equal("123 Main", dto.ShippingAddress.Street);
        Assert.Equal("Portland", dto.ShippingAddress.City);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal("Widget", dto.Items[0].ProductName);
        Assert.Equal(19.99m, dto.Items[1].Price);
    }

    [Fact]
    public void NestedDto_ToSource_RoundTrips()
    {
        var order = new Order
        {
            Id = 1,
            OrderDate = new DateTime(2026, 1, 1),
            ShippingAddress = new Address { Street = "123 Main", City = "Portland", ZipCode = "97201" },
            Items = new List<OrderItem>
            {
                new() { ProductId = 10, ProductName = "Widget", Price = 9.99m, Quantity = 2 }
            }
        };

        var dto = OrderDto.FromSource(order);
        var roundTripped = dto.ToSource();

        Assert.Equal(order.Id, roundTripped.Id);
        Assert.Equal(order.ShippingAddress.Street, roundTripped.ShippingAddress.Street);
        Assert.Equal(order.Items[0].ProductId, roundTripped.Items[0].ProductId);
    }

    [Fact]
    public void NullableNestedDto_FromSource_HandlesNull()
    {
        var user = new User
        {
            Id = 1,
            Name = "Alice",
            Email = "alice@test.com",
            HomeAddress = null
        };

        var dto = UserDto.FromSource(user);

        Assert.Null(dto.HomeAddress);
    }

    [Fact]
    public void NullableNestedDto_FromSource_HandlesNonNull()
    {
        var user = new User
        {
            Id = 1,
            Name = "Alice",
            Email = "alice@test.com",
            HomeAddress = new Address { Street = "456 Oak", City = "Seattle", ZipCode = "98101" }
        };

        var dto = UserDto.FromSource(user);

        Assert.NotNull(dto.HomeAddress);
        Assert.Equal("456 Oak", dto.HomeAddress!.Street);
    }

    [Fact]
    public void Projection_CompilesWithNestedAndCollectionExpressions()
    {
        // Verify the projection expression compiles and can be invoked
        var projection = OrderDto.Projection;
        Assert.NotNull(projection);

        var func = projection.Compile();
        var order = new Order
        {
            Id = 1,
            OrderDate = DateTime.Now,
            ShippingAddress = new Address { Street = "Test", City = "Test", ZipCode = "00000" },
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, ProductName = "X", Price = 1m, Quantity = 1 }
            }
        };

        var dto = func(order);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Test", dto.ShippingAddress.Street);
        Assert.Single(dto.Items);
    }
}
```

**Step 5: Run integration tests**

Run: `dotnet test tests/Facette.IntegrationTests --verbosity quiet`
Expected: All tests pass.

**Step 6: Commit**

```bash
git add tests/Facette.IntegrationTests/
git commit -m "test: add integration tests for nested and collection mapping"
```

---

### Task 12: Update sample project

**Files:**
- Modify: `samples/Facette.Sample/Models/Product.cs`
- Create: `samples/Facette.Sample/Models/Category.cs`
- Create: `samples/Facette.Sample/Models/Review.cs`
- Create: `samples/Facette.Sample/Dtos/CategoryDto.cs`
- Create: `samples/Facette.Sample/Dtos/ReviewDto.cs`
- Modify: `samples/Facette.Sample/Dtos/ProductDto.cs`
- Modify: `samples/Facette.Sample/Program.cs`

**Step 1: Add nested domain models**

```csharp
// samples/Facette.Sample/Models/Category.cs
namespace Facette.Sample.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

```csharp
// samples/Facette.Sample/Models/Review.cs
namespace Facette.Sample.Models;

public class Review
{
    public int Id { get; set; }
    public string Author { get; set; } = "";
    public string Text { get; set; } = "";
    public int Rating { get; set; }
}
```

**Step 2: Update Product to have nested types**

Add to existing `Product`:
```csharp
public Category Category { get; set; } = new();
public List<Review> Reviews { get; set; } = new();
```

**Step 3: Create nested DTOs**

```csharp
// samples/Facette.Sample/Dtos/CategoryDto.cs
using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

[Facette(typeof(Category))]
public partial record CategoryDto;
```

```csharp
// samples/Facette.Sample/Dtos/ReviewDto.cs
using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

[Facette(typeof(Review))]
public partial record ReviewDto;
```

**Step 4: Update Program.cs**

Update the sample to demonstrate nested and collection mapping with a Product that has a Category and Reviews.

**Step 5: Build and run**

Run: `dotnet run --project samples/Facette.Sample`
Expected: Sample runs and shows nested mapping output.

**Step 6: Commit**

```bash
git add samples/
git commit -m "feat: update sample project with nested and collection mapping examples"
```

---

### Task 13: Run final full test suite

**Step 1: Run all tests**

Run: `dotnet test --verbosity quiet`
Expected: All tests pass (unit + integration).

**Step 2: Build entire solution with zero warnings**

Run: `dotnet build 2>&1 | grep -E "Warning|Error"`
Expected: 0 Warning(s), 0 Error(s).

**Step 3: Verify sample runs**

Run: `dotnet run --project samples/Facette.Sample`
Expected: Clean output demonstrating all mapping features.
