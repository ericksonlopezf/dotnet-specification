```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean     | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|------:|-------:|----------:|------------:|
| &#39;Manual: x =&gt; left &amp;&amp; right&#39;            | 321.4 ns |  1.00 | 0.0334 |     560 B |        1.00 |
| &#39;EricksonLopez: ExpressionComposer.And&#39; | 173.6 ns |  0.54 | 0.0267 |     448 B |        0.80 |
| &#39;EricksonLopez: 5-way AND&#39;              | 631.8 ns |  1.97 | 0.1030 |    1736 B |        3.10 |
