using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class NullableModeIntegrationTests
{
    [Fact]
    public void AllNullable_FromSource_MapsCorrectly()
    {
        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@test.com",
            PasswordHash = "hash",
            CreatedAt = new DateTime(2024, 1, 1)
        };

        var dto = NullableUserDto.FromSource(user);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Alice", dto.FirstName);
    }

    [Fact]
    public void AllNullable_PropertiesAcceptNull()
    {
        // NullableUserDto should allow null values for all properties
        var dto = new NullableUserDto
        {
            Id = null,
            FirstName = null,
            LastName = null,
            Email = null,
            CreatedAt = null
        };

        Assert.Null(dto.Id);
        Assert.Null(dto.FirstName);
    }
}
