using Facette.Tests.Helpers;
using Xunit;

namespace Facette.Tests;

public class HookTests
{
    [Fact]
    public void Generator_EmitsPartialHookDeclarations()
    {
        var source = @"
using Facette.Abstractions;

namespace TestNs;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[Facette(typeof(Product))]
public partial record ProductDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("partial void OnAfterFromSource(", generated);
        Assert.Contains("partial void OnBeforeToSource()", generated);
        Assert.Contains("partial void OnAfterToSource(", generated);
    }

    [Fact]
    public void Generator_FromSource_CallsOnAfterFromSource()
    {
        var source = @"
using Facette.Abstractions;

namespace TestNs;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[Facette(typeof(Product))]
public partial record ProductDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ProductDto.g.cs"))
            .GetText().ToString();

        // FromSource should use var result = ... and call OnAfterFromSource
        Assert.Contains("var result = new ProductDto", generated);
        Assert.Contains("result.OnAfterFromSource(source)", generated);
        Assert.Contains("return result;", generated);
    }

    [Fact]
    public void Generator_ToSource_CallsHooks()
    {
        var source = @"
using Facette.Abstractions;

namespace TestNs;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[Facette(typeof(Product))]
public partial record ProductDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("OnBeforeToSource()", generated);
        Assert.Contains("OnAfterToSource(result)", generated);
    }

    [Fact]
    public void Generator_WhenToSourceDisabled_OmitsToSourceHooks()
    {
        var source = @"
using Facette.Abstractions;

namespace TestNs;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[Facette(typeof(Product), GenerateToSource = false)]
public partial record ProductDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("partial void OnAfterFromSource(", generated);
        Assert.DoesNotContain("OnBeforeToSource", generated);
        Assert.DoesNotContain("OnAfterToSource", generated);
    }
}
