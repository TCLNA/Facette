using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(Order))]
public partial record OrderDto;
