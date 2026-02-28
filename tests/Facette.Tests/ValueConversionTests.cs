using Facette.Tests.Helpers;

namespace Facette.Tests;

public class ValueConversionTests
{
    [Fact]
    public void Convert_InFromSource_GeneratesMethodCall()
    {
        var source = """
            using Facette.Abstractions;
            using System;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public DateTime CreatedAt { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("CreatedAt", Convert = nameof(ToIso))]
                public string CreatedDate { get; init; }

                public static string ToIso(DateTime dt) => dt.ToString("O");
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // FromSource should call the convert method
        Assert.Contains("UserDto.ToIso(source.CreatedAt)", generatedCode);
    }

    [Fact]
    public void ConvertBack_InToSource_GeneratesMethodCall()
    {
        var source = """
            using Facette.Abstractions;
            using System;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public DateTime CreatedAt { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("CreatedAt", Convert = nameof(ToIso), ConvertBack = nameof(FromIso))]
                public string CreatedDate { get; init; }

                public static string ToIso(DateTime dt) => dt.ToString("O");
                public static DateTime FromIso(string s) => DateTime.Parse(s);
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // ToSource should call the convert back method
        Assert.Contains("UserDto.FromIso(this.CreatedDate)", generatedCode);
    }

    [Fact]
    public void NoConvertBack_ExcludedFromToSource()
    {
        var source = """
            using Facette.Abstractions;
            using System;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public DateTime CreatedAt { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("CreatedAt", Convert = nameof(ToIso))]
                public string CreatedDate { get; init; }

                public static string ToIso(DateTime dt) => dt.ToString("O");
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // ToSource should NOT contain CreatedDate (the converted property without ConvertBack)
        var toSourceIdx = generatedCode.IndexOf("ToSource()");
        Assert.True(toSourceIdx >= 0, "ToSource method should exist");
        var projectionIdx = generatedCode.IndexOf("Projection", toSourceIdx);
        var toSourceSection = projectionIdx > 0
            ? generatedCode.Substring(toSourceIdx, projectionIdx - toSourceIdx)
            : generatedCode.Substring(toSourceIdx);
        Assert.DoesNotContain("CreatedDate", toSourceSection);
    }

    [Fact]
    public void ParameterlessMapFrom_UsesSamePropertyName()
    {
        var source = """
            using Facette.Abstractions;
            using System;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public DateTime CreatedAt { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom(Convert = nameof(ToIso))]
                public string CreatedAt { get; init; }

                public static string ToIso(DateTime dt) => dt.ToString("O");
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Should use property's own name as source
        Assert.Contains("UserDto.ToIso(source.CreatedAt)", generatedCode);
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
                public int Score { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("Score", Convert = "NonExistentMethod")]
                public string ScoreText { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT008"
            && d.GetMessage().Contains("NonExistentMethod"));
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
                public int Score { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("Score", Convert = nameof(ToStr), ConvertBack = "NonExistent")]
                public string ScoreText { get; init; }

                public static string ToStr(int v) => v.ToString();
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT009"
            && d.GetMessage().Contains("NonExistent"));
    }

    [Fact]
    public void Convert_InProjection_GeneratesMethodCall()
    {
        var source = """
            using Facette.Abstractions;
            using System;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public DateTime CreatedAt { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("CreatedAt", Convert = nameof(ToIso))]
                public string CreatedDate { get; init; }

                public static string ToIso(DateTime dt) => dt.ToString("O");
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Projection should also call the convert method
        var projectionIdx = generatedCode.IndexOf("Projection");
        Assert.True(projectionIdx >= 0);
        var projectionSection = generatedCode.Substring(projectionIdx);
        Assert.Contains("UserDto.ToIso(source.CreatedAt)", projectionSection);
    }
}
