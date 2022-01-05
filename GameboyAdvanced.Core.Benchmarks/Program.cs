using BenchmarkDotNet.Running;
using GameboyAdvanced.Core.Benchmarks;

var utilsPerfSummary = BenchmarkRunner.Run<UtilsBenchmarks>();