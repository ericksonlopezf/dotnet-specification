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
| Method                                      | Mean       | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------- |-----------:|------:|-------:|----------:|------------:|
| &#39;Manual lambda&#39;                             |   547.9 ns |  1.00 | 0.0095 |     800 B |        1.00 |
| &#39;EricksonLopez: Specification.ToExpression&#39; | 1,283.0 ns |  2.34 | 0.0248 |    2152 B |        2.69 |
| &#39;EricksonLopez: Spec.For factory&#39;           | 1,173.9 ns |  2.14 | 0.0248 |    2168 B |        2.71 |
