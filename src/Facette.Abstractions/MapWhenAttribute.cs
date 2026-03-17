namespace Facette.Abstractions;

/// <summary>
/// Conditionally includes a property in the mapping based on a runtime condition.
/// The property is only mapped when the specified method returns <c>true</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MapWhenAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MapWhenAttribute"/> class.
    /// </summary>
    /// <param name="conditionMethodName">
    /// The name of a static parameterless method on the DTO type that returns <c>bool</c>.
    /// </param>
    public MapWhenAttribute(string conditionMethodName)
    {
        ConditionMethodName = conditionMethodName;
    }

    /// <summary>
    /// Gets the name of the static condition method.
    /// The method must have the signature <c>static bool MethodName()</c>.
    /// </summary>
    public string ConditionMethodName { get; }
}
