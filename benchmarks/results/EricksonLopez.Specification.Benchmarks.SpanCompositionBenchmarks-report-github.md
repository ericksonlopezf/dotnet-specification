```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.33GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 10.02 GB Available
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |---------:|------:|--------:|-------:|----------:|------------:|
| &#39;Chained .And() x4&#39;       | 1.180 μs |  1.00 |    0.00 | 0.0229 |   1.94 KB |        1.00 |
| &#39;AndAll(ReadOnlySpan) x5&#39; | 1.163 μs |  0.99 |    0.10 | 0.0229 |   1.94 KB |        1.00 |
| &#39;Chained Spec.And() x4&#39;   | 3.375 μs |  2.89 |    0.30 | 0.0648 |   5.41 KB |        2.79 |
