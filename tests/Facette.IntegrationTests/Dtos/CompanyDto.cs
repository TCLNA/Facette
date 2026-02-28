using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(Company))]
public partial record CompanyDto
{
    // FacetteIgnore: this property should not participate in mapping
    [FacetteIgnore]
    public string DisplayLabel { get; init; } = "";

    // Convention-based flattening: HeadquartersCity -> Headquarters.City
    public string HeadquartersCity { get; init; } = "";

    // Dot-notation flattening via MapFrom
    [MapFrom("Headquarters.ZipCode")]
    public string HqZip { get; init; } = "";

    // Value conversion
    [MapFrom("FoundedAt", Convert = nameof(ToIso), ConvertBack = nameof(FromIso))]
    public string Founded { get; init; } = "";

    public static string ToIso(DateTime dt) => dt.ToString("yyyy-MM-dd");
    public static DateTime FromIso(string s) => DateTime.Parse(s);
}
