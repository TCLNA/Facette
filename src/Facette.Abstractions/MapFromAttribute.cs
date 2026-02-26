namespace Facette.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class MapFromAttribute : Attribute
{
    public MapFromAttribute(string sourcePropertyName)
    {
        SourcePropertyName = sourcePropertyName;
    }

    public string SourcePropertyName { get; }
}
