using Facette.Tests.Helpers;

namespace Facette.Tests;

public class DiagnosticTests
{
    [Fact]
    public void Diagnostic_FCT002_IncludeAndExcludeBothSpecified()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Password { get; set; }
            }

            [Facette(typeof(User), "Password", Include = new[] { "Id" })]
            public partial record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT002");
    }

    [Fact]
    public void Diagnostic_FCT001_TypeNotPartial()
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
            public record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT001");
        // Should not generate code when there's an error
        Assert.Empty(result.GeneratedTrees);
    }

    [Fact]
    public void Diagnostic_SourceTypeNotResolvable_DoesNotCrash()
    {
        // When typeof() references a non-existent type, the compiler reports CS0246
        // before the generator runs. ForAttributeWithMetadataName does not fire.
        // This test verifies the generator doesn't crash and produces no output.
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            [Facette(typeof(NonExistentType))]
            public partial record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        // Generator should not produce any output (transform never fires)
        Assert.Empty(result.GeneratedTrees);
    }

    [Fact]
    public void Diagnostic_FCT004_ExcludePropertyNotFound()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), "NonExistentProp")]
            public partial record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT004"
            && d.GetMessage().Contains("NonExistentProp"));
        // FCT004 is a warning, so code should still be generated
        Assert.NotEmpty(result.GeneratedTrees);
    }

    [Fact]
    public void Diagnostic_FCT004_IncludePropertyNotFound()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), Include = new[] { "Id", "Bogus" })]
            public partial record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT004"
            && d.GetMessage().Contains("Bogus"));
        // Valid properties should still be generated
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();
        Assert.Contains("public int Id { get; init; }", generatedCode);
    }
}
