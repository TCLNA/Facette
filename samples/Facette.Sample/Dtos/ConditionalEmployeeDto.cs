using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

/// Demonstrates: Conditional mapping with [MapWhen] — salary only mapped when authorized
[Facette(typeof(Employee), "SocialSecurityNumber",
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record ConditionalEmployeeDto
{
    private static bool _includeSalary = true;

    [MapWhen(nameof(ShouldIncludeSalary))]
    public decimal Salary { get; init; }

    public static bool ShouldIncludeSalary() => _includeSalary;
    public static void SetIncludeSalary(bool value) => _includeSalary = value;
}
