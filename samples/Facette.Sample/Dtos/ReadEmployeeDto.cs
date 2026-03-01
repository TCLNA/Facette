using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

/// Demonstrates: CRUD preset (Read disables ToSource — read-only DTO)
[Facette(typeof(Employee), "SocialSecurityNumber",
    Preset = FacettePreset.Read,
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record ReadEmployeeDto;
