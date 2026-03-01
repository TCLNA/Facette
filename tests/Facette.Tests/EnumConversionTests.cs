using Facette.Tests.Helpers;

namespace Facette.Tests;

public class EnumConversionTests
{
    [Fact]
    public void EnumToString_InFromSource_GeneratesToString()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum OrderStatus { Pending, Shipped, Delivered }

            public class Order
            {
                public int Id { get; set; }
                public OrderStatus Status { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto
            {
                [MapFrom("Status")]
                public string StatusText { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("source.Status.ToString()", generatedCode);
    }

    [Fact]
    public void StringToEnum_InFromSource_GeneratesParse()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum OrderStatus { Pending, Shipped, Delivered }

            public class Order
            {
                public int Id { get; set; }
                public string Status { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto
            {
                [MapFrom("Status")]
                public OrderStatus Status { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("System.Enum.Parse", generatedCode);
        Assert.Contains("source.Status", generatedCode);
    }

    [Fact]
    public void EnumToInt_InFromSource_GeneratesCast()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum OrderStatus { Pending, Shipped, Delivered }

            public class Order
            {
                public int Id { get; set; }
                public OrderStatus Status { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto
            {
                [MapFrom("Status")]
                public int StatusCode { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("(int)source.Status", generatedCode);
    }

    [Fact]
    public void IntToEnum_InFromSource_GeneratesCast()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum OrderStatus { Pending, Shipped, Delivered }

            public class Order
            {
                public int Id { get; set; }
                public int StatusCode { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto
            {
                [MapFrom("StatusCode")]
                public OrderStatus Status { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        Assert.Contains("(global::TestApp.OrderStatus)source.StatusCode", generatedCode);
    }

    [Fact]
    public void EnumToString_InToSource_GeneratesParse()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum OrderStatus { Pending, Shipped, Delivered }

            public class Order
            {
                public int Id { get; set; }
                public OrderStatus Status { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto
            {
                [MapFrom("Status")]
                public string StatusText { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        // In ToSource, EnumToString reverses to StringToEnum: parse back
        var toSourceIdx = generatedCode.IndexOf("ToSource()");
        Assert.True(toSourceIdx >= 0, "ToSource method should exist");
        var toSourceSection = generatedCode.Substring(toSourceIdx);

        Assert.Contains("System.Enum.Parse", toSourceSection);
        Assert.Contains("this.StatusText", toSourceSection);
    }

    [Fact]
    public void EnumToEnum_InFromSource_GeneratesCast()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum OrderStatus { Pending, Shipped, Delivered }
            public enum OrderStatusDto { Pending, Shipped, Delivered }

            public class Order
            {
                public int Id { get; set; }
                public OrderStatus Status { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto
            {
                [MapFrom("Status")]
                public OrderStatusDto Status { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        // EnumToEnum casts through int: (TargetEnum)(int)source.Prop
        Assert.Contains("(global::TestApp.OrderStatusDto)(int)source.Status", generatedCode);
    }

    [Fact]
    public void EnumToString_InProjection_GeneratesToString()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum OrderStatus { Pending, Shipped, Delivered }

            public class Order
            {
                public int Id { get; set; }
                public OrderStatus Status { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto
            {
                [MapFrom("Status")]
                public string StatusText { get; init; }
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        // Projection should also contain ToString()
        var projectionIdx = generatedCode.IndexOf("Projection");
        Assert.True(projectionIdx >= 0, "Projection should exist");
        var projectionSection = generatedCode.Substring(projectionIdx);
        Assert.Contains("source.Status.ToString()", projectionSection);
    }

    [Fact]
    public void FCT011_StringToEnum_WithProjection_EmitsWarning()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum OrderStatus { Pending, Shipped, Delivered }

            public class Order
            {
                public int Id { get; set; }
                public string Status { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto
            {
                [MapFrom("Status")]
                public OrderStatus Status { get; init; }
            }
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT011");
    }

    [Fact]
    public void EnumToString_WithConvertOverride_UsesConvert()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum OrderStatus { Pending, Shipped, Delivered }

            public class Order
            {
                public int Id { get; set; }
                public OrderStatus Status { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto
            {
                [MapFrom("Status", Convert = nameof(FormatStatus))]
                public string StatusText { get; init; }

                public static string FormatStatus(OrderStatus s) => s.ToString().ToLower();
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        // When Convert is specified, it should use the custom method, not auto enum conversion
        Assert.Contains("OrderDto.FormatStatus(source.Status)", generatedCode);
        // The auto .ToString() should NOT appear in FromSource
        var fromSourceIdx = generatedCode.IndexOf("FromSource");
        var toSourceIdx = generatedCode.IndexOf("ToSource()");
        var fromSourceSection = toSourceIdx > 0
            ? generatedCode.Substring(fromSourceIdx, toSourceIdx - fromSourceIdx)
            : generatedCode.Substring(fromSourceIdx);
        Assert.DoesNotContain("source.Status.ToString()", fromSourceSection);
    }

    [Fact]
    public void AutoGeneratedEnumProperty_KeepsSourceType()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public enum OrderStatus { Pending, Shipped, Delivered }

            public class Order
            {
                public int Id { get; set; }
                public OrderStatus Status { get; set; }
            }

            [Facette(typeof(Order))]
            public partial record OrderDto;
            """;

        var result = GeneratorTestHelper.RunGenerator(source);
        var generatedCode = result.GeneratedTrees
            .First(t => t.FilePath.Contains("OrderDto.g.cs"))
            .GetText().ToString();

        // Auto-generated property should keep the enum type as-is (no conversion)
        Assert.Contains("global::TestApp.OrderStatus Status { get; init; }", generatedCode);
        // FromSource should do a direct assignment, no ToString() or cast
        Assert.Contains("Status = source.Status", generatedCode);
    }
}
