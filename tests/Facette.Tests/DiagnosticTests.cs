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

    [Fact]
    public void Diagnostic_FCT005_MapFromPropertyNotFound()
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
                [MapFrom("NonExistentProperty")]
                public string Custom { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT005"
            && d.GetMessage().Contains("NonExistentProperty"));
    }

    [Fact]
    public void Diagnostic_FCT007_FlattenedPathSegmentNotFound()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Address
            {
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
                [MapFrom("Address.NonExistent")]
                public string AddressZip { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT007"
            && d.GetMessage().Contains("NonExistent"));
    }

    [Fact]
    public void Diagnostic_FCT008_ConvertMethodNotFound()
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
                [MapFrom("Name", Convert = "NonExistentConvert")]
                public string Name { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT008"
            && d.GetMessage().Contains("NonExistentConvert"));
    }

    [Fact]
    public void Diagnostic_FCT009_ConvertBackMethodNotFound()
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
                [MapFrom("Name", ConvertBack = "NonExistentConvertBack")]
                public string Name { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT009"
            && d.GetMessage().Contains("NonExistentConvertBack"));
    }

    [Fact]
    public void Diagnostic_FCT010_AmbiguousNestedDto()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Address
            {
                public string City { get; set; }
            }

            public class User
            {
                public int Id { get; set; }
                public Address Address { get; set; }
            }

            [Facette(typeof(Address))]
            public partial record AddressDtoA;

            [Facette(typeof(Address))]
            public partial record AddressDtoB;

            [Facette(typeof(User))]
            public partial record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT010");
    }

    [Fact]
    public void Diagnostic_FCT013_ConditionalMethodNotFound()
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
                [MapWhen("NonExistentCondition")]
                public string Name { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT013"
            && d.GetMessage().Contains("NonExistentCondition"));
    }

    [Fact]
    public void Diagnostic_FCT013_ConditionalMethodWrongSignature()
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
                // Method exists but takes parameters (wrong signature)
                public static bool BadCondition(int x) => true;

                [MapWhen("BadCondition")]
                public string Name { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT013");
    }

    [Fact]
    public void Diagnostic_NoDiagnostics_WhenValid()
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
            public partial record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Empty(diagnostics);
        Assert.NotEmpty(result.GeneratedTrees);
    }
}
