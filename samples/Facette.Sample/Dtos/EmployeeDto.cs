using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

/// Demonstrates: exclusion, enum→string conversion, value conversion,
/// copy attributes from source, nested DTO, AfterMap hook
[Facette(typeof(Employee), "SocialSecurityNumber",
    NestedDtos = new[] { typeof(AddressDto) },
    CopyAttributes = true)]
public partial record EmployeeDto
{
    // Enum → string conversion (Department enum becomes a string)
    [MapFrom("Department")]
    public string DepartmentName { get; init; } = "";

    // Value conversion: DateTime → string with custom converter
    [MapFrom("HireDate", Convert = nameof(FormatDate), ConvertBack = nameof(ParseDate))]
    public string HiredOn { get; init; } = "";

    // Ignored computed property — filled by AfterMap hook
    [FacetteIgnore]
    public string FullName { get; set; } = "";

    public static string FormatDate(DateTime dt) => dt.ToString("yyyy-MM-dd");
    public static DateTime ParseDate(string s) => DateTime.Parse(s);

    // AfterMap hook to compute FullName after mapping
    partial void OnAfterFromSource(Employee source)
    {
        FullName = $"{source.FirstName} {source.LastName}";
    }
}
