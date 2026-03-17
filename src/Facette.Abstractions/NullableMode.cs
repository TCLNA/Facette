namespace Facette.Abstractions;

/// <summary>
/// Controls how nullable annotations are applied to generated DTO properties.
/// </summary>
public enum NullableMode
{
    /// <summary>
    /// Preserves the nullability of the source property as-is.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Makes all generated properties nullable, useful for PATCH-style DTOs
    /// where any property may be omitted.
    /// </summary>
    AllNullable = 1,

    /// <summary>
    /// Makes all generated properties non-nullable, regardless of source nullability.
    /// </summary>
    AllRequired = 2
}
