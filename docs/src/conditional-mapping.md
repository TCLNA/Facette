# Conditional Mapping

The `[MapWhen]` attribute lets you conditionally include or exclude a property during mapping based on a runtime condition.

## Usage

```csharp
[Facette(typeof(Employee), "SocialSecurityNumber",
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record ConditionalEmployeeDto
{
    private static bool _includeSalary = true;

    [MapWhen(nameof(ShouldIncludeSalary))]
    public decimal Salary { get; init; }

    public static bool ShouldIncludeSalary() => _includeSalary;
    public static void SetIncludeSalary(bool value) => _includeSalary = value;
}
```

## Generated code

The generator wraps the property assignment in a conditional:

```csharp
// Generated FromSource
Salary = ConditionalEmployeeDto.ShouldIncludeSalary()
    ? source.Salary
    : default
```

When the condition returns `false`, the property gets the `default` value for its type (`0` for `decimal`, `null` for reference types, etc.).

## Method requirements

The condition method must be:

- **`static`** — no instance required
- **Parameterless** — takes no arguments
- **Returns `bool`** — true to include, false to skip

If the method doesn't meet these requirements, Facette emits [FCT013](./diagnostics.md).

## Runtime toggling

Because the condition is evaluated at mapping time (not compile time), you can change behavior dynamically:

```csharp
// Include salary for authorized users
ConditionalEmployeeDto.SetIncludeSalary(true);
var withSalary = ConditionalEmployeeDto.FromSource(employee);
Console.WriteLine(withSalary.Salary); // 95000

// Hide salary for unauthorized users
ConditionalEmployeeDto.SetIncludeSalary(false);
var withoutSalary = ConditionalEmployeeDto.FromSource(employee);
Console.WriteLine(withoutSalary.Salary); // 0
```

## Use cases

- **Role-based field visibility** — hide sensitive fields based on user role
- **Feature flags** — conditionally include new fields during rollout
- **Environment-based** — include debug fields only in development
