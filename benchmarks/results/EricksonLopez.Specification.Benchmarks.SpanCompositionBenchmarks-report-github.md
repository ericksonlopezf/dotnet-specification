```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                    | Mean       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Chained .And() x4&#39;       | 1,003.4 ns |  1.00 |    0.02 | 0.1183 |   1.94 KB |        1.00 |
| &#39;AndAll(ReadOnlySpan) x5&#39; |   871.5 ns |  0.87 |    0.01 | 0.1183 |   1.94 KB |        1.00 |
| &#39;Chained Spec.And() x4&#39;   | 3,228.7 ns |  3.22 |    0.05 | 0.3204 |   5.41 KB |        2.79 |
