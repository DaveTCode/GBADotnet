using BenchmarkDotNet.Attributes;

namespace GameboyAdvanced.Core.Benchmarks;

internal class GeneratorBenchmark
{
    [Benchmark]
    public void BenchmarkGenerator()
    {
        static IEnumerator<int> GetEnumerator()
        {
            yield return 1;
        }

        var enumerator = GetEnumerator();

        _ = enumerator.MoveNext();
    }

    [Benchmark]
    public void BenchmarkFunctionCall()
    {
        static int Get()
        {
            return 1;
        }

        _ = Get();
    }
}
