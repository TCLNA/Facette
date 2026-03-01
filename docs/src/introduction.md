# Introduction

Facette is a C# source generator that automatically creates DTO mapping code from annotated partial records. Instead of writing tedious `FromSource`, `ToSource`, and LINQ projection methods by hand — or relying on runtime reflection — Facette generates all of it at compile time.

## Why Facette?

- **Zero reflection** — all mapping code is generated at compile time
- **Zero runtime dependencies** — the `Facette.Abstractions` package contains only attributes
- **Full IntelliSense** — generated code is visible in your IDE
- **LINQ projection support** — generates `Expression<Func<TSource, TDto>>` for efficient database queries
- **Compile-time diagnostics** — catches configuration mistakes before you run the app

## How it works

Facette uses the [Roslyn incremental source generator](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) API. When you annotate a `partial record` with `[Facette(typeof(SourceType))]`, the generator inspects the source type at compile time and emits:

1. **`FromSource(source)`** — a static factory that maps source → DTO
2. **`ToSource()`** — an instance method that maps DTO → source
3. **`Projection`** — a static `Expression<Func<TSource, TDto>>` for LINQ/EF Core queries
4. **Mapper extensions** — `ToDto()`, `ProjectToDto()` extension methods on the source type

## What you write

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber")]
public partial record EmployeeDto;
```

## What gets generated

```csharp
public partial record EmployeeDto
{
    public int Id { get; init; }
    public string FirstName { get; init; }
    public string LastName { get; init; }
    // ... all properties except SocialSecurityNumber

    public static EmployeeDto FromSource(Employee source) { /* ... */ }
    public Employee ToSource() { /* ... */ }
    public static Expression<Func<Employee, EmployeeDto>> Projection => /* ... */;
}
```

No boilerplate. No runtime overhead. Just annotate and go.

## Credits

Facette was heavily inspired by:

- [Facet](https://github.com/Tim-Maes/Facet) — a source generator that generates DTOs, mappings, and LINQ projections from domain models, with a similar attribute-driven approach
- [Mapperly](https://github.com/riok/mapperly) — a compile-time object mapper for .NET that pioneered the source-generator approach to mapping

This project is entirely vibe coded with [Claude Code](https://claude.com/claude-code).
