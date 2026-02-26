using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

[Facette(typeof(Product), "InternalSku")]
public partial record ProductDto;
