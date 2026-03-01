using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

[Facette(typeof(OrderItem))]
public partial record OrderItemDto;
