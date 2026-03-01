using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(User), NullableMode = NullableMode.AllNullable, NestedDtos = new[] { typeof(AddressDto) })]
public partial record NullableUserDto;
