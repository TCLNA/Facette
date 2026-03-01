using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

/// Demonstrates: Nested collections, nested objects, enum→string conversion,
/// expression mapping for query rewriting
[Facette(typeof(Order), NestedDtos = new[] { typeof(AddressDto), typeof(OrderItemDto) })]
public partial record OrderDto
{
    [MapFrom("Status")]
    public string StatusText { get; init; } = "";
}
