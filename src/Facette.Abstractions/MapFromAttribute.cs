namespace Facette.Abstractions;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class MapFromAttribute : Attribute
{
    public MapFromAttribute(string sourcePropertyName)
    {
        SourcePropertyName = sourcePropertyName;
    }

    public MapFromAttribute()
    {
        SourcePropertyName = null;
    }

    public string? SourcePropertyName { get; }
    public string? Convert { get; set; }
    public string? ConvertBack { get; set; }
}
