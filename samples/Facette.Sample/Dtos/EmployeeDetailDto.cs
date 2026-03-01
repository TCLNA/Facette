using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

/// Demonstrates: Multi-source mapping — combines Employee + EmployeeProfile
[Facette(typeof(Employee), "SocialSecurityNumber",
    NestedDtos = new[] { typeof(AddressDto) },
    GenerateProjection = false)]
[AdditionalSource(typeof(EmployeeProfile), "Profile")]
public partial record EmployeeDetailDto;
