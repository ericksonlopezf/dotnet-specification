```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |---------:|------:|--------:|-------:|----------:|------------:|
| &#39;Chained .And() x4&#39;       | 1.351 μs |  1.00 |    0.01 | 0.1183 |   1.94 KB |        1.00 |
| &#39;AndAll(ReadOnlySpan) x5&#39; | 1.232 μs |  0.91 |    0.01 | 0.1183 |   1.94 KB |        1.00 |
| &#39;Chained Spec.And() x4&#39;   | 4.217 μs |  3.12 |    0.04 | 0.3052 |   5.41 KB |        2.79 |
