using Microsoft.EntityFrameworkCore;

namespace Facette.EntityFrameworkCore.Tests;

public class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    public DbSet<TestUser> Users { get; set; } = null!;
}
