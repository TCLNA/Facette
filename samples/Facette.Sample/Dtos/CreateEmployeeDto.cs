using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

/// Demonstrates: CRUD preset (Create excludes Id, disables projection)
[Facette(typeof(Employee), "SocialSecurityNumber",
    Preset = FacettePreset.Create,
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record CreateEmployeeDto;
