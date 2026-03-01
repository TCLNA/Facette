# Multi-Source Mapping

The `[AdditionalSource]` attribute lets a DTO combine properties from multiple source types into a single DTO.

## Usage

```csharp
public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    // ...
}

public class EmployeeProfile
{
    public string Bio { get; set; } = "";
    public string PhotoUrl { get; set; } = "";
}

[Facette(typeof(Employee), "SocialSecurityNumber",
    NestedDtos = new[] { typeof(AddressDto) },
    GenerateProjection = false)]
[AdditionalSource(typeof(EmployeeProfile), "Profile")]
public partial record EmployeeDetailDto;
```

## How it works

The `[AdditionalSource]` attribute takes two parameters:

- **`sourceType`** — the additional type to map from
- **`prefix`** — a string prefix added to properties from this source

With the prefix `"Profile"`, the properties from `EmployeeProfile` are generated as:

- `ProfileBio` (from `Bio`)
- `ProfilePhotoUrl` (from `PhotoUrl`)

## Generated FromSource

The generated `FromSource` method accepts multiple source parameters:

```csharp
public static EmployeeDetailDto FromSource(Employee source, EmployeeProfile source1)
{
    return new EmployeeDetailDto
    {
        Id = source.Id,
        FirstName = source.FirstName,
        // ... Employee properties ...
        ProfileBio = source1.Bio,
        ProfilePhotoUrl = source1.PhotoUrl,
    };
}
```

## Usage

```csharp
var employee = new Employee { /* ... */ };
var profile = new EmployeeProfile
{
    Bio = "Senior engineer with 10 years of experience",
    PhotoUrl = "https://example.com/photos/alice.jpg"
};

var dto = EmployeeDetailDto.FromSource(employee, profile);
Console.WriteLine(dto.ProfileBio);      // "Senior engineer with 10 years of experience"
Console.WriteLine(dto.ProfilePhotoUrl); // "https://example.com/photos/alice.jpg"
```

## Notes

- **Projection is typically disabled** (`GenerateProjection = false`) for multi-source DTOs, since LINQ projections work with a single `IQueryable<T>` source
- Multiple `[AdditionalSource]` attributes can be stacked for more than two sources
- The prefix helps avoid property name collisions between sources
- An empty prefix (`""`) means properties are added without a prefix — only safe if names don't collide
