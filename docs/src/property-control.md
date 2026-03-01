# Property Control

Facette provides several ways to control which properties appear on the generated DTO and how they're mapped.

## Exclude

Pass property names to exclude in the `[Facette]` constructor. The second parameter is `params string[]`, so you can list them inline:

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber", "Notes")]
public partial record EmployeeDto;
```

Or pass an explicit array with the named parameter:

```csharp
[Facette(typeof(Employee), exclude: new[] { "SocialSecurityNumber", "Notes" })]
public partial record EmployeeDto;
```

Both forms are equivalent. The generated DTO will have all of `Employee`'s public properties except `SocialSecurityNumber` and `Notes`.

## Include

Use the `Include` named parameter to specify an allowlist. Only listed properties will be generated:

```csharp
[Facette(typeof(Employee),
    Include = new[] { "Id", "FirstName", "LastName", "Email" })]
public partial record EmployeeSummaryDto;
```

> **Note:** You cannot use both `Include` and `Exclude` on the same DTO. Doing so produces diagnostic [FCT002](./diagnostics.md).

## FacetteIgnore

Mark properties you define manually on the DTO to prevent the generator from trying to map them:

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber")]
public partial record EmployeeDto
{
    [FacetteIgnore]
    public string FullName { get; set; } = "";
}
```

Without `[FacetteIgnore]`, the generator would look for a `FullName` property on `Employee` and fail. With it, the property is left untouched — you can populate it in an [AfterMap hook](./hooks.md).

## MapFrom

Rename a property or map it from a different source property:

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber")]
public partial record EmployeeDto
{
    [MapFrom("Department")]
    public string DepartmentName { get; init; } = "";
}
```

Here, `DepartmentName` on the DTO is mapped from the `Department` property on `Employee`. Since `Department` is an enum and `DepartmentName` is a string, Facette will also auto-detect the [enum conversion](./enum-conversion.md).

`MapFrom` also supports dot-notation for [flattened paths](./flattening.md):

```csharp
[MapFrom("HomeAddress.State")]
public string State { get; init; } = "";
```

### MapFrom with value conversion

`MapFrom` has optional `Convert` and `ConvertBack` parameters for [value conversions](./value-conversions.md):

```csharp
[MapFrom("HireDate", Convert = nameof(FormatDate), ConvertBack = nameof(ParseDate))]
public string HiredOn { get; init; } = "";

public static string FormatDate(DateTime dt) => dt.ToString("yyyy-MM-dd");
public static DateTime ParseDate(string s) => DateTime.Parse(s);
```

## Referencing non-existent properties

If an `Include`, `Exclude`, or `MapFrom` reference names a property that doesn't exist on the source type, Facette emits a warning diagnostic:

- [FCT004](./diagnostics.md) — property in Include/Exclude not found
- [FCT005](./diagnostics.md) — MapFrom source property not found
