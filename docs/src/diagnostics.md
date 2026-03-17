# Diagnostics

Facette emits compile-time diagnostics to help you catch configuration errors early. All diagnostics use the `FCT` prefix.

## Reference

| Code | Severity | Title | Description |
|------|----------|-------|-------------|
| **FCT001** | Error | Type must be partial | The type annotated with `[Facette]` must be declared as `partial`. |
| **FCT002** | Error | Include and Exclude conflict | Both `Include` and `Exclude` are specified on the same DTO, which is not allowed. Use one or the other. |
| **FCT003** | Error | Source type not found | The `SourceType` passed to `[Facette]` could not be resolved. Check that the type exists and is accessible. |
| **FCT004** | Warning | Property not found on source | A property name in `Include` or `Exclude` doesn't exist on the source type. |
| **FCT005** | Warning | MapFrom property not found | The `SourcePropertyName` in `[MapFrom]` doesn't exist on the source type. |
| **FCT006** | Warning | Circular reference | A nested DTO chain forms a cycle (e.g., A → B → A). Facette skips the cycle to prevent infinite recursion. |
| **FCT007** | Warning | Flattened path segment not found | A segment in a convention-flattened or dot-notation path couldn't be resolved on the source type. |
| **FCT008** | Warning | Convert method not found | The `Convert` method specified in `[MapFrom]` wasn't found as a static method on the DTO type. |
| **FCT009** | Warning | ConvertBack method not found | The `ConvertBack` method specified in `[MapFrom]` wasn't found as a static method on the DTO type. |
| **FCT010** | Error | Ambiguous nested DTO | Multiple DTOs target the same source type and Facette can't determine which to use. Specify `NestedDtos` on the parent DTO to resolve. |
| **FCT011** | Warning | Enum projection warning | An enum conversion (e.g., `.ToString()`) in a LINQ projection may not be translatable by EF Core. |
| **FCT012** | Info | Attribute reconstruction skipped | When `CopyAttributes = true`, an attribute on the source property couldn't be fully reconstructed and was skipped. |
| **FCT013** | Error | Conditional method invalid | The method referenced by `[MapWhen]` wasn't found, isn't `static`, isn't parameterless, or doesn't return `bool`. |
| **FCT099** | Warning | Internal generator error | An unexpected error occurred during code generation. Please [report it](https://github.com/TCLNA/Facette/issues). |

## Treating warnings as errors

You can promote Facette warnings to errors in your project file:

```xml
<PropertyGroup>
  <WarningsAsErrors>FCT004;FCT005;FCT006;FCT007;FCT008;FCT009;FCT011</WarningsAsErrors>
</PropertyGroup>
```

## Suppressing diagnostics

To suppress specific diagnostics:

```xml
<PropertyGroup>
  <NoWarn>FCT011;FCT012</NoWarn>
</PropertyGroup>
```
