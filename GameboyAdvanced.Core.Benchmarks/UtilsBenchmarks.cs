using BenchmarkDotNet.Attributes;

namespace GameboyAdvanced.Core.Benchmarks;

public class UtilsBenchmarks
{
    private byte[] _bytes = Array.Empty<byte>();

    [Params(0, 0x3FF)]
    public uint Address;

    [GlobalSetup]
    public void Setup()
    {
        _bytes = new byte[0x400];
        new Random().NextBytes(_bytes);
    }

    [Benchmark]
    public uint BenchmarkReadWord() => _ = Utils.ReadWord(_bytes, Address, 0x3FF);


    [Benchmark]
    public void BenchmarkWriteWord() => Utils.WriteWord(_bytes, 0x3FF, Address, 0x123456);
}
