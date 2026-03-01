namespace Facette.Abstractions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class FacetteAttribute : Attribute
{
    public FacetteAttribute(Type sourceType, params string[] exclude)
    {
        SourceType = sourceType;
        Exclude = exclude;
    }

    public Type SourceType { get; }
    public string[] Exclude { get; }
    public string[]? Include { get; set; }
    public bool GenerateToSource { get; set; } = true;
    public bool GenerateProjection { get; set; } = true;
    public bool GenerateMapper { get; set; } = true;
    public Type[]? NestedDtos { get; set; }
    public NullableMode NullableMode { get; set; } = NullableMode.Auto;
    public bool CopyAttributes { get; set; } = false;
    public FacettePreset Preset { get; set; } = FacettePreset.Default;
}
