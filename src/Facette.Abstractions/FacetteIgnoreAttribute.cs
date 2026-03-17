namespace Facette.Abstractions;

/// <summary>
/// Excludes a property from Facette mapping code generation.
/// The property will not appear in <c>FromSource</c>, <c>ToSource</c>, or <c>Projection</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class FacetteIgnoreAttribute : Attribute
{
}
