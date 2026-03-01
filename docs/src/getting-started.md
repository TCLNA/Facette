# Getting Started

## Installation

Add the Facette NuGet packages to your project:

```bash
dotnet add package Facette.Abstractions
dotnet add package Facette
```

The `Facette.Abstractions` package provides the attributes you use in your code. The `Facette` package is the source generator that runs at compile time.

> **Note:** The `Facette` generator package targets `netstandard2.0` so it works with any .NET project (net8.0+).

## Your first DTO

Given a model:

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string InternalSku { get; set; } = "";
}
```

Create a DTO by adding a partial record with the `[Facette]` attribute:

```csharp
using Facette.Abstractions;

[Facette(typeof(Product), "InternalSku")]
public partial record ProductDto;
```

That's it. The generator will create properties for every public property on `Product` except `InternalSku`, plus `FromSource`, `ToSource`, and `Projection` members.

## Using the generated code

```csharp
// Source → DTO
var dto = ProductDto.FromSource(product);

// DTO → Source
var entity = dto.ToSource();

// LINQ projection (for EF Core queries)
var dtos = dbContext.Products
    .Select(ProductDto.Projection)
    .ToListAsync();
```

## What gets generated

After building, you can inspect the generated file in your `obj/` directory. The generator produces a partial record that extends your declaration with:

| Member | Purpose |
|--------|---------|
| Properties (`get; init;`) | One per included source property |
| `static FromSource(source)` | Maps source → DTO |
| `ToSource()` | Maps DTO → source |
| `static Projection` | `Expression<Func<TSource, TDto>>` for LINQ |

## Next steps

- [Basic Mapping](./basic-mapping.md) — understand FromSource, ToSource, and Projection in detail
- [Property Control](./property-control.md) — include, exclude, and rename properties
