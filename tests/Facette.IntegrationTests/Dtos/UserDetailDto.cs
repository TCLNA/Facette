using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(User), "PasswordHash", NestedDtos = new[] { typeof(AddressDto) }, GenerateProjection = false)]
[AdditionalSource(typeof(UserProfile), "Profile")]
public partial record UserDetailDto;
