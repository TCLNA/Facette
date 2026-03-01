using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(User), Preset = FacettePreset.Create, NestedDtos = new[] { typeof(AddressDto) })]
public partial record CreateUserDto;
