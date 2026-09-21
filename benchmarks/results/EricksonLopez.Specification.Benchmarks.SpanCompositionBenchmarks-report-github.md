```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |---------:|------:|--------:|-------:|----------:|------------:|
| &#39;Chained .And() x4&#39;       | 1.308 μs |  1.00 |    0.00 | 0.0782 |   1.94 KB |        1.00 |
| &#39;AndAll(ReadOnlySpan) x5&#39; | 1.178 μs |  0.90 |    0.00 | 0.0782 |   1.94 KB |        1.00 |
| &#39;Chained Spec.And() x4&#39;   | 4.270 μs |  3.26 |    0.12 | 0.2136 |   5.41 KB |        2.79 |
