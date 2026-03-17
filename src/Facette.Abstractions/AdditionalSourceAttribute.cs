namespace Facette.Abstractions;

/// <summary>
/// Specifies an additional source type to include in the <c>FromSource</c> mapping.
/// Properties from this source are matched by name (with an optional prefix) to DTO properties.
/// Multiple additional sources can be applied to the same DTO.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AdditionalSourceAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdditionalSourceAttribute"/> class.
    /// </summary>
    /// <param name="sourceType">The additional source type to map from.</param>
    /// <param name="prefix">
    /// An optional prefix for matching DTO property names to source properties.
    /// For example, a prefix of <c>"Billing"</c> maps source property <c>Address</c>
    /// to DTO property <c>BillingAddress</c>.
    /// </param>
    public AdditionalSourceAttribute(Type sourceType, string prefix = "")
    {
        SourceType = sourceType;
        Prefix = prefix;
    }

    /// <summary>
    /// Gets the additional source type to map from.
    /// </summary>
    public Type SourceType { get; }

    /// <summary>
    /// Gets the prefix used when matching DTO property names to source properties.
    /// </summary>
    public string Prefix { get; }
}
