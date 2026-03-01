using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

/// Demonstrates: Include filter, convention flattening, MapFrom with dot-notation
[Facette(typeof(Employee),
    Include = new[] { "Id", "FirstName", "LastName", "Email", "HomeAddress" },
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record EmployeeSummaryDto
{
    // Convention flattening: HomeAddress.City → HomeAddressCity
    public string HomeAddressCity { get; init; } = "";

    // Explicit dot-notation flattening with MapFrom
    [MapFrom("HomeAddress.State")]
    public string State { get; init; } = "";
}
