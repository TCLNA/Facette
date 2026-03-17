namespace Facette.Abstractions;

/// <summary>
/// Overrides the default property mapping by specifying a different source property name
/// and/or custom conversion methods.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class MapFromAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapFromAttribute"/> class
    /// with an explicit source property name.
    /// </summary>
    /// <param name="sourcePropertyName">The name of the source property to map from. Supports dot notation for flattened paths (e.g. <c>"Address.City"</c>).</param>
    public MapFromAttribute(string sourcePropertyName)
    {
        SourcePropertyName = sourcePropertyName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapFromAttribute"/> class
    /// without a source property name, used when only <see cref="Convert"/> or
    /// <see cref="ConvertBack"/> is needed.
    /// </summary>
    public MapFromAttribute()
    {
        SourcePropertyName = null;
    }

    /// <summary>
    /// Gets the source property name to map from, or <c>null</c> if using the default name.
    /// Supports dot notation for flattened paths.
    /// </summary>
    public string? SourcePropertyName { get; }

    /// <summary>
    /// Gets or sets the name of a static method on the DTO type that converts the source
    /// property value to the DTO property value (used in <c>FromSource</c>).
    /// The method must have the signature <c>static TDto_Property MethodName(TSource_Property value)</c>.
    /// </summary>
    public string? Convert { get; set; }

    /// <summary>
    /// Gets or sets the name of a static method on the DTO type that converts the DTO
    /// property value back to the source property value (used in <c>ToSource</c>).
    /// The method must have the signature <c>static TSource_Property MethodName(TDto_Property value)</c>.
    /// </summary>
    public string? ConvertBack { get; set; }
}
