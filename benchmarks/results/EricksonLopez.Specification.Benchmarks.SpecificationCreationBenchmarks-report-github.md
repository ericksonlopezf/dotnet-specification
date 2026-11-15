```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 10.18 GB Available
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                      | Mean     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------- |---------:|------:|--------:|-------:|----------:|------------:|
| &#39;Manual lambda&#39;                             | 348.6 ns |  1.00 |    0.00 | 0.0477 |     800 B |        1.00 |
| &#39;EricksonLopez: Specification.ToExpression&#39; | 841.1 ns |  2.41 |    0.04 | 0.1278 |    2152 B |        2.69 |
| &#39;EricksonLopez: Spec.For factory&#39;           | 855.8 ns |  2.46 |    0.04 | 0.1287 |    2168 B |        2.71 |
