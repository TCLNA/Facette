# Inheritance

Facette supports DTO inheritance, allowing you to create base DTOs and extend them with additional properties.

## Base and derived DTOs

Define a base DTO with shared properties, then create derived DTOs that inherit from it:

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber",
    GenerateMapper = false)]
public partial record EmployeeBaseDto;

[Facette(typeof(Employee), "SocialSecurityNumber",
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record EmployeeFullDto : EmployeeBaseDto;
```

The derived DTO inherits all generated properties from the base and adds its own.

## GenerateMapper = false

When using inheritance, set `GenerateMapper = false` on the base DTO to avoid conflicting extension methods. If both the base and derived DTO generate mapper extensions for the same source type, you'll get ambiguous method errors.

```csharp
// Base — no mapper extensions
[Facette(typeof(Employee), "SocialSecurityNumber",
    GenerateMapper = false)]
public partial record EmployeeBaseDto;

// Derived — generates mapper extensions
[Facette(typeof(Employee), "SocialSecurityNumber")]
public partial record EmployeeDetailDto : EmployeeBaseDto;
```

## How generated code works

Each DTO in the hierarchy generates its own `FromSource`, `ToSource`, and `Projection` independently. The derived DTO's generated members handle all properties (inherited + own), so you can use any DTO in the hierarchy directly:

```csharp
var baseDto = EmployeeBaseDto.FromSource(employee);
var fullDto = EmployeeFullDto.FromSource(employee);
```

## Tips

- Use `GenerateMapper = false` on base DTOs to prevent extension method conflicts
- Each level generates complete mapping logic — no need to chain base/derived calls
- Consider using [Include](./property-control.md) on the base DTO to limit it to core fields
