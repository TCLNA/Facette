using Facette.Abstractions;
using Facette.Sample.Models;

namespace Facette.Sample.Dtos;

[Facette(typeof(Category))]
public partial record CategoryDto;
