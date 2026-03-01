using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class PresetIntegrationTests
{
    [Fact]
    public void Create_FromSource_ExcludesId()
    {
        var user = new User
        {
            Id = 42,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@test.com",
            PasswordHash = "hash",
            CreatedAt = new DateTime(2024, 1, 1)
        };

        var dto = CreateUserDto.FromSource(user);

        // Id should be excluded, but other properties should be mapped
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);

        // CreateUserDto should NOT have an Id property
        var idProp = typeof(CreateUserDto).GetProperty("Id");
        Assert.Null(idProp);
    }

    [Fact]
    public void Create_HasNoProjection()
    {
        // Create preset should not generate Projection
        var projectionProp = typeof(CreateUserDto).GetProperty("Projection",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.Null(projectionProp);
    }
}
