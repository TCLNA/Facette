using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class NestedDtoOverrideIntegrationTests
{
    [Fact]
    public void NestedDtos_Override_UsesSpecifiedDto_FromSource()
    {
        // UserDto specifies NestedDtos = [AddressDto] to resolve ambiguity
        // (both AddressDto and AddressSummaryDto target Address)
        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@test.com",
            PasswordHash = "hash",
            HomeAddress = new Address { Street = "123 Main", City = "Portland", ZipCode = "97201" }
        };

        var dto = UserDto.FromSource(user);

        // AddressDto has Street, City, ZipCode
        Assert.Equal("123 Main", dto.HomeAddress!.Street);
        Assert.Equal("Portland", dto.HomeAddress.City);
        Assert.Equal("97201", dto.HomeAddress.ZipCode);
    }

    [Fact]
    public void NestedDtos_Override_UsesSpecifiedDto_ToSource()
    {
        var user = new User
        {
            Id = 1,
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob@test.com",
            PasswordHash = "hash",
            HomeAddress = new Address { Street = "456 Oak", City = "Seattle", ZipCode = "98101" }
        };

        var dto = UserDto.FromSource(user);
        var roundTripped = dto.ToSource();

        Assert.Equal("456 Oak", roundTripped.HomeAddress!.Street);
    }

    [Fact]
    public void NestedDtos_Override_UsesSpecifiedDto_Projection()
    {
        var projection = UserDto.Projection;
        Assert.NotNull(projection);

        var func = projection.Compile();
        var user = new User
        {
            Id = 1,
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com",
            PasswordHash = "hash",
            HomeAddress = new Address { Street = "Test St", City = "Test City", ZipCode = "00000" }
        };

        var dto = func(user);
        Assert.Equal("Test St", dto.HomeAddress!.Street);
        Assert.Equal("Test City", dto.HomeAddress.City);
    }

    [Fact]
    public void AddressSummaryDto_HasOnlyIncludedProperties()
    {
        var address = new Address { Street = "123 Main", City = "Portland", ZipCode = "97201" };
        var dto = AddressSummaryDto.FromSource(address);

        Assert.Equal("Portland", dto.City);
        Assert.Equal("97201", dto.ZipCode);
    }
}
