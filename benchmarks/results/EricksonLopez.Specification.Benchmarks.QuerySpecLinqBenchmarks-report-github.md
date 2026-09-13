```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Manual LINQ&#39;                    | 854.7 μs |  1.00 |    0.05 | 3.9063 | 1.9531 |  67.83 KB |        1.00 |
| &#39;EricksonLopez: QuerySpec.Apply&#39; | 855.6 μs |  1.00 |    0.05 | 3.9063 | 1.9531 |  67.01 KB |        0.99 |
