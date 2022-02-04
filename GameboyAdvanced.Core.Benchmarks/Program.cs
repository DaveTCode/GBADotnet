using BenchmarkDotNet.Running;
using GameboyAdvanced.Core.Benchmarks;

var generatorBenchmarkPerfSummary = BenchmarkRunner.Run<GeneratorBenchmark>();