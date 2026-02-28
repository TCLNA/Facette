using Facette.Tests.Helpers;

namespace Facette.Tests;

public class FlatteningTests
{
    [Fact]
    public void Flattening_ConventionBased_ResolvesAddressCity()
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
                public Address Address { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                public string AddressCity { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // FromSource should resolve AddressCity to source.Address.City
        Assert.Contains("AddressCity = source.Address.City", generatedCode);
    }

    [Fact]
    public void Flattening_DotNotation_MapFrom()
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
                public Address Address { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("Address.City")]
                public string CityName { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should use flattened path from MapFrom
        Assert.Contains("CityName = source.Address.City", generatedCode);
    }

    [Fact]
    public void Flattening_NullableSegment_GeneratesNullCheck()
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
                public Address? Address { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                public string AddressCity { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should generate null check for nullable intermediate segment
        Assert.Contains("source.Address != null", generatedCode);
        Assert.Contains("source.Address.City", generatedCode);
    }

    [Fact]
    public void Flattening_ExcludedFromToSource()
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
                public Address Address { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                public string AddressCity { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // ToSource should reconstruct the Address object from flattened properties
        var toSourceIdx = generatedCode.IndexOf("ToSource()");
        Assert.True(toSourceIdx >= 0, "ToSource method should exist");
        var projectionIdx = generatedCode.IndexOf("Projection", toSourceIdx);
        var toSourceSection = projectionIdx > 0
            ? generatedCode.Substring(toSourceIdx, projectionIdx - toSourceIdx)
            : generatedCode.Substring(toSourceIdx);
        // Reverse flattening: Address = new ... { City = this.AddressCity }
        Assert.Contains("Address = new", toSourceSection);
        Assert.Contains("City = this.AddressCity", toSourceSection);
    }

    [Fact]
    public void Flattening_FCT007_InvalidPathSegment()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("Address.City")]
                public string CityName { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT007"
            && d.GetMessage().Contains("Address"));
    }
}
