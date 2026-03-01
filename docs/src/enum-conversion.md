# Enum Conversion

Facette automatically detects type mismatches between enum properties on the source and their DTO counterparts, generating the appropriate conversion code.

## Auto-detection

When a source property is an enum and the DTO property is a different type (or vice versa), Facette chooses the right conversion:

```csharp
public class Employee
{
    public Department Department { get; set; }  // enum
}

[Facette(typeof(Employee), "SocialSecurityNumber")]
public partial record EmployeeDto
{
    [MapFrom("Department")]
    public string DepartmentName { get; init; } = "";  // string
}
```

Facette detects `Department` (enum) → `DepartmentName` (string) and generates:

```csharp
// FromSource
DepartmentName = source.Department.ToString()

// ToSource
Department = Enum.Parse<Department>(this.DepartmentName)
```

## Conversion kinds

| Source Type | DTO Type | FromSource | ToSource |
|-------------|----------|------------|----------|
| `enum` | `string` | `.ToString()` | `Enum.Parse<T>(value)` |
| `string` | `enum` | `Enum.Parse<T>(value)` | `.ToString()` |
| `enum` | `int` | `(int)value` | `(T)value` |
| `int` | `enum` | `(T)value` | `(int)value` |
| `EnumA` | `EnumB` | `(EnumB)(int)value` | `(EnumA)(int)value` |

## Projection considerations

Enum-to-string conversions use `.ToString()` in projections, which some LINQ providers (like EF Core) may not be able to translate to SQL. Facette emits [FCT011](./diagnostics.md) as a warning in this case.

If you use EF Core and need enum properties in projections, consider:

- Keeping the same enum type on the DTO (no conversion needed)
- Using `int` on the DTO (cast is translatable)
- Configuring EF Core to store enums as strings via `HasConversion<string>()`

## Example with OrderStatus

```csharp
public class Order
{
    public OrderStatus Status { get; set; }  // enum
}

[Facette(typeof(Order), NestedDtos = new[] { typeof(AddressDto), typeof(OrderItemDto) })]
public partial record OrderDto
{
    [MapFrom("Status")]
    public string StatusText { get; init; } = "";
}
```

```csharp
var orderDto = OrderDto.FromSource(order);
Console.WriteLine(orderDto.StatusText); // "Shipped"
```
