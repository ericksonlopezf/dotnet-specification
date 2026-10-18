```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 6.98 GB Available
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Manual: x =&gt; left &amp;&amp; right&#39;            |   583.0 ns |  1.00 |    0.00 | 0.0334 |     560 B |        1.00 |
| &#39;EricksonLopez: ExpressionComposer.And&#39; |   321.9 ns |  0.55 |    0.01 | 0.0267 |     448 B |        0.80 |
| &#39;EricksonLopez: 5-way AND&#39;              | 1,247.7 ns |  2.14 |    0.02 | 0.1030 |    1736 B |        3.10 |
