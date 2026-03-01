using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

/// Demonstrates: NullableMode — all properties become nullable (useful for PATCH)
[Facette(typeof(Employee), "SocialSecurityNumber",
    NullableMode = NullableMode.AllNullable,
    NestedDtos = new[] { typeof(AddressDto) })]
public partial record NullableEmployeeDto;
