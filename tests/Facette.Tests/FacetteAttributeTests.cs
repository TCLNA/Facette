using Facette.Abstractions;

namespace Facette.Tests;

public class FacetteAttributeTests
{
    [Fact]
    public void Constructor_SetsSourceType()
    {
        var attr = new FacetteAttribute(typeof(string));
        Assert.Equal(typeof(string), attr.SourceType);
    }

    [Fact]
    public void Constructor_SetsExclude()
    {
        var attr = new FacetteAttribute(typeof(string), "A", "B");
        Assert.Equal(new[] { "A", "B" }, attr.Exclude);
    }

    [Fact]
    public void Constructor_DefaultExcludeIsEmpty()
    {
        var attr = new FacetteAttribute(typeof(string));
        Assert.Empty(attr.Exclude);
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var attr = new FacetteAttribute(typeof(string));
        Assert.Null(attr.Include);
        Assert.True(attr.GenerateToSource);
        Assert.True(attr.GenerateProjection);
        Assert.True(attr.GenerateMapper);
    }
}
