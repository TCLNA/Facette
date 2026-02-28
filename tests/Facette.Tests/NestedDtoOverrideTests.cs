using Facette.Tests.Helpers;

namespace Facette.Tests;

public class NestedDtoOverrideTests
{
    [Fact]
    public void Ambiguous_TwoDtosForSameSource_NoOverride_EmitsFCT010()
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
            public partial record AddressSummaryDto;

            [Facette(typeof(Address))]
            public partial record AddressDetailDto;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        // FCT010 is an error — should be reported on UserDto
        Assert.Contains(diagnostics, d => d.Id == "FCT010"
            && d.GetMessage().Contains("Address"));
    }

    [Fact]
    public void NestedDtos_Override_ResolvesAmbiguity()
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
            public partial record AddressSummaryDto;

            [Facette(typeof(Address))]
            public partial record AddressDetailDto;

            [Facette(typeof(User), NestedDtos = new[] { typeof(AddressDetailDto) })]
            public partial record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        // No FCT010 — override resolves ambiguity
        Assert.DoesNotContain(diagnostics, d => d.Id == "FCT010");

        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should use AddressDetailDto, not AddressSummaryDto
        Assert.Contains("AddressDetailDto", generatedCode);
        Assert.DoesNotContain("AddressSummaryDto", generatedCode);
    }

    [Fact]
    public void NestedDtos_AppliedToNestedProperty()
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
            public partial record AddressSummaryDto;

            [Facette(typeof(Address))]
            public partial record AddressDetailDto;

            [Facette(typeof(User), NestedDtos = new[] { typeof(AddressSummaryDto) })]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should use AddressSummaryDto
        Assert.Contains("AddressSummaryDto", generatedCode);
        Assert.Contains("AddressSummaryDto.FromSource(source.HomeAddress)", generatedCode);
    }

    [Fact]
    public void NestedDtos_AppliedToCollectionElement()
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
            public partial record TagSummaryDto;

            [Facette(typeof(Tag))]
            public partial record TagDetailDto;

            [Facette(typeof(Item), NestedDtos = new[] { typeof(TagDetailDto) })]
            public partial record ItemDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ItemDto.g.cs"))
            .GetText().ToString();

        // Should use TagDetailDto for the collection element
        Assert.Contains("TagDetailDto", generatedCode);
        Assert.DoesNotContain("TagSummaryDto", generatedCode);
    }

    [Fact]
    public void Unambiguous_SingleDto_WorksWithoutNestedDtos()
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

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        // No ambiguity — no FCT010
        Assert.DoesNotContain(diagnostics, d => d.Id == "FCT010");

        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("AddressDto", generatedCode);
    }
}
