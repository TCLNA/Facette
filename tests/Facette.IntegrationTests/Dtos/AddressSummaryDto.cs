using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(Address), Include = new[] { "City", "ZipCode" })]
public partial record AddressSummaryDto;
