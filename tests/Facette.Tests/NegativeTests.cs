using Facette.Tests.Helpers;

namespace Facette.Tests;

public class NegativeTests
{
    [Fact]
    public void SourceType_WithNoPublicProperties_GeneratesEmptyDto()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class EmptySource
            {
                private int _hidden;
            }

            [Facette(typeof(EmptySource))]
            public partial record EmptyDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        Assert.NotEmpty(result.GeneratedTrees);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("EmptyDto.g.cs"))
            .GetText().ToString();
        Assert.Contains("FromSource", generatedCode);
        // No properties should be generated
        Assert.DoesNotContain("{ get; init; }", generatedCode);
    }

    [Fact]
    public void SourceType_AsInterface_GeneratesDto()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public interface IUser
            {
                int Id { get; }
                string Name { get; }
            }

            [Facette(typeof(IUser))]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        Assert.NotEmpty(result.GeneratedTrees);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();
        Assert.Contains("public int Id { get; init; }", generatedCode);
        Assert.Contains("public string Name { get; init; }", generatedCode);
    }

    [Fact]
    public void ExcludeAllProperties_GeneratesEmptyDto()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), "Id", "Name")]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        Assert.NotEmpty(result.GeneratedTrees);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();
        Assert.DoesNotContain("{ get; init; }", generatedCode);
    }

    [Fact]
    public void EmptyInclude_GeneratesEmptyDto()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), Include = new string[] { })]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        Assert.NotEmpty(result.GeneratedTrees);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();
        Assert.DoesNotContain("{ get; init; }", generatedCode);
    }

    [Fact]
    public void DeepNesting_FiveLevels_GeneratesCorrectly()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Level5 { public string Value { get; set; } }
            public class Level4 { public Level5 Child { get; set; } }
            public class Level3 { public Level4 Child { get; set; } }
            public class Level2 { public Level3 Child { get; set; } }
            public class Level1 { public Level2 Child { get; set; } }

            [Facette(typeof(Level5))]
            public partial record Level5Dto;

            [Facette(typeof(Level4))]
            public partial record Level4Dto;

            [Facette(typeof(Level3))]
            public partial record Level3Dto;

            [Facette(typeof(Level2))]
            public partial record Level2Dto;

            [Facette(typeof(Level1))]
            public partial record Level1Dto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        // Should generate DTOs for all 5 levels without stack overflow
        Assert.True(result.GeneratedTrees.Length >= 5);
    }

    [Fact]
    public void SourceType_WithReadOnlyProperties_MapsCorrectly()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class ReadOnlySource
            {
                public int Id { get; }
                public string Name { get; }

                public ReadOnlySource(int id, string name)
                {
                    Id = id;
                    Name = name;
                }
            }

            [Facette(typeof(ReadOnlySource))]
            public partial record ReadOnlyDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        Assert.NotEmpty(result.GeneratedTrees);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ReadOnlyDto.g.cs"))
            .GetText().ToString();
        Assert.Contains("public int Id { get; init; }", generatedCode);
    }

    [Fact]
    public void FacetteIgnore_OnAllProperties_GeneratesEmptyDto()
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
                public int Id { get; init; }

                [FacetteIgnore]
                public string Name { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        Assert.NotEmpty(result.GeneratedTrees);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();
        // Properties declared by user with [FacetteIgnore] shouldn't be re-generated
        Assert.DoesNotContain("{ get; init; }", generatedCode);
    }

    [Fact]
    public void AllGenerationDisabled_GeneratesMinimalOutput()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), GenerateToSource = false, GenerateProjection = false, GenerateMapper = false)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);

        Assert.NotEmpty(result.GeneratedTrees);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();
        Assert.Contains("FromSource", generatedCode);
        Assert.DoesNotContain("ToSource()", generatedCode);
        Assert.DoesNotContain("Projection", generatedCode);
        // No mapper class should be generated
        Assert.DoesNotMatch(".*UserDtoMapper.*", string.Join("", result.GeneratedTrees.Select(t => t.FilePath)));
    }
}
