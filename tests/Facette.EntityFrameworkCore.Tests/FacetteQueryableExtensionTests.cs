using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Facette.EntityFrameworkCore.Tests;

public class FacetteQueryableExtensionTests : IDisposable
{
    private readonly TestDbContext _db;

    public FacetteQueryableExtensionTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new TestDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _db.Users.AddRange(
            new TestUser { Id = 1, Name = "Alice", Email = "alice@test.com", Age = 30 },
            new TestUser { Id = 2, Name = "Bob", Email = "bob@test.com", Age = 25 },
            new TestUser { Id = 3, Name = "Alice", Email = "alice2@test.com", Age = 35 }
        );
        _db.SaveChanges();
    }

    [Fact]
    public void ProjectToFacette_ReturnsProjectedDtos()
    {
        var dtos = _db.Users
            .ProjectToFacette(TestUserDto.Projection)
            .ToList();

        Assert.Equal(3, dtos.Count);
        Assert.All(dtos, dto =>
        {
            Assert.True(dto.Id > 0);
            Assert.NotNull(dto.Name);
        });
    }

    [Fact]
    public void ProjectToFacette_PreservesData()
    {
        var dto = _db.Users
            .ProjectToFacette(TestUserDto.Projection)
            .First(d => d.Id == 1);

        Assert.Equal("Alice", dto.Name);
        Assert.Equal("alice@test.com", dto.Email);
        Assert.Equal(30, dto.Age);
    }

    [Fact]
    public void WhereFacette_FiltersCorrectly()
    {
        Expression<Func<TestUserDto, bool>> predicate = dto => dto.Name == "Alice";

        var users = _db.Users.AsQueryable()
            .WhereFacette(predicate, TestUserDto.MapExpression)
            .ToList();

        Assert.Equal(2, users.Count);
        Assert.All(users, u => Assert.Equal("Alice", u.Name));
    }

    [Fact]
    public void WhereFacette_WithComplexPredicate()
    {
        Expression<Func<TestUserDto, bool>> predicate = dto => dto.Age > 28;

        var users = _db.Users.AsQueryable()
            .WhereFacette(predicate, TestUserDto.MapExpression)
            .ToList();

        Assert.Equal(2, users.Count);
        Assert.All(users, u => Assert.True(u.Age > 28));
    }

    [Fact]
    public void ProjectToFacette_DbSet_Works()
    {
        var dtos = _db.Users
            .ProjectToFacette(TestUserDto.Projection)
            .Where(d => d.Id == 2)
            .ToList();

        Assert.Single(dtos);
        Assert.Equal("Bob", dtos[0].Name);
    }

    [Fact]
    public void WhereFacette_DbSet_Works()
    {
        Expression<Func<TestUserDto, bool>> predicate = dto => dto.Email == "bob@test.com";

        var users = _db.Users
            .WhereFacette(predicate, TestUserDto.MapExpression)
            .ToList();

        Assert.Single(users);
        Assert.Equal("Bob", users[0].Name);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }
}
