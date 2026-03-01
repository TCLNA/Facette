using Facette.Tests.Helpers;

namespace Facette.Tests;

public class ConditionalMappingTests
{
    [Fact]
    public void MapWhen_InFromSource_WrapsWithCondition()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Email { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapWhen(nameof(ShouldMapEmail))]
                public string Email { get; init; }

                public static bool ShouldMapEmail() => true;
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("UserDto.ShouldMapEmail() ? source.Email : default", generatedCode);
    }

    [Fact]
    public void MapWhen_InToSource_WrapsWithCondition()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Email { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapWhen(nameof(ShouldMapEmail))]
                public string Email { get; init; }

                public static bool ShouldMapEmail() => true;
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        var toSourceIdx = generatedCode.IndexOf("ToSource()");
        Assert.True(toSourceIdx >= 0);
        var toSourceSection = generatedCode.Substring(toSourceIdx);
        Assert.Contains("UserDto.ShouldMapEmail() ? this.Email : default", toSourceSection);
    }

    [Fact]
    public void FCT013_MethodNotFound_EmitsError()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Email { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapWhen("NonExistentMethod")]
                public string Email { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "FCT013");
    }

    [Fact]
    public void FCT013_MethodWrongSignature_EmitsError()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Email { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapWhen(nameof(WrongSignature))]
                public string Email { get; init; }

                // Wrong: takes a parameter, should be parameterless
                public static bool WrongSignature(string s) => true;
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);
        Assert.Contains(diagnostics, d => d.Id == "FCT013");
    }

    [Fact]
    public void MapWhen_WithMapFrom_BothApply()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string EmailAddress { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("EmailAddress")]
                [MapWhen(nameof(ShouldMap))]
                public string Email { get; init; }

                public static bool ShouldMap() => true;
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("UserDto.ShouldMap() ? source.EmailAddress : default", generatedCode);
    }
}
