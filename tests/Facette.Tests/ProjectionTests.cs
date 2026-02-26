using Facette.Tests.Helpers;

namespace Facette.Tests;

public class ProjectionTests
{
    [Fact]
    public void Generator_GeneratesProjectionExpression()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Order
            {
                public int Id { get; set; }
                public string Description { get; set; }
                public decimal Total { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("System.Linq.Expressions.Expression<System.Func<", generatedCode);
        Assert.Contains("Projection =>", generatedCode);
        Assert.Contains("Id = source.Id", generatedCode);
    }

    [Fact]
    public void Generator_WithGenerateProjectionFalse_OmitsProjection()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Order
            {
                public int Id { get; set; }
            }

            [Facette(typeof(Order), GenerateProjection = false)]
            public partial record OrderDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        Assert.DoesNotContain("Projection", generatedCode);
    }
}
