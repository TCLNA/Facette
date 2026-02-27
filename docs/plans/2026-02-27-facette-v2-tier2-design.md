# Facette v2 Tier 2 — Design Document

## Overview

Tier 2 adds four features that address real-world mapping gaps from Tier 1:

1. **Multi-level projection inlining** — fix the Tier 1 limitation where nested DTOs with their own nested/collection properties break projection expressions
2. **`[FacetteIgnore]` attribute** — property-level alternative to string-based `Exclude`
3. **Convention-based flattening** — auto-detect `AddressCity` → `source.Address.City`, with `[MapFrom("Address.City")]` override
4. **Value conversions** — `[MapFrom(Convert = nameof(Method))]` for type transformations

## 1. Multi-Level Projection Inlining

### Problem

`GetSimpleSourceProperties` returns only `Direct` PropertyModels. When a nested DTO's source type has its own nested/collection properties, the inlined projection assigns source types directly instead of mapping them:

```csharp
// BROKEN: Order has Address ShippingAddress and List<OrderItem> Items
Projection: new OrderDto {
    ShippingAddress = source.ShippingAddress,  // Address != AddressDto
    Items = source.Items                        // List<OrderItem> != List<OrderItemDto>
}
```

### Fix

Replace `GetSimpleSourceProperties` with `GetNestedSourceProperties`:

```csharp
private static ImmutableArray<PropertyModel> GetNestedSourceProperties(
    INamedTypeSymbol sourceType,
    Dictionary<string, FacetteDtoInfo> facetteLookup,
    HashSet<string> visited)
```

This method:
1. Accepts the facette lookup and a `visited` set (fully-qualified source type names) for cycle detection
2. Adds the source type to `visited` before processing
3. For each source property, checks collection → nested DTO → direct (same logic as `GetSourceProperties` minus Include/Exclude)
4. For nested/collection element types, checks `visited` to detect cycles — if found, skips the property
5. Recursively populates `NestedProperties` on nested/collection PropertyModels

### ProjectionBuilder Changes

Extract nested initializer generation into a recursive method that handles all `MappingKind` variants:
- `Direct`/`Custom`: `PropName = source.Path.PropName`
- `Nested`: `PropName = new DtoType { /* recurse */ }` with nullable wrapping
- `Collection` with DTO elements: `PropName = source.Path.Select(x => new DtoType { /* recurse */ }).ToList()` with nullable wrapping

### New Diagnostic

| Code | Severity | Description |
|------|----------|-------------|
| `FCT006` | Warning | Circular reference detected: type '{0}' references itself through '{1}'. Property skipped from projection. |

## 2. `[FacetteIgnore]` Attribute

### New Attribute

```csharp
namespace Facette.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class FacetteIgnoreAttribute : Attribute { }
```

### Usage

```csharp
[Facette(typeof(User))]
public partial record UserDto
{
    [FacetteIgnore]
    public string PasswordHash { get; init; }
}
```

### Behavior

During the user-declared property scan in ModelBuilder (where `[MapFrom]` is already checked):
- If `[FacetteIgnore]` is found: add property name to `userDeclaredNames` (prevents auto-generation) but do NOT add to `customMappings` (prevents mapping)
- Functionally identical to user-declared without `[MapFrom]`, but makes intent explicit and discoverable

No new diagnostics — placing `[FacetteIgnore]` on a property whose name doesn't match any source property is harmless.

## 3. Convention-Based Flattening

### Auto-Detection Algorithm

When a user-declared property has no `[MapFrom]` and no `[FacetteIgnore]`, before skipping it entirely, try to resolve it as a flattened path:

1. Take the property name (e.g., `AddressCity`)
2. Greedily match source navigation property names:
   - Try longest prefix first
   - `HomeAddressCity` → try `HomeAddressCity` (no match) → `HomeAddress` (match, type has `City`) → resolved: `HomeAddress.City`
3. If resolved, treat as `MappingKind.Custom` with `SourcePropertyName` set to the dot-path (e.g., `"HomeAddress.City"`)

### Dot-Notation in `[MapFrom]`

`[MapFrom("Address.City")]` explicitly specifies a flattened path. The generator splits on `.` and walks the source type chain to validate each segment.

### New `MappingKind`

Add `Flattened` to the `MappingKind` enum:

```csharp
public enum MappingKind
{
    Direct,
    Custom,
    Nested,
    Collection,
    Flattened  // New: source.Address.City → AddressCity
}
```

### Generated Code

