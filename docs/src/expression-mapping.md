# Expression Mapping

`MapExpression` rewrites LINQ expressions written against your DTO type into expressions against the source type. This lets you write filters, sorts, and predicates using DTO property names and have them automatically translated for use with `IQueryable<TSource>`.

## Usage

```csharp
// Write a predicate against the DTO type
Expression<Func<OrderDto, bool>> dtoPredicate = dto => dto.CustomerName == "Bob Smith";

// Rewrite it to work against the source type
Expression<Func<Order, bool>> sourcePredicate = OrderDto.MapExpression(dtoPredicate);

// Use it in a LINQ query
var results = orders.AsQueryable().Where(sourcePredicate).ToList();
```

## How it works

Facette generates a nested `ExpressionVisitor` inside the DTO that:

1. Walks the expression tree
2. Replaces references to DTO properties with their corresponding source property paths
3. Returns a new expression targeting the source type

This handles renamed properties (`[MapFrom]`), flattened paths, and nested DTO property access.

## Example with renamed properties

```csharp
[Facette(typeof(Order), NestedDtos = new[] { typeof(AddressDto), typeof(OrderItemDto) })]
public partial record OrderDto
{
    [MapFrom("Status")]
    public string StatusText { get; init; } = "";
}

// This DTO predicate...
Expression<Func<OrderDto, bool>> pred = dto => dto.StatusText == "Shipped";

// ...gets rewritten to reference Order.Status with the appropriate conversion
var sourcePred = OrderDto.MapExpression(pred);
```

## Combining with LINQ

```csharp
var orders = dbContext.Orders.AsQueryable();

// Filter using DTO expressions
Expression<Func<OrderDto, bool>> filter = dto => dto.CustomerName.StartsWith("B");
var filtered = orders.Where(OrderDto.MapExpression(filter));

// Project to DTOs
var results = filtered.Select(OrderDto.Projection).ToList();
```

## Generated signature

```csharp
public static Expression<Func<TSource, TResult>> MapExpression<TResult>(
    Expression<Func<TDto, TResult>> expression)
```

The return type is generic — it preserves whatever `TResult` your expression returns (`bool` for predicates, any type for selectors).

## See also

- [EF Core Integration](./ef-core.md) — `WhereFacette` wraps `MapExpression` for convenience
- [Mapper Extensions](./mapper-extensions.md) — `WhereDto` extension method
