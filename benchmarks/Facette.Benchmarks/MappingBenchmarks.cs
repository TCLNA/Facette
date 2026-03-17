using BenchmarkDotNet.Attributes;
using Facette.Abstractions;

namespace Facette.Benchmarks;

public class Source
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

[Facette(typeof(Source))]
public partial record SourceDto;

[MemoryDiagnoser]
[ShortRunJob]
public class MappingBenchmarks
{
    private Source _source = null!;
    private SourceDto _dto = null!;
    private List<Source> _sources = null!;

    [GlobalSetup]
    public void Setup()
    {
        _source = new Source
        {
            Id = 1,
            Name = "Alice",
            Email = "alice@example.com",
            Age = 30,
            IsActive = true
        };
        _dto = SourceDto.FromSource(_source);

        _sources = Enumerable.Range(1, 1000)
            .Select(i => new Source
            {
                Id = i,
                Name = "User" + i,
                Email = "user" + i + "@example.com",
                Age = 20 + (i % 50),
                IsActive = i % 2 == 0
            })
            .ToList();
    }

    [Benchmark(Baseline = true)]
    public SourceDto FromSource_Facette()
    {
        return SourceDto.FromSource(_source);
    }

    [Benchmark]
    public Source FromSource_HandWritten()
    {
        // Simulate what the generated code does, but hand-written
        var result = new Source
        {
            Id = _source.Id,
            Name = _source.Name,
            Email = _source.Email,
            Age = _source.Age,
            IsActive = _source.IsActive
        };
        return result;
    }

    [Benchmark]
    public Source ToSource_Facette()
    {
        return _dto.ToSource();
    }

    [Benchmark]
    public List<SourceDto> CollectionMapping_1000Items()
    {
        return _sources.Select(SourceDto.FromSource).ToList();
    }
}
