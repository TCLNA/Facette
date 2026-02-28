using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class MultiSourceIntegrationTests
{
    [Fact]
    public void MultiSource_FromSource_MapsBothSources()
    {
        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@test.com",
            PasswordHash = "hash",
            HomeAddress = new Address { Street = "123 Main", City = "Portland", ZipCode = "97201" }
        };

        var profile = new UserProfile
        {
            Bio = "Software developer",
            AvatarUrl = "https://example.com/avatar.jpg"
        };

        var dto = UserDetailDto.FromSource(user, profile);

        // Primary source properties
        Assert.Equal(1, dto.Id);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("alice@test.com", dto.Email);

        // Additional source properties (prefixed)
        Assert.Equal("Software developer", dto.ProfileBio);
        Assert.Equal("https://example.com/avatar.jpg", dto.ProfileAvatarUrl);
    }

    [Fact]
    public void MultiSource_ToSource_OnlyMapsPrimarySource()
    {
        var user = new User
        {
            Id = 2,
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob@test.com",
            PasswordHash = "hash",
            HomeAddress = new Address { Street = "456 Oak", City = "Seattle", ZipCode = "98101" }
        };

        var profile = new UserProfile { Bio = "Designer", AvatarUrl = "https://example.com/bob.jpg" };

        var dto = UserDetailDto.FromSource(user, profile);
        var roundTripped = dto.ToSource();

        Assert.Equal(2, roundTripped.Id);
        Assert.Equal("Bob", roundTripped.FirstName);
        Assert.Equal("bob@test.com", roundTripped.Email);
    }
}