```csharp
// User declares: public string AddressCity { get; init; }
// Resolved: Address.City

// FromSource: AddressCity = source.Address.City
// ToSource:   (skipped — flattened properties are read-only)
// Projection: AddressCity = source.Address.City
```

### Nullability

If any segment in the path is nullable, generate null-conditional access:

```csharp
// source.Address? is nullable:
// FromSource: AddressCity = source.Address != null ? source.Address.City : default
// Projection: AddressCity = source.Address != null ? source.Address.City : default
```

### PropertyModel Changes

Add a new field to store the full path for flattened properties:

```csharp
public string FlattenedPath { get; }  // e.g., "Address.City"
public bool FlattenedPathHasNullableSegment { get; }
```

Both participate in `Equals` and `GetHashCode`.

### New Diagnostic

| Code | Severity | Description |
|------|----------|-------------|
| `FCT007` | Warning | `[MapFrom]` path '{0}' could not be resolved on source type '{1}'. Segment '{2}' not found. |

## 4. Value Conversions

### Updated `MapFromAttribute`

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class MapFromAttribute : Attribute
{
    public MapFromAttribute(string sourcePropertyName)
    {
        SourcePropertyName = sourcePropertyName;
    }

    public MapFromAttribute()
    {
        SourcePropertyName = null;
    }

    public string? SourcePropertyName { get; }
    public string? Convert { get; set; }
    public string? ConvertBack { get; set; }
}
```

### Usage

```csharp
[Facette(typeof(User))]
public partial record UserDto
{
    // Rename + convert
    [MapFrom("CreatedAt", Convert = nameof(ToIso), ConvertBack = nameof(FromIso))]
    public string CreatedDate { get; init; }

    // Same name, just convert
    [MapFrom(Convert = nameof(FormatPrice))]
    public string Price { get; init; }

    private static string ToIso(DateTime dt) => dt.ToString("o");
    private static DateTime FromIso(string s) => DateTime.Parse(s);
    private static string FormatPrice(decimal price) => price.ToString("F2");
}
```

### Generator Behavior

- `Convert`: generator looks for a `static` method with that name on the target type (DTO). Method must accept one parameter matching the source property type and return the DTO property type.
- `ConvertBack`: same but reversed — accepts DTO property type, returns source property type. Optional. If absent, property is excluded from `ToSource`.
- When `SourcePropertyName` is null (parameterless constructor), the property's own name is used as source property name.

### Generated Code

```csharp
// FromSource: CreatedDate = UserDto.ToIso(source.CreatedAt)
// ToSource:   CreatedAt = UserDto.FromIso(this.CreatedDate)
// Projection: CreatedDate = UserDto.ToIso(source.CreatedAt)
```

### New Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| `FCT008` | Warning | Convert method '{0}' not found as a static method on type '{1}' |
| `FCT009` | Warning | ConvertBack method '{0}' not found as a static method on type '{1}' |

## 5. Full Diagnostics Table (Tier 2 additions)

| Code | Severity | Description |
|------|----------|-------------|
| `FCT006` | Warning | Circular reference detected: type '{0}' references itself through '{1}' |
| `FCT007` | Warning | `[MapFrom]` path '{0}' could not be resolved on source type '{1}'. Segment '{2}' not found |
| `FCT008` | Warning | Convert method '{0}' not found as a static method on type '{1}' |
| `FCT009` | Warning | ConvertBack method '{0}' not found as a static method on type '{1}' |

## 6. Testing Strategy

### Multi-Level Projection Tests
- 2-level nesting: `Order.ShippingAddress` (Address → AddressDto) inlines correctly
- Collection with nested elements: `Order.Items` inlines `.Select(x => new OrderItemDto { ... })`
- 3-level nesting: projection recurses correctly
- Circular reference: FCT006 emitted, property skipped
- Integration: OrderDto with Address + List<OrderItem> compiles and round-trips

### FacetteIgnore Tests
- `[FacetteIgnore]` excludes property from generation and mapping
- Not present in FromSource/ToSource/Projection

### Flattening Tests
- Convention: `AddressCity` auto-resolves to `source.Address.City`
- Dot-notation: `[MapFrom("Address.City")]` works
- Nullable path segment generates null-conditional access
- FCT007 for invalid path segments
- Excluded from ToSource

### Value Conversion Tests
- `Convert` generates method call in FromSource
- `ConvertBack` generates method call in ToSource
- No `ConvertBack` → property excluded from ToSource
- Parameterless `[MapFrom(Convert = ...)]` uses same property name
- FCT008/FCT009 for invalid method names
- Integration: end-to-end with converted properties
