using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(Address))]
public partial record AddressWithHookDto
{
    [FacetteIgnore]
    public string FullAddress { get; set; } = "";

    partial void OnAfterFromSource(Address source)
    {
        FullAddress = $"{source.Street}, {source.City} {source.ZipCode}";
    }
}
