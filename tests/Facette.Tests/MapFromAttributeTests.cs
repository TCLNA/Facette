using Facette.Abstractions;

namespace Facette.Tests;

public class MapFromAttributeTests
{
    [Fact]
    public void MapFromAttribute_StoresSourcePropertyName()
    {
        var attr = new MapFromAttribute("FirstName");
        Assert.Equal("FirstName", attr.SourcePropertyName);
    }
}
