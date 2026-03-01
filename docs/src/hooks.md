# Hooks

Facette generates partial method hooks that you can implement to run custom logic before or after mapping operations.

## Available hooks

| Hook | Called |
|------|--------|
| `OnAfterFromSource(TSource source)` | After `FromSource` completes |
| `OnBeforeToSource(TSource target)` | Before `ToSource` returns, with the target object |
| `OnAfterToSource(TSource target)` | After `ToSource` populates the target, before returning |

## OnAfterFromSource

Runs after all properties have been mapped from the source. Use this to compute derived properties:

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber",
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record EmployeeDto
{
    [FacetteIgnore]
    public string FullName { get; set; } = "";

    partial void OnAfterFromSource(Employee source)
    {
        FullName = $"{source.FirstName} {source.LastName}";
    }
}
```

```csharp
var dto = EmployeeDto.FromSource(employee);
Console.WriteLine(dto.FullName); // "Alice Johnson"
```

## OnBeforeToSource / OnAfterToSource

Run custom logic during `ToSource()`:

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber")]
public partial record EmployeeDto
{
    partial void OnBeforeToSource(Employee target)
    {
        // Runs before properties are assigned to target
    }

    partial void OnAfterToSource(Employee target)
    {
        // Runs after all properties are assigned
        // Useful for setting computed or derived fields on the source
    }
}
```

## How it works

Since hooks are declared as `partial void` methods, they compile to no-ops if you don't implement them — zero overhead when unused. The generator emits the partial method declarations, and you provide the implementations in your part of the partial record.

## Tips

- Use `[FacetteIgnore]` on properties that are populated by hooks, so the generator doesn't try to map them from the source
- Hooks run synchronously and have access to both the DTO instance (`this`) and the source/target object
- Hooks don't affect `Projection` — expression trees cannot contain imperative code
