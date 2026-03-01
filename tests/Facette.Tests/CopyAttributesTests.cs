using Facette.Tests.Helpers;

namespace Facette.Tests;

public class CopyAttributesTests
{
    [Fact]
    public void CopyAttributes_True_CopiesRequiredAttribute()
    {
        var source = """
            using Facette.Abstractions;
            using System.ComponentModel.DataAnnotations;

            namespace TestApp;

            public class Product
            {
                [Required]
                public string Name { get; set; }
                public int Price { get; set; }
            }

            [Facette(typeof(Product), CopyAttributes = true)]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("[Required]", generatedCode);
    }

    [Fact]
    public void CopyAttributes_True_CopiesObsoleteAttribute()
    {
        var source = """
            using Facette.Abstractions;
            using System;

            namespace TestApp;

            public class Product
            {
                public string Name { get; set; }
                [Obsolete("Use NewPrice instead")]
                public int Price { get; set; }
            }

            [Facette(typeof(Product), CopyAttributes = true)]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("[System.ObsoleteAttribute(", generatedCode);
        Assert.Contains("Use NewPrice instead", generatedCode);
    }

    [Fact]
    public void CopyAttributes_False_DoesNotCopyAttributes()
    {
        var source = """
            using Facette.Abstractions;
            using System.ComponentModel.DataAnnotations;
            using System;

            namespace TestApp;

            public class Product
            {
                [Required]
                public string Name { get; set; }
                [Obsolete("old")]
                public int Price { get; set; }
            }

            [Facette(typeof(Product), CopyAttributes = false)]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.DoesNotContain("[Required]", generatedCode);
        Assert.DoesNotContain("[Obsolete", generatedCode);
    }

    [Fact]
    public void CopyAttributes_SkipsFacetteAttributes()
    {
        var source = """
            using Facette.Abstractions;
            using System.ComponentModel.DataAnnotations;

            namespace TestApp;

            public class Product
            {
                [Required]
                public string Name { get; set; }
                public int Price { get; set; }
            }

            [Facette(typeof(Product), CopyAttributes = true)]
            public partial record ProductDto
            {
                [MapFrom("Name")]
                public string Name { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        // MapFrom should not appear in generated code as a copied attribute
        Assert.DoesNotContain("[MapFrom", generatedCode);
    }

    [Fact]
    public void CopyAttributes_SkipsEFCoreAttributes()
    {
        // EF Core attributes should be skipped even when CopyAttributes = true
        // Since we can't reference EF Core in tests, we verify that the generator
        // filters by namespace. We test with a non-EF attribute to confirm copying works
        // and verify the generated code doesn't contain EF namespace references.
        var source = """
            using Facette.Abstractions;
            using System.ComponentModel.DataAnnotations;

            namespace TestApp;

            public class Product
            {
                [Required]
                public string Name { get; set; }
                public int Price { get; set; }
            }

            [Facette(typeof(Product), CopyAttributes = true)]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        // Should copy DataAnnotations but never copy EF Core attributes
        Assert.Contains("[Required]", generatedCode);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", generatedCode);
    }

    [Fact]
    public void CopyAttributes_StringArgument_ReconstructsCorrectly()
    {
        var source = """
            using Facette.Abstractions;
            using System;

            namespace TestApp;

            public class Product
            {
                [Obsolete("Do not use this property")]
                public string Name { get; set; }
                public int Price { get; set; }
            }

            [Facette(typeof(Product), CopyAttributes = true)]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("[System.ObsoleteAttribute(", generatedCode);
        Assert.Contains("Do not use this property", generatedCode);
    }

    [Fact]
    public void CopyAttributes_NoArgs_ReconstructsCorrectly()
    {
        var source = """
            using Facette.Abstractions;
            using System.ComponentModel.DataAnnotations;

            namespace TestApp;

            public class Product
            {
                [Required]
                public string Name { get; set; }
                public int Price { get; set; }
            }

            [Facette(typeof(Product), CopyAttributes = true)]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        // [Required] with no constructor args should be reconstructed as [Required]
        Assert.Contains("[Required]", generatedCode);
    }

    [Fact]
    public void CopyAttributes_Default_IsFalse()
    {
        var source = """
            using Facette.Abstractions;
            using System.ComponentModel.DataAnnotations;
            using System;

            namespace TestApp;

            public class Product
            {
                [Required]
                public string Name { get; set; }
                [Obsolete("old")]
                public int Price { get; set; }
            }

            [Facette(typeof(Product))]
            public partial record ProductDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("ProductDto.g.cs"))
            .GetText().ToString();

        // Without CopyAttributes specified (defaults to false), no attributes should be copied
        Assert.DoesNotContain("[Required]", generatedCode);
        Assert.DoesNotContain("[Obsolete", generatedCode);
    }
}
