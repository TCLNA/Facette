using Facette.Tests.Helpers;

namespace Facette.Tests;

public class MappingTests
{
    [Fact]
    public void Generator_GeneratesFromSourceMethod()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public decimal Price { get; set; }
            }

            [Facette(typeof(Product))]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("public static ProductDto FromSource(", generatedCode);
        Assert.Contains("Id = source.Id", generatedCode);
        Assert.Contains("Name = source.Name", generatedCode);
        Assert.Contains("Price = source.Price", generatedCode);
    }

    [Fact]
    public void Generator_GeneratesToSourceMethod()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Product))]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("ToSource()", generatedCode);
        Assert.Contains("Id = this.Id", generatedCode);
        Assert.Contains("Name = this.Name", generatedCode);
    }

    [Fact]
    public void Generator_WithGenerateToSourceFalse_OmitsToSource()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Product
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Product), GenerateToSource = false)]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("FromSource", generatedCode);
        Assert.DoesNotContain("ToSource", generatedCode);
    }
}
