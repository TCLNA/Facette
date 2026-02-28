using Facette.Tests.Helpers;
using Xunit;

namespace Facette.Tests;

public class ReverseFlatteningTests
{
    private const string BaseSource = @"
using Facette.Abstractions;

namespace TestNs;

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string ZipCode { get; set; }
}

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Address Headquarters { get; set; }
}
";

    [Fact]
    public void ToSource_ReversesConventionFlattening()
    {
        var source = BaseSource + @"
[Facette(typeof(Company))]
public partial record CompanyFlatDto
{
    public string HeadquartersCity { get; init; } = """";
    public string HeadquartersZipCode { get; init; } = """";
}
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("CompanyFlatDto.g.cs"))
            .GetText().ToString();

        // ToSource should reconstruct the Headquarters object
        Assert.Contains("Headquarters = new", generated);
        Assert.Contains("City = this.HeadquartersCity", generated);
        Assert.Contains("ZipCode = this.HeadquartersZipCode", generated);
    }

    [Fact]
    public void ToSource_ReversesDotNotationFlattening()
    {
        var source = BaseSource + @"
[Facette(typeof(Company))]
public partial record CompanyFlatDto
{
    [MapFrom(""Headquarters.Street"")]
    public string HqStreet { get; init; } = """";

    [MapFrom(""Headquarters.City"")]
    public string HqCity { get; init; } = """";
}
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("CompanyFlatDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("Headquarters = new", generated);
        Assert.Contains("Street = this.HqStreet", generated);
        Assert.Contains("City = this.HqCity", generated);
    }

    [Fact]
    public void ToSource_FlatteningClaimsNavigation_OverNested()
    {
        // When flattened properties claim a navigation, the navigation is NOT auto-generated as Nested
        // The reverse flattening takes over instead
        var source = BaseSource + @"
[Facette(typeof(Address))]
public partial record AddressDto;

[Facette(typeof(Company))]
public partial record CompanyDto
{
    // HeadquartersCity is flattened — this claims the Headquarters navigation
    public string HeadquartersCity { get; init; } = """";
}
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("CompanyDto.g.cs"))
            .GetText().ToString();

        var toSourceSection = generated.Substring(generated.IndexOf("ToSource()"));
        var nextMethodIdx = toSourceSection.IndexOf("partial void");
        if (nextMethodIdx > 0) toSourceSection = toSourceSection.Substring(0, nextMethodIdx);

        // Should have reverse-flattened Headquarters, not Nested
        Assert.Contains("Headquarters = new", toSourceSection);
        Assert.Contains("City = this.HeadquartersCity", toSourceSection);
        // Should NOT have .ToSource() call for Headquarters
        Assert.DoesNotContain("Headquarters.ToSource()", toSourceSection);
    }

    [Fact]
    public void ToSource_ReverseFlatteningWithConvertBack()
    {
        var source = @"
using Facette.Abstractions;
using System;

namespace TestNs;

public class Inner
{
    public DateTime CreatedAt { get; set; }
}

public class Outer
{
    public int Id { get; set; }
    public Inner Details { get; set; }
}

[Facette(typeof(Outer))]
public partial record OuterDto
{
    [MapFrom(""Details.CreatedAt"", Convert = nameof(ToIso), ConvertBack = nameof(FromIso))]
    public string DetailsCreated { get; init; } = """";

    public static string ToIso(DateTime dt) => dt.ToString(""yyyy-MM-dd"");
    public static DateTime FromIso(string s) => DateTime.Parse(s);
}
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("OuterDto.g.cs"))
            .GetText().ToString();

        // Should reverse-flatten with ConvertBack
        Assert.Contains("Details = new", generated);
        Assert.Contains("OuterDto.FromIso(this.DetailsCreated)", generated);
    }
}
