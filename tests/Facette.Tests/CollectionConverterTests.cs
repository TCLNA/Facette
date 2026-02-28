using Facette.Tests.Helpers;
using Xunit;

namespace Facette.Tests;

public class CollectionConverterTests
{
    private const string BaseSource = @"
using Facette.Abstractions;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TestNs;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; }
}
";

    [Fact]
    public void HashSet_GeneratesToHashSet()
    {
        var source = BaseSource + @"
public class Container
{
    public int Id { get; set; }
    public HashSet<string> Tags { get; set; }
}

[Facette(typeof(Container))]
public partial record ContainerDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ContainerDto.g.cs"))
            .GetText().ToString();

        Assert.Contains(".ToHashSet()", generated);
        Assert.Contains("HashSet<", generated);
    }

    [Fact]
    public void IReadOnlyList_GeneratesToList()
    {
        var source = BaseSource + @"
public class Container
{
    public int Id { get; set; }
    public IReadOnlyList<string> Names { get; set; }
}

[Facette(typeof(Container))]
public partial record ContainerDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ContainerDto.g.cs"))
            .GetText().ToString();

        // IReadOnlyList maps to List in the DTO, using .ToList()
        Assert.Contains(".ToList()", generated);
        Assert.Contains("List<", generated);
    }

    [Fact]
    public void ImmutableArray_GeneratesToImmutableArray()
    {
        var source = BaseSource + @"
public class Container
{
    public int Id { get; set; }
    public ImmutableArray<string> Values { get; set; }
}

[Facette(typeof(Container))]
public partial record ContainerDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ContainerDto.g.cs"))
            .GetText().ToString();

        Assert.Contains(".ToImmutableArray()", generated);
        Assert.Contains("ImmutableArray<", generated);
        Assert.Contains("using System.Collections.Immutable;", generated);
    }

    [Fact]
    public void ImmutableList_GeneratesToImmutableList()
    {
        var source = BaseSource + @"
public class Container
{
    public int Id { get; set; }
    public ImmutableList<string> Items { get; set; }
}

[Facette(typeof(Container))]
public partial record ContainerDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ContainerDto.g.cs"))
            .GetText().ToString();

        Assert.Contains(".ToImmutableList()", generated);
        Assert.Contains("ImmutableList<", generated);
    }

    [Fact]
    public void NestedDtoCollection_WithHashSet_UsesCorrectConversion()
    {
        var source = BaseSource + @"
public class Container
{
    public int Id { get; set; }
    public HashSet<Item> Items { get; set; }
}

[Facette(typeof(Item))]
public partial record ItemDto;

[Facette(typeof(Container))]
public partial record ContainerDto;
";

        var result = GeneratorTestHelper.RunGenerator(source);
        var generated = result.GeneratedTrees
            .First(t => t.FilePath.EndsWith("ContainerDto.g.cs"))
            .GetText().ToString();

        // Should use Select + ToHashSet for nested DTO collections
        Assert.Contains(".Select(", generated);
        Assert.Contains(".ToHashSet()", generated);
    }
}
