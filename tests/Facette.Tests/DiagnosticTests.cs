using Facette.Tests.Helpers;

namespace Facette.Tests;

public class DiagnosticTests
{
    [Fact]
    public void Diagnostic_FCT002_IncludeAndExcludeBothSpecified()
    {
        var source = """
            using Facette.Abstractions;

            namespace TestApp;

            public class User
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public string Password { get; set; }
            }

            [Facette(typeof(User), "Password", Include = new[] { "Id" })]
            public partial record UserDto;
            """;

        var (result, diagnostics) = GeneratorTestHelper.RunGeneratorWithDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == "FCT002");
    }
}
