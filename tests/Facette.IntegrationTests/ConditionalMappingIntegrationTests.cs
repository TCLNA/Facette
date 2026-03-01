using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class ConditionalMappingIntegrationTests
{
    [Fact]
    public void MapWhen_True_MapsProperty()
    {
        ConditionalUserDto.SetShouldMapEmail(true);

        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@test.com",
            PasswordHash = "hash",
            CreatedAt = new DateTime(2024, 1, 1)
        };

        var dto = ConditionalUserDto.FromSource(user);

        Assert.Equal("alice@test.com", dto.Email);
    }

    [Fact]
    public void MapWhen_False_UsesDefault()
    {
        ConditionalUserDto.SetShouldMapEmail(false);

        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@test.com",
            PasswordHash = "hash",
            CreatedAt = new DateTime(2024, 1, 1)
        };

        var dto = ConditionalUserDto.FromSource(user);

        Assert.Null(dto.Email);

        // Reset for other tests
        ConditionalUserDto.SetShouldMapEmail(true);
    }
}
