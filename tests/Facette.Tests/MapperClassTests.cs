using Facette.Tests.Helpers;

namespace Facette.Tests;

public class MapperClassTests
{
    [Fact]
    public void Generator_GeneratesMapperClass()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Customer))]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomerDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("public static partial class CustomerDtoMapper", mapperCode);
        Assert.Contains("public static CustomerDto ToDto(this", mapperCode);
        Assert.Contains("CustomerDto.FromSource(source)", mapperCode);
    }

    [Fact]
    public void Generator_MapperClass_IncludesToSourceExtension()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Customer))]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomerDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("ToSource(this CustomerDto dto)", mapperCode);
    }

    [Fact]
    public void Generator_MapperClass_IncludesQueryableProjection()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Customer))]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomerDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("IQueryable<CustomerDto> ProjectToDto", mapperCode);
        Assert.Contains("query.Select(CustomerDto.Projection)", mapperCode);
    }

    [Fact]
    public void Generator_MapperClass_IncludesCollectionToDto()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Customer))]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomerDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("IEnumerable<CustomerDto> ToDto(this IEnumerable<global::TestApp.Customer> sources)", mapperCode);
        Assert.Contains("sources.Select(CustomerDto.FromSource)", mapperCode);
    }

    [Fact]
    public void Generator_MapperClass_IncludesCollectionToDtoList()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Customer))]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomerDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("List<CustomerDto> ToDtoList(this IEnumerable<global::TestApp.Customer> sources)", mapperCode);
        Assert.Contains(".FromSource).ToList()", mapperCode);
    }

    [Fact]
    public void Generator_MapperClass_IncludesCollectionToSource()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Customer))]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomerDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("IEnumerable<global::TestApp.Customer> ToSource(this IEnumerable<CustomerDto> dtos)", mapperCode);
        Assert.Contains("dtos.Select(dto => dto.ToSource())", mapperCode);
    }

    [Fact]
    public void Generator_MapperClass_IncludesCollectionToSourceList()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            [Facette(typeof(Customer))]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var mapperCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("CustomerDtoMapper.g.cs"))
            .GetText().ToString();

        Assert.Contains("List<global::TestApp.Customer> ToSourceList(this IEnumerable<CustomerDto> dtos)", mapperCode);
        Assert.Contains("dtos.Select(dto => dto.ToSource()).ToList()", mapperCode);
    }

    [Fact]
    public void Generator_WithGenerateMapperFalse_OmitsMapperClass()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class Customer
            {
                public int Id { get; set; }
            }

            [Facette(typeof(Customer), GenerateMapper = false)]
            public partial record CustomerDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var hasMapper = result.GeneratedTrees
            .Any(t => t.FilePath.Contains("Mapper.g.cs"));

        Assert.False(hasMapper);
    }
}
