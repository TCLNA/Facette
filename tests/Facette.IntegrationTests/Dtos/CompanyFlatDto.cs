using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(Company), NestedDtos = new[] { typeof(AddressDto) }, GenerateMapper = false)]
public partial record CompanyFlatDto
{
    // Convention-based flattening
    public string HeadquartersCity { get; init; } = "";

    // Dot-notation flattening
    [MapFrom("Headquarters.ZipCode")]
    public string HqZip { get; init; } = "";
}
