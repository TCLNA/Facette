using Facette.Abstractions;

namespace Facette.EntityFrameworkCore.Tests;

[Facette(typeof(TestUser))]
public partial record TestUserDto;
