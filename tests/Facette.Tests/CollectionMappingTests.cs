using Facette.Tests.Helpers;

namespace Facette.Tests;

public class CollectionMappingTests
{
    [Fact]
    public void Generator_WithListOfNestedDto_GeneratesSelectMapping()
    {
        var source = """
            using Facette.Abstractions;
            using System.Collections.Generic;

            namespace TestApp;

            public class OrderItem
            {
                public int ProductId { get; set; }
                public int Quantity { get; set; }
            }

            public class Order
            {
                public int Id { get; set; }
                public List<OrderItem> Items { get; set; }
            }

            [Facette(typeof(OrderItem))]
            public partial record OrderItemDto;

            [Facette(typeof(Order))]
            public partial record OrderDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        // Should use Select + FromSource for nested DTO collections
        Assert.Contains("OrderItemDto.FromSource(", generatedCode);
        Assert.Contains(".ToList()", generatedCode);
    }

    [Fact]
    public void Generator_WithArrayOfNestedDto_GeneratesToArrayMapping()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Tag
            {
                public string Name { get; set; }
            }

            public class Article
            {
                public int Id { get; set; }
                public Tag[] Tags { get; set; }
            }

            [Facette(typeof(Tag))]
            public partial record TagDto;

            [Facette(typeof(Article))]
            public partial record ArticleDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ArticleDto.g.cs"))
            .GetText().ToString();

        // Should use Select + FromSource for nested DTO arrays
        Assert.Contains("TagDto.FromSource(", generatedCode);
        Assert.Contains(".ToArray()", generatedCode);
    }

    [Fact]
    public void Generator_WithSimpleCollection_GeneratesDirectCopy()
    {
        var source = """
            using Facette.Abstractions;
            using System.Collections.Generic;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public List<string> Tags { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should use ToList() for simple collection copy
        Assert.Contains(".ToList()", generatedCode);
        // Should NOT use Select + FromSource for simple types (collection of primitives)
        Assert.DoesNotContain(".Select(", generatedCode);
    }

    [Fact]
    public void Generator_WithNullableCollection_GeneratesNullCheck()
    {
        var source = """
            using Facette.Abstractions;
            using System.Collections.Generic;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public List<string>? Tags { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should include null check for nullable collection
        Assert.Contains("source.Tags != null", generatedCode);
    }
}
