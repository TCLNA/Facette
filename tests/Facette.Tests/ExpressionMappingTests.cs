using Facette.Tests.Helpers;

namespace Facette.Tests;

public class ExpressionMappingTests
{
    [Fact]
    public void MapExpression_GeneratedWhenProjectionEnabled()
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

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("MapExpression<TResult>", generatedCode);
        Assert.Contains("_ExpressionVisitor", generatedCode);
    }

    [Fact]
    public void MapExpression_NotGeneratedWhenProjectionDisabled()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), GenerateProjection = false)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.DoesNotContain("MapExpression", generatedCode);
    }

    [Fact]
    public void MapExpression_DirectProperty_GeneratesPropertyAccess()
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

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("Expression.Property(SourceParam, \"Id\")", generatedCode);
        Assert.Contains("Expression.Property(SourceParam, \"Name\")", generatedCode);
    }

    [Fact]
    public void MapExpression_CustomMapping_UsesSourcePropertyName()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string FullName { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("FullName")]
                public string Name { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("Expression.Property(SourceParam, \"FullName\")", generatedCode);
    }

    [Fact]
    public void MapExpression_FlattenedProperty_GeneratesNestedAccess()
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
                [MapFrom("Address.City")]
                public string City { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Flattened should generate nested Property access
        Assert.Contains("Expression.Property(", generatedCode);
        Assert.Contains("\"Address\"", generatedCode);
        Assert.Contains("\"City\"", generatedCode);
    }

    [Fact]
    public void MapExpression_EnumToString_GeneratesCallExpression()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum Status { Active, Inactive }

            public class User
            {
                public int Id { get; set; }
                public Status Status { get; set; }
            }

            [Facette(typeof(User))]
            public partial record UserDto
            {
                [MapFrom("Status")]
                public string StatusText { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // EnumToString should use Expression.Call for .ToString()
        Assert.Contains("Expression.Call(", generatedCode);
        Assert.Contains("\"ToString\"", generatedCode);
    }

    [Fact]
    public void MapExpression_VisitorStructure_IsCorrect()
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

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Verify visitor structure
        Assert.Contains("class _ExpressionVisitor : System.Linq.Expressions.ExpressionVisitor", generatedCode);
        Assert.Contains("VisitParameter", generatedCode);
        Assert.Contains("VisitMember", generatedCode);
        Assert.Contains("SourceParam", generatedCode);
    }

    [Fact]
    public void WhereDto_GeneratedInMapper()
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

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("WhereDto", mapperCode);
        Assert.Contains("MapExpression", mapperCode);
    }

    [Fact]
    public void WhereDto_NotGeneratedWhenProjectionDisabled()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), GenerateProjection = false)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.DoesNotContain("WhereDto", mapperCode);
    }

    [Fact]
    public void MapExpression_ConvertMethod_GeneratesCallExpression()
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
                public string Created { get; init; }

                public static string ToIso(DateTime dt) => dt.ToString("O");
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Convert method should generate Expression.Call with GetMethod
        Assert.Contains("GetMethod(\"ToIso\")", generatedCode);
    }
}
