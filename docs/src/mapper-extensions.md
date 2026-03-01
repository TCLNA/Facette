# Mapper Extensions

When `GenerateMapper = true` (the default), Facette generates extension methods on the source type for convenient mapping.

## Generated methods

For a DTO like:

```csharp
[Facette(typeof(Product), "InternalSku")]
public partial record ProductDto;
```

Facette generates a static class with these extension methods:

```csharp
public static class ProductFacetteMapperExtensions
{
    public static ProductDto ToProductDto(this Product source)
        => ProductDto.FromSource(source);

    public static IQueryable<ProductDto> ProjectToProductDto(this IQueryable<Product> query)
        => query.Select(ProductDto.Projection);

    public static IQueryable<Product> WhereProductDto(
        this IQueryable<Product> query,
        Expression<Func<ProductDto, bool>> predicate)
        => query.Where(ProductDto.MapExpression(predicate));
}
```

## Usage

```csharp
// Instead of ProductDto.FromSource(product)
var dto = product.ToProductDto();

// Instead of query.Select(ProductDto.Projection)
var dtos = dbContext.Products.ProjectToProductDto().ToList();

// Instead of query.Where(ProductDto.MapExpression(pred))
var filtered = dbContext.Products
    .WhereProductDto(dto => dto.Name == "Widget")
    .ToList();
```

## Ambiguity with multiple DTOs

When multiple DTOs target the same source type, the extension methods can become ambiguous. For example, if both `ProductDto` and `ProductSummaryDto` target `Product`, calling `product.ToProductDto()` and `product.ToProductSummaryDto()` would both exist and work fine — but if the method names collide, use the explicit static calls instead:

```csharp
var dto = ProductDto.FromSource(product);
var summary = ProductSummaryDto.FromSource(product);
```

## Disabling mapper generation

Set `GenerateMapper = false` to suppress extension method generation:

```csharp
[Facette(typeof(Employee), GenerateMapper = false)]
public partial record EmployeeBaseDto;
```

This is useful for:

- [Base DTOs in inheritance hierarchies](./inheritance.md) — avoid conflicting extensions
- DTOs where you prefer explicit `FromSource` calls
- Reducing generated code when extensions aren't needed
