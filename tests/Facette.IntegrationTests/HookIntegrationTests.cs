using Facette.IntegrationTests.Dtos;
using Facette.IntegrationTests.Models;
using Xunit;

namespace Facette.IntegrationTests;

public class HookIntegrationTests
{
    [Fact]
    public void OnAfterFromSource_IsCalled()
    {
        var address = new Address { Street = "123 Main", City = "Portland", ZipCode = "97201" };
        var dto = AddressWithHookDto.FromSource(address);

        // The hook should set FullAddress to "123 Main, Portland 97201"
        Assert.Equal("123 Main, Portland 97201", dto.FullAddress);
    }

    [Fact]
    public void OnBeforeToSource_IsCalled()
    {
        var dto = new AddressWithHookDto
        {
            Street = "456 Oak",
            City = "Seattle",
            ZipCode = "98101",
            FullAddress = "ignored"
        };

        var source = dto.ToSource();

        Assert.Equal("456 Oak", source.Street);
        Assert.Equal("Seattle", source.City);
    }
}
