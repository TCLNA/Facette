using Facette.Tests.Helpers;

namespace Facette.Tests;

public class PresetTests
{
    [Fact]
    public void Create_ExcludesId()
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

            [Facette(typeof(User), Preset = FacettePreset.Create)]
            public partial record CreateUserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CreateUserDto.g.cs"))
            .GetText().ToString();

        Assert.DoesNotContain("int Id { get; init; }", generatedCode);
        Assert.Contains("Name", generatedCode);
        Assert.Contains("Email", generatedCode);
    }

    [Fact]
    public void Create_ExcludesSourceTypeNameId()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Order
            {
                public int Id { get; set; }
                public int OrderId { get; set; }
                public string Description { get; set; }
            }

            [Facette(typeof(Order), Preset = FacettePreset.Create)]
            public partial record CreateOrderDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CreateOrderDto.g.cs"))
            .GetText().ToString();

        Assert.DoesNotContain("int Id { get; init; }", generatedCode);
        Assert.DoesNotContain("int OrderId { get; init; }", generatedCode);
        Assert.Contains("Description", generatedCode);
    }

    [Fact]
    public void Create_DisablesProjectionByDefault()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), Preset = FacettePreset.Create)]
            public partial record CreateUserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CreateUserDto.g.cs"))
            .GetText().ToString();

        Assert.DoesNotContain("Projection", generatedCode);
    }

    [Fact]
    public void Read_DisablesToSourceByDefault()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), Preset = FacettePreset.Read)]
            public partial record ReadUserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ReadUserDto.g.cs"))
            .GetText().ToString();

        Assert.DoesNotContain("ToSource()", generatedCode);
        Assert.Contains("FromSource", generatedCode);
        Assert.Contains("Projection", generatedCode);
    }

    [Fact]
    public void Create_UserOverrideProjection_Wins()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), Preset = FacettePreset.Create, GenerateProjection = true)]
            public partial record CreateUserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CreateUserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("Projection", generatedCode);
    }

    [Fact]
    public void Default_NoExclusions()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), Preset = FacettePreset.Default)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("int Id { get; init; }", generatedCode);
        Assert.Contains("Name", generatedCode);
        Assert.Contains("ToSource()", generatedCode);
        Assert.Contains("Projection", generatedCode);
    }
}
