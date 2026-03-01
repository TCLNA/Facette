# Basic Mapping

Facette generates three core mapping members for every annotated DTO: `FromSource`, `ToSource`, and `Projection`.

## FromSource

A static factory method that creates a DTO instance from a source object:

```csharp
var dto = ProductDto.FromSource(product);
```

The generated code copies each property from the source, handling nested objects and collections automatically.

## ToSource

An instance method that creates a new source object from the DTO:

```csharp
var entity = dto.ToSource();
```

This is useful when accepting DTOs from an API and converting them back to domain objects. You can disable this with `GenerateToSource = false`:

```csharp
[Facette(typeof(Product), GenerateToSource = false)]
public partial record ProductReadDto;
```

## Projection

A static `Expression<Func<TSource, TDto>>` property for use in LINQ queries:

```csharp
var dtos = dbContext.Products
    .Select(ProductDto.Projection)
    .ToListAsync();
```

Because it's an expression tree (not a compiled delegate), EF Core can translate it to SQL. The projection inlines nested DTO projections and handles collections with inner `Select` calls.

You can disable projection generation with `GenerateProjection = false`:

```csharp
[Facette(typeof(Employee), GenerateProjection = false)]
public partial record EmployeeCommandDto;
```

## Full example

```csharp
// Model
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string InternalSku { get; set; } = "";
}

// DTO — excludes InternalSku
[Facette(typeof(Product), "InternalSku")]
public partial record ProductDto;

// Usage
var product = new Product { Id = 1, Name = "Widget", Price = 9.99m, InternalSku = "WDG-001" };

var dto = ProductDto.FromSource(product);
// dto.Id = 1, dto.Name = "Widget", dto.Price = 9.99
// No InternalSku property exists on ProductDto

var roundTripped = dto.ToSource();
// roundTripped.Id = 1, roundTripped.Name = "Widget", roundTripped.Price = 9.99
```

## Controlling generation

| Property | Default | Effect |
|----------|---------|--------|
| `GenerateToSource` | `true` | Generates the `ToSource()` method |
| `GenerateProjection` | `true` | Generates the `Projection` expression |
| `GenerateMapper` | `true` | Generates extension methods on the source type |
