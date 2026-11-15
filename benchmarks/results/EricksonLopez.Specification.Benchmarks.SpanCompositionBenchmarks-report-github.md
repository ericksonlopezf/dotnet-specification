```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 10.18 GB Available
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Chained .And() x4&#39;       |   849.3 ns |  1.00 |    0.00 | 0.1183 |   1.94 KB |        1.00 |
| &#39;AndAll(ReadOnlySpan) x5&#39; |   908.6 ns |  1.07 |    0.12 | 0.1183 |   1.94 KB |        1.00 |
| &#39;Chained Spec.And() x4&#39;   | 2,546.8 ns |  3.00 |    0.13 | 0.3281 |   5.41 KB |        2.79 |
