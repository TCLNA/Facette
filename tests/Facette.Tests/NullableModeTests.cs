using Facette.Tests.Helpers;

namespace Facette.Tests;

public class NullableModeTests
{
    [Fact]
    public void AllNullable_ValueType_GeneratesNullableProperty()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), NullableMode = NullableMode.AllNullable)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Value type int should become int?
        Assert.Contains("int? Id { get; init; }", generatedCode);
    }

    [Fact]
    public void AllNullable_ReferenceType_GeneratesNullableProperty()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), NullableMode = NullableMode.AllNullable)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Reference type string should become string?
        Assert.Contains("string? Name { get; init; }", generatedCode);
    }

    [Fact]
    public void AllRequired_ReferenceType_GeneratesDefault()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), NullableMode = NullableMode.AllRequired)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Reference type should get = default!;
        Assert.Contains("string Name { get; init; } = default!;", generatedCode);
    }

    [Fact]
    public void AllRequired_NullableRef_StripsNullable()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string? NickName { get; set; }
            }

            [Facette(typeof(User), NullableMode = NullableMode.AllRequired)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Nullable reference type should have ? stripped and get = default!;
        Assert.Contains("string NickName { get; init; } = default!;", generatedCode);
        Assert.DoesNotContain("string? NickName", generatedCode);
    }

    [Fact]
    public void Auto_DefaultBehavior_Unchanged()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), NullableMode = NullableMode.Auto)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Auto mode: value types no suffix, reference types get = default!;
        Assert.Contains("int Id { get; init; }", generatedCode);
        Assert.Contains("string Name { get; init; } = default!;", generatedCode);
    }

    [Fact]
    public void AllNullable_Collection_GeneratesNullable()
    {
        var source = """
            using Facette.Abstractions;
            using System.Collections.Generic;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public List<string> Tags { get; set; }
            }

            [Facette(typeof(User), NullableMode = NullableMode.AllNullable)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Collection type should also become nullable
        Assert.Contains("?", generatedCode);
        Assert.Contains("Tags { get; init; }", generatedCode);
    }

    [Fact]
    public void AllRequired_ValueType_NoChange()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(User), NullableMode = NullableMode.AllRequired)]
            public partial record UserDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("UserDto.g.cs"))
            .GetText().ToString();

        // Value types should NOT get = default!; in AllRequired mode
        Assert.Contains("int Id { get; init; }", generatedCode);
        Assert.DoesNotContain("int Id { get; init; } = default!;", generatedCode);
    }

    [Fact]
    public void NullableMode_ParsedFromAttribute()
    {
        // Verify that specifying NullableMode on the attribute actually changes behavior
        // by comparing AllNullable vs default (Auto)
        var sourceAllNullable = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Item
            {
                public int Id { get; set; }
                public double Price { get; set; }
            }

            [Facette(typeof(Item), NullableMode = NullableMode.AllNullable)]
            public partial record ItemDto;
            """;

        var sourceAuto = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Item
            {
                public int Id { get; set; }
                public double Price { get; set; }
            }

            [Facette(typeof(Item))]
            public partial record ItemDto;
            """;

        var resultNullable = GeneratorTestHelper.RunGenerator(sourceAllNullable);
        var codeNullable = resultNullable.GeneratedTrees
            .First(t => t.FilePath.Contains("ItemDto.g.cs"))
            .GetText().ToString();

        var resultAuto = GeneratorTestHelper.RunGenerator(sourceAuto);
        var codeAuto = resultAuto.GeneratedTrees
            .First(t => t.FilePath.Contains("ItemDto.g.cs"))
            .GetText().ToString();

        // AllNullable should have int? but Auto should have int (no ?)
        Assert.Contains("int? Id { get; init; }", codeNullable);
        Assert.Contains("int Id { get; init; }", codeAuto);
        Assert.DoesNotContain("int? Id", codeAuto);
    }
}
