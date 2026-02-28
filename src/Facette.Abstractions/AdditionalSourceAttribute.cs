namespace Facette.Abstractions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AdditionalSourceAttribute : Attribute
{
    public AdditionalSourceAttribute(Type sourceType, string prefix = "")
    {
        SourceType = sourceType;
        Prefix = prefix;
    }

    public Type SourceType { get; }
    public string Prefix { get; }
}
