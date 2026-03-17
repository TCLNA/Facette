namespace Facette.Abstractions;

/// <summary>
/// Marks a partial record as a Facette DTO, enabling compile-time mapping code generation
/// from the specified source type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class FacetteAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FacetteAttribute"/> class.
    /// </summary>
    /// <param name="sourceType">The source type to map from.</param>
    /// <param name="exclude">Property names to exclude from mapping.</param>
    public FacetteAttribute(Type sourceType, params string[] exclude)
    {
        SourceType = sourceType;
        Exclude = exclude;
    }

    /// <summary>
    /// Gets the source type that this DTO maps from.
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// Gets the list of source property names to exclude from mapping.
    /// Cannot be combined with <see cref="Include"/>.
    /// </summary>
    public string[] Exclude { get; }

    /// <summary>
    /// Gets or sets an explicit whitelist of source property names to include.
    /// When set, only these properties are mapped. Cannot be combined with <see cref="Exclude"/>.
    /// </summary>
    public string[]? Include { get; set; }

    /// <summary>
    /// Gets or sets whether to generate a <c>ToSource()</c> method for reverse mapping.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool GenerateToSource { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate a <c>Projection</c> expression for LINQ/EF Core queries.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool GenerateProjection { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate a static mapper extension class with
    /// <c>ToDto()</c>, <c>ToSource()</c>, and <c>ProjectToDto()</c> extension methods.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool GenerateMapper { get; set; } = true;

    /// <summary>
    /// Gets or sets explicit nested DTO types to resolve ambiguity when multiple DTOs
    /// map from the same nested source type.
    /// </summary>
    public Type[]? NestedDtos { get; set; }

    /// <summary>
    /// Gets or sets how nullable annotations are applied to generated properties.
    /// Defaults to <see cref="NullableMode.Auto"/>.
    /// </summary>
    public NullableMode NullableMode { get; set; } = NullableMode.Auto;

    /// <summary>
    /// Gets or sets whether to copy data annotation attributes (e.g. validation attributes)
    /// from source properties to generated DTO properties. Defaults to <c>false</c>.
    /// </summary>
    public bool CopyAttributes { get; set; } = false;

    /// <summary>
    /// Gets or sets a CRUD preset that configures multiple options at once.
    /// Defaults to <see cref="FacettePreset.Default"/>.
    /// </summary>
    public FacettePreset Preset { get; set; } = FacettePreset.Default;
}
