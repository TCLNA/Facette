using Facette.Abstractions;
using Facette.IntegrationTests.Models;

namespace Facette.IntegrationTests.Dtos;

[Facette(typeof(EntityBase), GenerateMapper = false)]
public partial record EntityBaseDto;
