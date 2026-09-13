```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Chained .And() x4&#39;       |   947.7 ns |  1.00 |    0.01 | 0.1183 |   1.94 KB |        1.00 |
| &#39;AndAll(ReadOnlySpan) x5&#39; |   851.0 ns |  0.90 |    0.01 | 0.1183 |   1.94 KB |        1.00 |
| &#39;Chained Spec.And() x4&#39;   | 3,326.5 ns |  3.51 |    0.16 | 0.3357 |   5.49 KB |        2.83 |
