using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(User), "PasswordHash")]
public partial record UserDto;
