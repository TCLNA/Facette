using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(Product))]
public partial record ProductDto : EntityBaseDto;
