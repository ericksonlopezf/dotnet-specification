```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                      | Mean       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Manual lambda&#39;                             |   590.3 ns |  1.00 |    0.00 | 0.0315 |     800 B |        1.00 |
| &#39;EricksonLopez: Specification.ToExpression&#39; | 1,428.3 ns |  2.42 |    0.02 | 0.0839 |    2152 B |        2.69 |
| &#39;EricksonLopez: Spec.For factory&#39;           | 1,413.2 ns |  2.39 |    0.00 | 0.0858 |    2168 B |        2.71 |
