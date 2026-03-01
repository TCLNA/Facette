# Flattening

Flattening lets you pull nested properties up to the top level of your DTO, avoiding deeply nested objects for simple read scenarios.

## Convention flattening

Name a DTO property by concatenating the navigation path, and Facette will automatically resolve it:

```csharp
public class Employee
{
    public Address? HomeAddress { get; set; }
}

public class Address
{
    public string City { get; set; } = "";
    public string State { get; set; } = "";
}

[Facette(typeof(Employee),
    Include = new[] { "Id", "FirstName", "LastName", "HomeAddress" },
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record EmployeeSummaryDto
{
    public string HomeAddressCity { get; init; } = "";
}
```

`HomeAddressCity` is automatically resolved as `HomeAddress.City` because the property name starts with `HomeAddress` (a navigation property) followed by `City` (a property on `Address`).

The generated code:

- **FromSource**: `HomeAddressCity = source.HomeAddress?.City ?? default`
- **Projection**: `HomeAddressCity = source.HomeAddress.City`

## Explicit dot-notation with MapFrom

For non-conventional names or deeper paths, use `[MapFrom]` with dot notation:

```csharp
[MapFrom("HomeAddress.State")]
public string State { get; init; } = "";
```

This maps `State` on the DTO from `HomeAddress.State` on the source. The property name doesn't need to follow any convention.

## Reverse flattening in ToSource

When generating `ToSource()`, Facette reverses flattened properties back into their nested structure:

```csharp
// Generated ToSource handles nested assignment:
var result = new Employee
{
    HomeAddress = new Address
    {
        City = this.HomeAddressCity,
        State = this.State
    }
};
```

## Limitations

- Convention flattening only activates for multi-segment paths (at least two parts: a navigation property and a leaf property). Single-segment names are never treated as flattened paths.
- If a path segment doesn't resolve, Facette emits [FCT007](./diagnostics.md).
