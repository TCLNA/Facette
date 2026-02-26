using Facette.Tests.Helpers;

namespace Facette.Tests;

public class ModelBuilderTests
{
    [Fact]
    public void Generator_WithSimpleEntity_GeneratesProperties()
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
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string Name { get; init; }", generatedCode);
        Assert.Contains("public string Email { get; init; }", generatedCode);
    }

    [Fact]
    public void Generator_WithExclude_OmitsExcludedProperties()
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

            [Facette(typeof(User), "Password")]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string Name { get; init; }", generatedCode);
        Assert.DoesNotContain("Password", generatedCode);
    }

    [Fact]
    public void Generator_WithInclude_OnlyIncludesSpecifiedProperties()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Email { get; set; }
                public string Password { get; set; }
            }

            [Facette(typeof(User), Include = new[] { "Id", "Name" })]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string Name { get; init; }", generatedCode);
        Assert.DoesNotContain("Email", generatedCode);
        Assert.DoesNotContain("Password", generatedCode);
    }
}
