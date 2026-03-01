using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

[Facette(typeof(Address))]
public partial record AddressDto;
