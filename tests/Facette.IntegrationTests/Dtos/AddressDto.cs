using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(Address))]
public partial record AddressDto;
