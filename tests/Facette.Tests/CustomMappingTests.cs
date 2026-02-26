using Facette.Tests.Helpers;

namespace Facette.Tests;

public class CustomMappingTests
{
    [Fact]
    public void Generator_WithMapFrom_MapsToSourceProperty()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string FirstName { get; set; }
                public string LastName { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("FirstName")]
                public string DisplayName { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should map DisplayName from FirstName
        Assert.Contains("DisplayName = source.FirstName", generatedCode);
        // Should NOT re-generate DisplayName as a property (user declared it)
        Assert.DoesNotContain("public string DisplayName { get; init; }", generatedCode);
        // Should still generate other properties
        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string LastName { get; init; }", generatedCode);
    }

    [Fact]
    public void Generator_WithMapFrom_GeneratesToSourceMapping()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string FirstName { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("FirstName")]
                public string DisplayName { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // ToSource should map back: FirstName = this.DisplayName
        Assert.Contains("FirstName = this.DisplayName", generatedCode);
    }

    [Fact]
    public void Generator_UserDeclaredPropertyWithoutMapFrom_IsSkipped()
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
                public string ComputedField { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should NOT contain ComputedField in mappings
        Assert.DoesNotContain("ComputedField", generatedCode);
        // Should still generate source properties
        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string Name { get; init; }", generatedCode);
    }

    [Fact]
    public void Diagnostic_FCT005_MapFromPropertyNotFound()
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
                [MapFrom("NonExistent")]
                public string DisplayName { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT005");
    }
}
