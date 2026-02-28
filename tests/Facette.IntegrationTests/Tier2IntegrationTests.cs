using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class Tier2IntegrationTests
{
    private static Company CreateTestCompany() => new Company
    {
        Id = 1,
        Name = "Acme Corp",
        Headquarters = new Address { Street = "100 Main St", City = "Portland", ZipCode = "97201" },
        FoundedAt = new DateTime(2020, 6, 15)
    };

    // --- FacetteIgnore ---

    [Fact]
    public void FacetteIgnore_PropertyNotMappedFromSource()
    {
        var company = CreateTestCompany();
        var dto = CompanyDto.FromSource(company);

        // DisplayLabel should remain at its default (not mapped from source)
        Assert.Equal("", dto.DisplayLabel);
    }

    [Fact]
    public void FacetteIgnore_PropertyNotMappedToSource()
    {
        var company = CreateTestCompany();
        var dto = CompanyDto.FromSource(company);
        // Set DisplayLabel manually
        dto = dto with { DisplayLabel = "Test Label" };
        var roundTripped = dto.ToSource();

        // Company has no DisplayLabel — just verify roundtrip doesn't crash
        Assert.Equal(1, roundTripped.Id);
    }

    // --- Convention-based flattening ---

    [Fact]
    public void ConventionFlattening_MapsNestedProperty()
    {
        var company = CreateTestCompany();
        var dto = CompanyDto.FromSource(company);

        Assert.Equal("Portland", dto.HeadquartersCity);
    }

    // --- Dot-notation MapFrom flattening ---

    [Fact]
    public void DotNotationMapFrom_MapsNestedProperty()
    {
        var company = CreateTestCompany();
        var dto = CompanyDto.FromSource(company);

        Assert.Equal("97201", dto.HqZip);
    }

    // --- Value conversions ---

    [Fact]
    public void ValueConversion_Convert_InFromSource()
    {
        var company = CreateTestCompany();
        var dto = CompanyDto.FromSource(company);

        Assert.Equal("2020-06-15", dto.Founded);
    }

    [Fact]
    public void ValueConversion_ConvertBack_InToSource()
    {
        var company = CreateTestCompany();
        var dto = CompanyDto.FromSource(company);
        var roundTripped = dto.ToSource();

        Assert.Equal(new DateTime(2020, 6, 15), roundTripped.FoundedAt);
    }

    // --- Projection ---

    [Fact]
    public void Projection_CompilesAndExecutes_WithAllTier2Features()
    {
        var projection = CompanyDto.Projection;
        Assert.NotNull(projection);

        var func = projection.Compile();
        var company = CreateTestCompany();
        var dto = func(company);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Acme Corp", dto.Name);
        Assert.Equal("Portland", dto.HeadquartersCity);
        Assert.Equal("97201", dto.HqZip);
        Assert.Equal("2020-06-15", dto.Founded);
    }

    // --- Multi-level projection inlining ---

    [Fact]
    public void MultiLevelProjection_NestedDtoProperties_InlinedCorrectly()
    {
        // OrderDto has ShippingAddress (nested AddressDto) which has its own properties
        // This tests that the projection correctly inlines multi-level nesting
        var projection = OrderDto.Projection;
        Assert.NotNull(projection);

        var func = projection.Compile();
        var order = new Order
        {
            Id = 42,
            OrderDate = DateTime.Now,
            ShippingAddress = new Address { Street = "123 Elm", City = "Seattle", ZipCode = "98101" },
            Items = new List<OrderItem>
            {
                new() { ProductId = 1, ProductName = "Widget", Price = 5.99m, Quantity = 3 }
            }
        };

        var dto = func(order);
        Assert.Equal(42, dto.Id);
        Assert.Equal("123 Elm", dto.ShippingAddress.Street);
        Assert.Equal("Seattle", dto.ShippingAddress.City);
        Assert.Single(dto.Items);
        Assert.Equal("Widget", dto.Items[0].ProductName);
    }

    // --- Mapper extension methods ---

    [Fact]
    public void Mapper_ToDto_WorksWithTier2Features()
    {
        var company = CreateTestCompany();
        var dto = company.ToDto();

        Assert.Equal("Acme Corp", dto.Name);
        Assert.Equal("Portland", dto.HeadquartersCity);
        Assert.Equal("2020-06-15", dto.Founded);
    }
}
