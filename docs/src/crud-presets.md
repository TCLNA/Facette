# CRUD Presets

Facette presets are shortcuts that configure common DTO patterns for create and read operations.

## FacettePreset.Create

Configures a DTO for creation operations:

- **Excludes `Id` properties** — new entities shouldn't have an ID yet
- **Disables projection** — create DTOs are not used for queries

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber",
    Preset = FacettePreset.Create,
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record CreateEmployeeDto;
```

```csharp
// No Id property exists
typeof(CreateEmployeeDto).GetProperty("Id"); // null

// No Projection property generated
// CreateEmployeeDto.Projection — does not exist

// FromSource and ToSource still work
var dto = CreateEmployeeDto.FromSource(employee);
var entity = dto.ToSource();
```

## FacettePreset.Read

Configures a DTO for read-only operations:

- **Disables `ToSource`** — read DTOs should not be converted back to entities

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber",
    Preset = FacettePreset.Read,
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record ReadEmployeeDto;
```

```csharp
// Has FromSource and Projection
var dto = ReadEmployeeDto.FromSource(employee);

// No ToSource method generated
typeof(ReadEmployeeDto).GetMethod("ToSource"); // null
```

## FacettePreset.Default

The default preset. All generation flags are enabled (`FromSource`, `ToSource`, `Projection`, mapper extensions). This is equivalent to not specifying a preset.

## Combining with other features

Presets can be combined with all other Facette features:

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber",
    Preset = FacettePreset.Create,
    NullableMode = NullableMode.AllRequired,
    CopyAttributes = true,
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record CreateEmployeeDto;
```
