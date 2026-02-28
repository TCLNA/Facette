using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class InheritanceIntegrationTests
{
    [Fact]
    public void BaseDto_FromSource_MapsBaseProperties()
    {
        var entity = new EntityBase { Id = 42, CreatedAt = new DateTime(2024, 1, 1) };
        var dto = EntityBaseDto.FromSource(entity);

        Assert.Equal(42, dto.Id);
        Assert.Equal(new DateTime(2024, 1, 1), dto.CreatedAt);
    }

    [Fact]
    public void DerivedDto_FromSource_MapsAllProperties()
    {
        var product = new Product
        {
            Id = 1,
            CreatedAt = new DateTime(2024, 6, 15),
            Name = "Widget",
            Price = 9.99m
        };

        var dto = ProductDto.FromSource(product);

        Assert.Equal(1, dto.Id);
        Assert.Equal(new DateTime(2024, 6, 15), dto.CreatedAt);
        Assert.Equal("Widget", dto.Name);
        Assert.Equal(9.99m, dto.Price);
    }

    [Fact]
    public void DerivedDto_ToSource_MapsAllProperties()
    {
        var product = new Product
        {
            Id = 2,
            CreatedAt = new DateTime(2024, 3, 10),
            Name = "Gadget",
            Price = 19.99m
        };

        var dto = ProductDto.FromSource(product);
        var roundTripped = dto.ToSource();

        Assert.Equal(2, roundTripped.Id);
        Assert.Equal("Gadget", roundTripped.Name);
        Assert.Equal(19.99m, roundTripped.Price);
    }

    [Fact]
    public void DerivedDto_IsInstanceOfBaseDto()
    {
        var product = new Product
        {
            Id = 3,
            CreatedAt = new DateTime(2024, 1, 1),
            Name = "Test",
            Price = 5m
        };

        var dto = ProductDto.FromSource(product);

        // ProductDto should be castable to EntityBaseDto
        EntityBaseDto baseDto = dto;
        Assert.Equal(3, baseDto.Id);
    }

    [Fact]
    public void DerivedDto_Projection_MapsAllProperties()
    {
        var projection = ProductDto.Projection;
        Assert.NotNull(projection);

        var func = projection.Compile();
        var product = new Product
        {
            Id = 4,
            CreatedAt = new DateTime(2024, 12, 25),
            Name = "Holiday Widget",
            Price = 24.99m
        };

        var dto = func(product);
        Assert.Equal(4, dto.Id);
        Assert.Equal("Holiday Widget", dto.Name);
        Assert.Equal(24.99m, dto.Price);
    }
}
