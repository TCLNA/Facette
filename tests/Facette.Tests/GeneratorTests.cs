using Facette.Tests.Helpers;

namespace Facette.Tests;

public class GeneratorTests
{
    [Fact]
    public void Generator_WithSimpleEntity_GeneratesOutput()
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

        Assert.NotEmpty(result.GeneratedTrees);
    }
}
