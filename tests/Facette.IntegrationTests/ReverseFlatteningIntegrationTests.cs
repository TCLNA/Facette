using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class ReverseFlatteningIntegrationTests
{
    [Fact]
    public void ConventionFlattening_RoundTrips_ViaToSource()
    {
        var company = new Company
        {
            Id = 1,
            Name = "Acme Corp",
            FoundedAt = new System.DateTime(2020, 1, 1),
            Headquarters = new Address { Street = "123 Main", City = "Portland", ZipCode = "97201" }
        };

        var dto = CompanyFlatDto.FromSource(company);
        Assert.Equal("Portland", dto.HeadquartersCity);

        var roundTripped = dto.ToSource();
        Assert.NotNull(roundTripped.Headquarters);
        Assert.Equal("Portland", roundTripped.Headquarters.City);
        Assert.Equal("97201", roundTripped.Headquarters.ZipCode);
    }

    [Fact]
    public void DotNotationFlattening_RoundTrips_ViaToSource()
    {
        var company = new Company
        {
            Id = 2,
            Name = "Test Inc",
            FoundedAt = new System.DateTime(2021, 6, 15),
            Headquarters = new Address { Street = "456 Oak", City = "Seattle", ZipCode = "98101" }
        };

        var dto = CompanyFlatDto.FromSource(company);
        Assert.Equal("98101", dto.HqZip);

        var roundTripped = dto.ToSource();
        Assert.NotNull(roundTripped.Headquarters);
        Assert.Equal("98101", roundTripped.Headquarters.ZipCode);
    }
}
