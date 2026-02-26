# Facette v2 Tier 1 — Design Document

## Overview

Tier 1 adds three features to Facette that unlock real-world mapping scenarios beyond simple property mirroring:

1. **Custom property mapping** — `[MapFrom]` attribute for when source and DTO property names differ
2. **Nested object mapping** — auto-detect nested Facette DTOs, generate chained mapping calls
3. **Collection mapping** — handle `List<T>`, `T[]`, and common collection interfaces with element-level mapping

## 1. Custom Property Mapping — `[MapFrom]`

### New Attribute

```csharp
namespace Facette.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MapFromAttribute : Attribute
{
    public MapFromAttribute(string sourcePropertyName)
    {
        SourcePropertyName = sourcePropertyName;
    }

    public string SourcePropertyName { get; }
}
```

### Usage

```csharp
[Facette(typeof(User))]
public partial record UserDto
{
    [MapFrom("FirstName")]
    public string DisplayName { get; init; }
}
```

### Behavior

- User declares a property on the partial record with `[MapFrom("SourceProp")]`.
- The generator detects user-declared properties on the target type.
- It skips generating a property with the same name (it already exists).
- It wires up the mapping in `FromSource`/`ToSource`/`Projection` using the specified source property name.
- User-declared properties **without** `[MapFrom]` are skipped entirely — the user manages them.

### Generated Code

```csharp
// FromSource: DisplayName = source.FirstName
// ToSource:   FirstName = this.DisplayName
// Projection: DisplayName = source.FirstName
```

## 2. Nested Object Mapping

### Auto-Detection

When a source property's type has a corresponding `[Facette]`-annotated DTO in the compilation, the generator automatically uses the nested DTO type and chains mapping calls.

The generator scans `context.SemanticModel.Compilation` to find all `[Facette]`-annotated types and builds a lookup of source type → DTO type.

### Override via `[MapFrom]`

Users can override auto-detection by declaring the property manually with a different DTO type:

```csharp
[Facette(typeof(User))]
public partial record UserDto
{
    [MapFrom("HomeAddress")]
    public FlatAddressDto HomeAddress { get; init; }
}
```

### Nullability

When the source property is nullable (`Address?`), the generated mapping includes null checks:

```csharp
// DTO property: public AddressDto? HomeAddress { get; init; }
// FromSource:   HomeAddress = source.HomeAddress != null ? AddressDto.FromSource(source.HomeAddress) : null
// ToSource:     HomeAddress = this.HomeAddress != null ? this.HomeAddress.ToSource() : null
// Projection:   HomeAddress = source.HomeAddress != null ? new AddressDto { ... } : null
```

### Projection

Projections inline the nested DTO's object initializer directly (rather than calling `FromSource()`) so EF Core can translate the expression to SQL:

```csharp
public static Expression<Func<User, UserDto>> Projection =>
    source => new UserDto
    {
        Id = source.Id,
        HomeAddress = new AddressDto
        {
            Street = source.HomeAddress.Street,
            City = source.HomeAddress.City
        }
    };
```

## 3. Collection Mapping

### Supported Collection Types

Input types recognized as collections (on the source entity):
- `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`
- `IReadOnlyList<T>`, `IReadOnlyCollection<T>`
- `T[]` (arrays)

String is explicitly excluded (even though it implements `IEnumerable<char>`).

### Output Type

- If the source is `T[]`, the DTO property is `T[]` (or `DtoT[]`).
- Otherwise, the DTO property is `System.Collections.Generic.List<T>` (or `List<DtoT>`).

### Collection with Nested DTO Elements

When the element type `T` has a corresponding Facette DTO:

```csharp
// Source: List<Order> Orders
// DTO:    List<OrderDto> Orders

// FromSource: Orders = source.Orders.Select(x => OrderDto.FromSource(x)).ToList()
// ToSource:   Orders = this.Orders.Select(x => x.ToSource()).ToList()
// Projection: Orders = source.Orders.Select(x => new OrderDto { OrderId = x.OrderId, ... }).ToList()
```

