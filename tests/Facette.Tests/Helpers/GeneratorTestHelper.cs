using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Facette.Tests.Helpers;

public static class GeneratorTestHelper
{
    public static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        // Add Facette.Abstractions reference
        references.Add(MetadataReference.CreateFromFile(
            typeof(Facette.Abstractions.FacetteAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new Facette.Generator.FacetteGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var diagnostics);

        return driver.GetRunResult();
    }

    public static (GeneratorDriverRunResult Result, IReadOnlyList<Diagnostic> Diagnostics) RunGeneratorWithDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        references.Add(MetadataReference.CreateFromFile(
            typeof(Facette.Abstractions.FacetteAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new Facette.Generator.FacetteGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var diagnostics);

        var result = driver.GetRunResult();

        // Generator-reported diagnostics appear in the run result, not compilation diagnostics
        var allDiagnostics = result.Diagnostics
            .Where(d => d.Id.StartsWith("FCT"))
            .Concat(outputCompilation.GetDiagnostics()
                .Where(d => d.Id.StartsWith("FCT")))
            .ToList();

        return (result, allDiagnostics);
    }
}
