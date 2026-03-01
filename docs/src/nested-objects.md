# Nested Objects

When a source type has a property whose type is also mapped by a Facette DTO, the generator automatically uses the nested DTO in `FromSource`, `ToSource`, and `Projection`.

## Auto-detection

If there is exactly one DTO targeting a nested source type, Facette resolves it automatically:

```csharp
public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public Address? HomeAddress { get; set; }
}

[Facette(typeof(Address))]
public partial record AddressDto;

[Facette(typeof(Employee))]
public partial record EmployeeDto;
// EmployeeDto.HomeAddress is of type AddressDto
```

The generated `FromSource` will call `AddressDto.FromSource(source.HomeAddress)`, and the `Projection` will inline `AddressDto.Projection` as a nested member-init expression.

## Resolving ambiguity with NestedDtos

When multiple DTOs target the same source type, Facette cannot auto-detect which one to use and emits [FCT010](./diagnostics.md). Resolve this by specifying which DTOs to use:

```csharp
[Facette(typeof(Employee),
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record EmployeeDto;
```

This tells the generator to use `AddressDto` whenever it encounters an `Address` property.

## Nullable nested objects

When a source property is nullable (e.g., `Address? HomeAddress`), the generated mapping handles null correctly:

- **FromSource**: `source.HomeAddress is not null ? AddressDto.FromSource(source.HomeAddress) : null`
- **ToSource**: similar null-guarded reverse mapping
- **Projection**: null-conditional in the expression tree

The generated DTO property will also be nullable to match.

## Circular references

If nested DTOs form a cycle (A → B → A), Facette detects this and emits [FCT006](./diagnostics.md) to prevent stack overflows during mapping.
