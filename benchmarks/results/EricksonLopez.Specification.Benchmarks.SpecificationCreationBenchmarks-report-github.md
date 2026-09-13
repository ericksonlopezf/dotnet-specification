```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                      | Mean     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------- |---------:|------:|--------:|-------:|----------:|------------:|
| &#39;Manual lambda&#39;                             | 416.2 ns |  1.00 |    0.02 | 0.0477 |     800 B |        1.00 |
| &#39;EricksonLopez: Specification.ToExpression&#39; | 981.6 ns |  2.36 |    0.03 | 0.1278 |    2152 B |        2.69 |
| &#39;EricksonLopez: Spec.For factory&#39;           | 984.2 ns |  2.37 |    0.03 | 0.1278 |    2168 B |        2.71 |
