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
| Method                                      | Mean       | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------- |-----------:|------:|-------:|----------:|------------:|
| &#39;Manual lambda&#39;                             |   735.7 ns |  1.00 | 0.0477 |     800 B |        1.00 |
| &#39;EricksonLopez: Specification.ToExpression&#39; | 1,598.6 ns |  2.17 | 0.1278 |    2152 B |        2.69 |
| &#39;EricksonLopez: Spec.For factory&#39;           | 1,613.5 ns |  2.19 | 0.1278 |    2168 B |        2.71 |
