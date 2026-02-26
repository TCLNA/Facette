using Facette.Tests.Helpers;

namespace Facette.Tests;

public class NestedMappingTests
{
    [Fact]
    public void Generator_WithNestedDto_GeneratesNestedProperty()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            public class User
            {
                public int Id { get; set; }
                public Address HomeAddress { get; set; }
            }

            [Facette(typeof(Address))]
            public partial record AddressDto;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should generate AddressDto property type
        Assert.Contains("AddressDto", generatedCode);
        // Should map via FromSource
        Assert.Contains("AddressDto.FromSource(source.HomeAddress)", generatedCode);
    }

    [Fact]
    public void Generator_WithNullableNestedDto_GeneratesNullCheck()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            public class User
            {
                public int Id { get; set; }
                public Address? HomeAddress { get; set; }
            }

            [Facette(typeof(Address))]
            public partial record AddressDto;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should generate nullable property type
        Assert.Contains("AddressDto?", generatedCode);
        // Should include null check
        Assert.Contains("source.HomeAddress != null", generatedCode);
    }

    [Fact]
    public void Generator_WithNestedDto_InlinesProjection()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            public class User
            {
                public int Id { get; set; }
                public Address HomeAddress { get; set; }
            }

            [Facette(typeof(Address))]
            public partial record AddressDto;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Projection should inline nested initializer (not FromSource)
        Assert.Contains("source.HomeAddress.Street", generatedCode);
        Assert.Contains("source.HomeAddress.City", generatedCode);
    }

    [Fact]
    public void Generator_WithNestedDto_GeneratesToSourceMapping()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }

            public class User
            {
                public int Id { get; set; }
                public Address HomeAddress { get; set; }
            }

            [Facette(typeof(Address))]
            public partial record AddressDto;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // ToSource should call .ToSource() on nested DTO
        Assert.Contains("this.HomeAddress.ToSource()", generatedCode);
    }
}
