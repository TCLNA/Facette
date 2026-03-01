using System.Linq.Expressions;
using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class ExpressionMappingIntegrationTests
{
    [Fact]
    public void MapExpression_RewritesPredicate()
    {
        // Create a predicate over the DTO type
        Expression<Func<UserDto, bool>> dtoPredicate = dto => dto.FirstName == "Alice";

        // MapExpression should rewrite it to a predicate over User
        var sourcePredicate = UserDto.MapExpression(dtoPredicate);

        Assert.NotNull(sourcePredicate);
        Assert.Equal(typeof(Func<User, bool>), sourcePredicate.Type);

        // Compile and test
        var compiled = sourcePredicate.Compile();
        var alice = new User { Id = 1, FirstName = "Alice", LastName = "Smith", Email = "a@b.com", PasswordHash = "h", CreatedAt = DateTime.Now };
        var bob = new User { Id = 2, FirstName = "Bob", LastName = "Jones", Email = "b@b.com", PasswordHash = "h", CreatedAt = DateTime.Now };

        Assert.True(compiled(alice));
        Assert.False(compiled(bob));
    }

    [Fact]
    public void MapExpression_WorksWithLinqWhere()
    {
        var users = new List<User>
        {
            new User { Id = 1, FirstName = "Alice", LastName = "Smith", Email = "a@b.com", PasswordHash = "h", CreatedAt = DateTime.Now },
            new User { Id = 2, FirstName = "Bob", LastName = "Jones", Email = "b@b.com", PasswordHash = "h", CreatedAt = DateTime.Now },
            new User { Id = 3, FirstName = "Alice", LastName = "Brown", Email = "c@b.com", PasswordHash = "h", CreatedAt = DateTime.Now }
        };

        Expression<Func<UserDto, bool>> dtoPredicate = dto => dto.FirstName == "Alice";
        var sourcePredicate = UserDto.MapExpression(dtoPredicate);

        var result = users.AsQueryable().Where(sourcePredicate).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, u => Assert.Equal("Alice", u.FirstName));
    }

    [Fact]
    public void MapExpression_IntProjection_Works()
    {
        // Map an int-returning expression
        Expression<Func<UserDto, int>> dtoExpr = dto => dto.Id;
        var sourceExpr = UserDto.MapExpression(dtoExpr);

        var compiled = sourceExpr.Compile();
        var user = new User { Id = 42, FirstName = "Alice", LastName = "Smith", Email = "a@b.com", PasswordHash = "h", CreatedAt = DateTime.Now };

        Assert.Equal(42, compiled(user));
    }

    [Fact]
    public void WhereDto_MapperExtension_Works()
    {
        var users = new List<User>
        {
            new User { Id = 1, FirstName = "Alice", LastName = "Smith", Email = "a@b.com", PasswordHash = "h", CreatedAt = DateTime.Now },
            new User { Id = 2, FirstName = "Bob", LastName = "Jones", Email = "b@b.com", PasswordHash = "h", CreatedAt = DateTime.Now },
        };

        Expression<Func<UserDto, bool>> predicate = dto => dto.Id == 1;
        var result = users.AsQueryable().WhereDto(predicate).ToList();

        Assert.Single(result);
        Assert.Equal("Alice", result[0].FirstName);
    }
}
