# Copy Attributes

When `CopyAttributes = true`, Facette copies data annotation and validation attributes from source properties to the generated DTO properties.

## Usage

```csharp
public class Employee
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = "";

    [EmailAddress]
    public string Email { get; set; } = "";
}

[Facette(typeof(Employee), "SocialSecurityNumber",
    CopyAttributes = true)]
public partial record EmployeeDto;
```

The generated DTO will include the same attributes:

```csharp
public partial record EmployeeDto
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; init; }

    [EmailAddress]
    public string Email { get; init; }
}
```

This is useful for API validation — your DTOs automatically inherit the validation rules defined on your domain models.

## Verifying at runtime

```csharp
var attrs = typeof(EmployeeDto).GetProperty("FirstName")!
    .GetCustomAttributes(false);
// Contains: RequiredAttribute, StringLengthAttribute
```

## Skipped attributes

Some attributes cannot be fully reconstructed at compile time (e.g., attributes with complex constructor arguments or non-constant property values). When Facette encounters these, it skips them and emits [FCT012](./diagnostics.md) as an informational diagnostic.

Attributes from certain system namespaces (like `System.Runtime.CompilerServices`) are also excluded to avoid noise.

## When to use

- **API DTOs** — share validation attributes between model and DTO without duplication
- **Swagger/OpenAPI** — copied attributes are picked up by schema generators
- **FluentValidation interop** — if your validators inspect data annotations

## When not to use

If your DTO validation rules differ from your domain model, keep `CopyAttributes = false` (the default) and define attributes manually on the DTO.
