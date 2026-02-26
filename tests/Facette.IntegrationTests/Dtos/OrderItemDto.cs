using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(OrderItem))]
public partial record OrderItemDto;
