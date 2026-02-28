using Facette.Tests.Helpers;

namespace Facette.Tests;

public class MultiLevelProjectionTests
{
    [Fact]
    public void Projection_TwoLevelNesting_InlinesRecursively()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Country
            {
                public string Name { get; set; }
                public string Code { get; set; }
            }

            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
                public Country Country { get; set; }
            }

            public class User
            {
                public int Id { get; set; }
                public Address HomeAddress { get; set; }
            }

            [Facette(typeof(Country))]
            public partial record CountryDto;

            [Facette(typeof(Address))]
            public partial record AddressDto;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should inline nested Address initializer with nested Country initializer
        Assert.Contains("source.HomeAddress.Street", generatedCode);
        Assert.Contains("source.HomeAddress.City", generatedCode);
        Assert.Contains("source.HomeAddress.Country", generatedCode);
        Assert.Contains("CountryDto", generatedCode);
    }

    [Fact]
    public void Projection_CollectionWithNestedElements_InlinesRecursively()
    {
        var source = """
            using Facette.Abstractions;
            using System.Collections.Generic;

            namespace TestApp;

            public class Tag
            {
                public string Name { get; set; }
                public string Color { get; set; }
            }

            public class Item
            {
                public int Id { get; set; }
                public List<Tag> Tags { get; set; }
            }

            [Facette(typeof(Tag))]
            public partial record TagDto;

            [Facette(typeof(Item))]
            public partial record ItemDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ItemDto.g.cs"))
            .GetText().ToString();

        // Collection with nested DTO elements should inline TagDto initializer
        Assert.Contains("TagDto", generatedCode);
        Assert.Contains("x.Name", generatedCode);
        Assert.Contains("x.Color", generatedCode);
    }

    [Fact]
    public void Projection_ThreeLevel_InlinesAllLevels()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Zip { public string Code { get; set; } }
            public class City { public string Name { get; set; } public Zip Zip { get; set; } }
            public class Address { public City City { get; set; } }
            public class User { public int Id { get; set; } public Address Address { get; set; } }

            [Facette(typeof(Zip))]
            public partial record ZipDto;

            [Facette(typeof(City))]
            public partial record CityDto;

            [Facette(typeof(Address))]
            public partial record AddressDto;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should reach 3 levels deep
        Assert.Contains("source.Address.City.Name", generatedCode);
        Assert.Contains("source.Address.City.Zip", generatedCode);
    }

    [Fact]
    public void Diagnostic_FCT006_CircularReference_DoesNotCrash()
    {
        // A circular reference should not cause infinite recursion
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Node
            {
                public int Id { get; set; }
                public Node Parent { get; set; }
            }

            [Facette(typeof(Node))]
            public partial record NodeDto;
            """;

        // This should not throw / infinite loop
        var result = GeneratorTestHelper.RunGenerator(source);

        // Should still generate code (the circular property just won't have deeply nested props)
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("NodeDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("NodeDto", generatedCode);
        Assert.Contains("FromSource", generatedCode);
    }
}
