<p align="center">
  <img src="docs/facette.png" alt="Facette logo" width="200" />
</p>

> [!IMPORTANT]
> You're probably searching for [Facet](https://github.com/Tim-Maes/Facet).

A C# source generator that auto-generates DTO mapping code from `[Facette]`-annotated partial records — zero reflection, zero runtime dependencies.

> This project is entirely vibe coded with [Claude Code](https://claude.com/claude-code).

[![NuGet](https://img.shields.io/nuget/v/Facette.svg)](https://www.nuget.org/packages/Facette)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Facette.svg)](https://www.nuget.org/packages/Facette)
[![License](https://img.shields.io/github/license/TCLNA/Facette)](https://github.com/TCLNA/Facette/blob/main/LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/TCLNA/Facette/publish.yml)](https://github.com/TCLNA/Facette/actions/workflows/publish.yml)
  
## Features

- **Compile-time mapping** — `FromSource()`, `ToSource()`, and LINQ `Projection` generated via Roslyn
- **Property control** — Include/Exclude lists, `[MapFrom]` renaming, `[FacetteIgnore]`
- **Nested objects & collections** — auto-detected nested DTOs with `List<T>`, arrays, `HashSet<T>`, `ImmutableArray<T>`
- **Flattening** — convention-based (`HomeAddressCity`) and explicit dot-notation (`[MapFrom("HomeAddress.City")]`)
- **Value conversions** — custom `Convert`/`ConvertBack` methods on `[MapFrom]`
- **Enum conversion** — auto-detect enum↔string/int mismatches
- **Nullable mode** — `AllNullable` for PATCH DTOs, `AllRequired` for strict contracts
- **Copy attributes** — forward data annotations from source to DTO
- **CRUD presets** — `FacettePreset.Create` (no Id, no projection) and `FacettePreset.Read` (no ToSource)
- **Conditional mapping** — `[MapWhen]` for runtime-toggled property inclusion
- **Hooks** — `OnAfterFromSource`, `OnBeforeToSource`, `OnAfterToSource` partial methods
- **Inheritance** — base/derived DTO hierarchies
- **Multi-source mapping** — `[AdditionalSource]` to combine multiple source types
- **Expression mapping** — `MapExpression<TResult>()` rewrites DTO predicates to source expressions
- **EF Core integration** — `ProjectToFacette()` and `WhereFacette()` extensions (no reflection)
- **13 compile-time diagnostics** — FCT001–FCT013 catch configuration errors early

## Quick start

```bash
dotnet add package Facette.Abstractions
dotnet add package Facette
```

Define your model and DTO:

```csharp
public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public decimal Salary { get; set; }
    public string SocialSecurityNumber { get; set; } = "";
}

[Facette(typeof(Employee), "SocialSecurityNumber")]
public partial record EmployeeDto;
```

Use the generated code:

```csharp
// Source → DTO
var dto = EmployeeDto.FromSource(employee);

// DTO → Source
var entity = dto.ToSource();

// LINQ projection (translates to SQL with EF Core)
var dtos = dbContext.Employees
    .Select(EmployeeDto.Projection)
    .ToListAsync();
```

## Packages

| Package | Description |
|---------|-------------|
| `Facette.Abstractions` | Attributes (`[Facette]`, `[MapFrom]`, `[MapWhen]`, etc.) |
| `Facette` | Source generator (compile-time only) |
| `Facette.EntityFrameworkCore` | EF Core extensions (`ProjectToFacette`, `WhereFacette`) |

## Documentation

Full documentation is available in the [mdbook](docs/) — run `mdbook serve docs/` to browse locally.

## Credits

Facette was heavily inspired by [Facet](https://github.com/Tim-Maes/Facet) and [Mapperly](https://github.com/riok/mapperly).

## License

See [LICENSE](LICENSE) for details.
