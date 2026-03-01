# Collections

Facette handles collection properties automatically, mapping each element using the nested DTO's `FromSource`/`ToSource` methods.

## Supported collection types

The generator recognizes and handles these collection types:

- `List<T>`
- `T[]` (arrays)
- `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`
- `HashSet<T>`, `ISet<T>`
- `ImmutableArray<T>`, `ImmutableList<T>`

## Example

```csharp
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

[Facette(typeof(OrderItem))]
public partial record OrderItemDto;

[Facette(typeof(Order), NestedDtos = new[] { typeof(OrderItemDto) })]
public partial record OrderDto;
```

The generated code:

- **FromSource**: `source.Items.Select(x => OrderItemDto.FromSource(x)).ToList()`
- **ToSource**: `Items.Select(x => x.ToSource()).ToList()`
- **Projection**: `Items = source.Items.Select(x => new OrderItemDto { ... }).ToList()`

## Primitive collections

Collections of primitive types (e.g., `List<string>`, `int[]`) are copied directly without element-level mapping:

```csharp
public class Tag
{
    public List<string> Labels { get; set; } = new();
}

[Facette(typeof(Tag))]
public partial record TagDto;
// TagDto.Labels is List<string>, copied as-is
```

## Custom collection converters

For collection types that Facette doesn't handle natively, you can use [value conversions](./value-conversions.md) with `Convert`/`ConvertBack` on `[MapFrom]` to provide custom conversion logic.
