namespace Facette.Abstractions;

[AttributeUsage(AttributeTargets.Property)]
public sealed class MapWhenAttribute : Attribute
{
    public MapWhenAttribute(string conditionMethodName)
    {
        ConditionMethodName = conditionMethodName;
    }

    public string ConditionMethodName { get; }
}
