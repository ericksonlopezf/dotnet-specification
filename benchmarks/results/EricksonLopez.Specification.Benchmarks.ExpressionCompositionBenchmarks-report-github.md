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
| Method                                  | Mean     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|------:|--------:|-------:|----------:|------------:|
| &#39;Manual: x =&gt; left &amp;&amp; right&#39;            | 246.8 ns |  1.00 |    0.00 | 0.0334 |     560 B |        1.00 |
| &#39;EricksonLopez: ExpressionComposer.And&#39; | 137.2 ns |  0.56 |    0.02 | 0.0267 |     448 B |        0.80 |
| &#39;EricksonLopez: 5-way AND&#39;              | 513.8 ns |  2.08 |    0.08 | 0.1030 |    1736 B |        3.10 |
