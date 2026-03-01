# Value Conversions

Value conversions let you transform property values during mapping — for example, formatting a `DateTime` as a string or converting between custom types.

## Defining conversions

Use the `Convert` and `ConvertBack` parameters on `[MapFrom]`:

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber")]
public partial record EmployeeDto
{
    [MapFrom("HireDate", Convert = nameof(FormatDate), ConvertBack = nameof(ParseDate))]
    public string HiredOn { get; init; } = "";

    public static string FormatDate(DateTime dt) => dt.ToString("yyyy-MM-dd");
    public static DateTime ParseDate(string s) => DateTime.Parse(s);
}
```

### Method requirements

- **`Convert`** — used in `FromSource` and `Projection`. Must be a `static` method on the DTO type that takes the source property type and returns the DTO property type.
- **`ConvertBack`** — used in `ToSource`. Must be a `static` method on the DTO type that takes the DTO property type and returns the source property type.

You can specify one without the other:

- `Convert` only — `ToSource` will skip this property (or you can disable `GenerateToSource`)
- `ConvertBack` only — useful when `FromSource` can map directly but the reverse needs transformation

## Generated output

```csharp
// FromSource
HiredOn = EmployeeDto.FormatDate(source.HireDate)

// ToSource
HireDate = EmployeeDto.ParseDate(this.HiredOn)

// Projection
HiredOn = EmployeeDto.FormatDate(source.HireDate)
```

## Diagnostics

If the referenced method doesn't exist or isn't a static method on the DTO type:

- [FCT008](./diagnostics.md) — `Convert` method not found
- [FCT009](./diagnostics.md) — `ConvertBack` method not found

## Tips

- Keep conversion methods simple — they're called per-property, per-mapping operation
- Remember that `Projection` conversions must be translatable by your LINQ provider if used with EF Core. Method calls in projections may not translate to SQL
- For enum↔string/int conversions, see [Enum Conversion](./enum-conversion.md) — Facette handles those automatically without custom converters