Array variant uses `.ToArray()` instead of `.ToList()`.

### Collection with Simple Elements

When the element type has no Facette DTO (e.g., `List<string>`, `int[]`):

```csharp
// FromSource: Tags = source.Tags.ToList()  (or .ToArray() for arrays)
// Projection: Tags = source.Tags.ToList()
```

If source and DTO types match exactly (e.g., both `List<string>`), direct assignment is used.

### Nullable Collections

```csharp
// FromSource: Orders = source.Orders != null ? source.Orders.Select(...).ToList() : null
```

## 4. Enriched PropertyModel

`PropertyModel` gains new fields to describe mapping behavior:

```csharp
public enum MappingKind
{
    Direct,     // Name = source.Name (v1 behavior)
    Custom,     // DtoName = source.SourcePropName (via [MapFrom])
    Nested,     // Address = AddressDto.FromSource(source.Address)
    Collection  // Orders = source.Orders.Select(x => ...).ToList()
}

public sealed class PropertyModel : IEquatable<PropertyModel>
{
    // Existing
    public string Name { get; }
    public string TypeFullName { get; }
    public bool IsValueType { get; }

    // New
    public MappingKind MappingKind { get; }
    public string SourcePropertyName { get; }           // source-side name (same as Name for Direct)
    public string NestedDtoTypeName { get; }             // e.g., "AddressDto"
    public string NestedDtoTypeFullName { get; }         // e.g., "global::MyApp.AddressDto"
    public string CollectionElementTypeFullName { get; } // element type before mapping
    public bool IsNullable { get; }                      // source property nullability
    public bool IsArray { get; }                         // T[] vs List<T>
}
```

All new fields participate in `Equals` and `GetHashCode` for incremental generator caching.

## 5. ModelBuilder Changes

### User-Declared Property Detection

`ModelBuilder.Build` scans `targetSymbol.GetMembers()` for user-declared properties:
- Properties with `[MapFrom]` → `Custom` (or `Nested`/`Collection` based on source property type)
- Properties without `[MapFrom]` → skipped entirely
- User-declared property names are excluded from auto-generation

### Nested DTO Discovery

The generator scans `context.SemanticModel.Compilation` for all types annotated with `[Facette]` and builds a `Dictionary<sourceTypeFullName, dtoTypeInfo>` lookup. When a source property's type matches a known Facette source type, the property becomes `Nested`.

### Collection Detection

A source property is a collection if:
1. Its type is `T[]`, or
2. Its type implements `IEnumerable<T>` (checked via `AllInterfaces`), and
3. Its type is not `string`

The element type `T` is extracted and checked against the nested DTO lookup.

### Nullability Detection

Uses `prop.NullableAnnotation == NullableAnnotation.Annotated` for reference types. For value types, checks if the type is `Nullable<T>`.

## 6. Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| `FCT005` | Warning | `[MapFrom]` references property '{0}' which was not found on source type '{1}' |

## 7. Testing Strategy

### Custom Mapping Tests
- `[MapFrom("SourceProp")]` generates correct FromSource/ToSource/Projection mappings
- User-declared property without `[MapFrom]` is skipped by generator
- FCT005 when `[MapFrom]` references invalid property name

### Nested Object Tests
- Auto-detected nested DTO generates `FromSource()` call in mapping
- Nullable nested property generates null-conditional mapping
- User override via `[MapFrom]` on nested property
- Projection inlines nested object initializer

### Collection Tests
- `List<T>` with nested DTO generates `.Select(x => Dto.FromSource(x)).ToList()`
- `T[]` with nested DTO generates `.Select(x => Dto.FromSource(x)).ToArray()`
- `List<string>` (simple element) generates `.ToList()` or direct copy
- Nullable collection generates null check
- Projection inlines collection `.Select(x => new Dto { ... }).ToList()`

### Integration Tests
- End-to-end: entity with nested objects + collections → DTO → back
- IQueryable projection compiles with nested/collection expressions
