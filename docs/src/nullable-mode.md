# Nullable Mode

The `NullableMode` property on `[Facette]` controls how nullability is handled on generated DTO properties.

## Modes

| Mode | Behavior |
|------|----------|
| `Auto` (default) | Matches the source property's nullability |
| `AllNullable` | All generated properties become nullable |
| `AllRequired` | All generated properties have nullability stripped |

## AllNullable — PATCH operations

`AllNullable` makes every property nullable, which is ideal for PATCH/partial update DTOs where omitted fields should remain unchanged:

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber",
    NullableMode = NullableMode.AllNullable)]
public partial record NullableEmployeeDto;
```

The generated DTO:

```csharp
public partial record NullableEmployeeDto
{
    public int? Id { get; init; }           // int → int?
    public string? FirstName { get; init; }  // already nullable-ref
    public decimal? Salary { get; init; }    // decimal → decimal?
    public bool? IsActive { get; init; }     // bool → bool?
    // ...
}
```

Usage in a PATCH endpoint:

```csharp
var patchDto = new NullableEmployeeDto
{
    FirstName = "Updated",  // only set what changed
    // everything else is null — meaning "don't change"
};
```

### ToSource with AllNullable

When generating `ToSource()` for `AllNullable` DTOs, value types use `?? default` to handle null:

```csharp
// Generated ToSource
Id = this.Id ?? default,
Salary = this.Salary ?? default,
IsActive = this.IsActive ?? default
```

## AllRequired

`AllRequired` strips nullability from all properties. Use this when you want a strict DTO where every field must be provided:

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber",
    NullableMode = NullableMode.AllRequired)]
public partial record StrictEmployeeDto;
```

Properties like `Address? HomeAddress` would become `Address HomeAddress` (non-nullable).

## Auto

The default mode. Each generated property matches the nullability of the corresponding source property exactly.
