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
| Method                                  | Mean     | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|------:|-------:|----------:|------------:|
| &#39;Manual: x =&gt; left &amp;&amp; right&#39;            | 374.3 ns |  1.00 | 0.0067 |     560 B |        1.00 |
| &#39;EricksonLopez: ExpressionComposer.And&#39; | 221.7 ns |  0.59 | 0.0052 |     448 B |        0.80 |
| &#39;EricksonLopez: 5-way AND&#39;              | 832.8 ns |  2.23 | 0.0200 |    1736 B |        3.10 |
