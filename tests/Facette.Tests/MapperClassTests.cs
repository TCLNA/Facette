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
