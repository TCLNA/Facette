using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class MappingIntegrationTests
{
    [Fact]
    public void FromSource_MapsAllIncludedProperties()
    {
        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            PasswordHash = "secret_hash",
            CreatedAt = new DateTime(2024, 1, 15)
        };

        var dto = UserDto.FromSource(user);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);
        Assert.Equal("alice@example.com", dto.Email);
        Assert.Equal(new DateTime(2024, 1, 15), dto.CreatedAt);
    }

    [Fact]
    public void ToSource_MapsBack()
    {
        var dto = new UserDto
        {
            Id = 2,
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob@example.com",
            CreatedAt = new DateTime(2024, 6, 1)
        };

        var user = dto.ToSource();

        Assert.Equal(2, user.Id);
        Assert.Equal("Bob", user.FirstName);
        Assert.Equal("Jones", user.LastName);
        Assert.Equal("bob@example.com", user.Email);
    }

    [Fact]
    public void ExtensionMethod_ToDto_Works()
    {
        var user = new User
        {
            Id = 3,
            FirstName = "Carol",
            LastName = "Lee",
            Email = "carol@example.com",
            PasswordHash = "hash123",
            CreatedAt = DateTime.UtcNow
        };

        var dto = user.ToDto();

        Assert.Equal(3, dto.Id);
        Assert.Equal("Carol", dto.FirstName);
    }

    [Fact]
    public void Projection_CanBeCompiled()
    {
        var projection = UserDto.Projection;
        var compiled = projection.Compile();

        var user = new User
        {
            Id = 4,
            FirstName = "Dave",
            LastName = "Kim",
            Email = "dave@example.com",
            PasswordHash = "hash456",
            CreatedAt = DateTime.UtcNow
        };

        var dto = compiled(user);

        Assert.Equal(4, dto.Id);
        Assert.Equal("Dave", dto.FirstName);
    }

    [Fact]
    public void ProjectToDto_WorksWithQueryable()
    {
        var users = new List<User>
        {
            new() { Id = 1, FirstName = "A", LastName = "B", Email = "a@b.com", PasswordHash = "h", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, FirstName = "C", LastName = "D", Email = "c@d.com", PasswordHash = "h", CreatedAt = DateTime.UtcNow }
        };

        var dtos = users.AsQueryable().ProjectToDto().ToList();

        Assert.Equal(2, dtos.Count);
        Assert.Equal("A", dtos[0].FirstName);
        Assert.Equal("C", dtos[1].FirstName);
    }

    [Fact]
    public void ExcludedProperty_NotOnDto()
    {
        var props = typeof(UserDto).GetProperties();
        Assert.DoesNotContain(props, p => p.Name == "PasswordHash");
    }
}
