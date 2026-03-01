using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(Order), NestedDtos = new[] { typeof(AddressDto), typeof(OrderItemDto) })]
public partial record OrderStatusDto
{
    [MapFrom("Status")]
    public string StatusText { get; init; }
}
