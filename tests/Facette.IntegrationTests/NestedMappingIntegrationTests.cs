using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class NestedMappingIntegrationTests
{
    [Fact]
    public void NestedDto_FromSource_MapsNestedObject()
    {
        var order = new Order
        {
            Id = 1,
            OrderDate = new DateTime(2026, 1, 1),
            ShippingAddress = new Address { Street = "123 Main", City = "Portland", ZipCode = "97201" },
            Items = new List<OrderItem>
            {
                new() { ProductId = 10, ProductName = "Widget", Price = 9.99m, Quantity = 2 },
                new() { ProductId = 20, ProductName = "Gadget", Price = 19.99m, Quantity = 1 }
            }
        };

        var dto = OrderDto.FromSource(order);

        Assert.Equal(1, dto.Id);
        Assert.Equal("123 Main", dto.ShippingAddress.Street);
        Assert.Equal("Portland", dto.ShippingAddress.City);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal("Widget", dto.Items[0].ProductName);
        Assert.Equal(19.99m, dto.Items[1].Price);
    }

    [Fact]
    public void NestedDto_ToSource_RoundTrips()
    {
        var order = new Order
        {
            Id = 1,
            OrderDate = new DateTime(2026, 1, 1),
            ShippingAddress = new Address { Street = "123 Main", City = "Portland", ZipCode = "97201" },
            Items = new List<OrderItem>
            {
                new() { ProductId = 10, ProductName = "Widget", Price = 9.99m, Quantity = 2 }
            }
        };

        var dto = OrderDto.FromSource(order);
        var roundTripped = dto.ToSource();

        Assert.Equal(order.Id, roundTripped.Id);
        Assert.Equal(order.ShippingAddress.Street, roundTripped.ShippingAddress.Street);
        Assert.Equal(order.Items[0].ProductId, roundTripped.Items[0].ProductId);
    }

    [Fact]
    public void NullableNestedDto_FromSource_HandlesNull()
    {
        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@test.com",
            PasswordHash = "hash",
            HomeAddress = null
        };

        var dto = UserDto.FromSource(user);

        Assert.Null(dto.HomeAddress);
    }

    [Fact]
    public void NullableNestedDto_FromSource_HandlesNonNull()
    {
        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@test.com",
            PasswordHash = "hash",
            HomeAddress = new Address { Street = "456 Oak", City = "Seattle", ZipCode = "98101" }
        };

        var dto = UserDto.FromSource(user);

        Assert.NotNull(dto.HomeAddress);
        Assert.Equal("456 Oak", dto.HomeAddress!.Street);
    }

    [Fact]
    public void Projection_CompilesWithNestedAndCollectionExpressions()
    {
        var projection = OrderDto.Projection;
        Assert.NotNull(projection);

        var func = projection.Compile();
        var order = new Order
        {
            Id = 1,
            OrderDate = DateTime.Now,
            ShippingAddress = new Address { Street = "Test", City = "Test", ZipCode = "00000" },
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, ProductName = "X", Price = 1m, Quantity = 1 }
            }
        };

        var dto = func(order);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Test", dto.ShippingAddress.Street);
        Assert.Single(dto.Items);
    }

    [Fact]
    public void SimpleCollection_ArrayRoundTrips()
    {
        var user = new User
        {
            Id = 1,
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob@test.com",
            PasswordHash = "hash",
            Tags = new[] { "admin", "user" }
        };

        var dto = UserDto.FromSource(user);
        Assert.Equal(2, dto.Tags.Length);
        Assert.Equal("admin", dto.Tags[0]);

        var roundTripped = dto.ToSource();
        Assert.Equal(2, roundTripped.Tags.Length);
        Assert.Equal("user", roundTripped.Tags[1]);
    }
}
