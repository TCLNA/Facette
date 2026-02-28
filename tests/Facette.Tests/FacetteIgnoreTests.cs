using Facette.Tests.Helpers;

namespace Facette.Tests;

public class FacetteIgnoreTests
{
    [Fact]
    public void FacetteIgnore_ExcludesPropertyFromGeneration()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [FacetteIgnore]
                public string ExtraField { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // ExtraField should NOT appear in generated code
        Assert.DoesNotContain("ExtraField", generatedCode);
        // Normal properties should still be generated
        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("Name { get; init; }", generatedCode);
    }

    [Fact]
    public void FacetteIgnore_ExcludesFromFromSource()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [FacetteIgnore]
                public string ComputedField { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.DoesNotContain("ComputedField", generatedCode);
    }

    [Fact]
    public void FacetteIgnore_ExcludesFromProjection()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [FacetteIgnore]
                public string ExtraField { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Projection should not reference ExtraField
        Assert.DoesNotContain("ExtraField", generatedCode);
        // Projection should still exist
        Assert.Contains("Projection", generatedCode);
    }

    [Fact]
    public void FacetteIgnore_PreventsAutoGenerationOfSameNamedSourceProperty()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [FacetteIgnore]
                public string Name { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Name should not be auto-generated (user declared with [FacetteIgnore])
        Assert.DoesNotContain("Name = source.Name", generatedCode);
        // Id should still be generated
        Assert.Contains("Id = source.Id", generatedCode);
    }
}
