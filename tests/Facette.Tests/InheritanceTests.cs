using Facette.Tests.Helpers;
using Xunit;

namespace Facette.Tests;

public class InheritanceTests
{
    private const string BaseSource = @"
using Facette.Abstractions;

namespace TestNs;

public class EntityBase
{
    public int Id { get; set; }
    public string CreatedBy { get; set; }
}

public class User : EntityBase
{
    public string Name { get; set; }
    public string Email { get; set; }
}

[Facette(typeof(EntityBase))]
public partial record EntityBaseDto;
";

    [Fact]
    public void Inheritance_DerivedDto_DoesNotRedeclareBaseProperties()
    {
        var source = BaseSource + @"
[Facette(typeof(User))]
public partial record UserDto : EntityBaseDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("UserDto.g.cs"))
            .GetText().ToString();

        // Should NOT declare Id or CreatedBy (already in base)
        Assert.DoesNotContain("int Id { get; init; }", generated);
        Assert.DoesNotContain("CreatedBy { get; init; }", generated);

        // Should declare Name and Email
        Assert.Contains("Name { get; init; }", generated);
        Assert.Contains("Email { get; init; }", generated);
    }

    [Fact]
    public void Inheritance_DerivedDto_FromSourceMapsAllProperties()
    {
        var source = BaseSource + @"
[Facette(typeof(User))]
public partial record UserDto : EntityBaseDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("UserDto.g.cs"))
            .GetText().ToString();

        // FromSource should map ALL properties including inherited ones
        Assert.Contains("Id = source.Id", generated);
        Assert.Contains("CreatedBy = source.CreatedBy", generated);
        Assert.Contains("Name = source.Name", generated);
        Assert.Contains("Email = source.Email", generated);
    }

    [Fact]
    public void Inheritance_DerivedDto_ProjectionIncludesAllProperties()
    {
        var source = BaseSource + @"
[Facette(typeof(User))]
public partial record UserDto : EntityBaseDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("UserDto.g.cs"))
            .GetText().ToString();

        // Projection should include all properties
        var projectionIdx = generated.IndexOf("Projection =>");
        Assert.True(projectionIdx >= 0, "Projection should exist");
        var projectionSection = generated.Substring(projectionIdx);

        Assert.Contains("Id = source.Id", projectionSection);
        Assert.Contains("Name = source.Name", projectionSection);
    }

    [Fact]
    public void Inheritance_BaseDto_GeneratesNormally()
    {
        var source = BaseSource + @"
[Facette(typeof(User))]
public partial record UserDto : EntityBaseDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var baseGenerated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("EntityBaseDto.g.cs"))
            .GetText().ToString();

        // Base DTO should generate Id and CreatedBy as properties
        Assert.Contains("Id { get; init; }", baseGenerated);
        Assert.Contains("CreatedBy { get; init; }", baseGenerated);
    }
}
