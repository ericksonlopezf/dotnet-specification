```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 6.83 GB Available
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |---------:|------:|--------:|-------:|----------:|------------:|
| &#39;Chained .And() x4&#39;       | 1.523 μs |  1.00 |    0.00 | 0.1183 |   1.94 KB |        1.00 |
| &#39;AndAll(ReadOnlySpan) x5&#39; | 1.451 μs |  0.95 |    0.01 | 0.1183 |   1.94 KB |        1.00 |
| &#39;Chained Spec.And() x4&#39;   | 4.770 μs |  3.13 |    0.02 | 0.3281 |   5.41 KB |        2.79 |
