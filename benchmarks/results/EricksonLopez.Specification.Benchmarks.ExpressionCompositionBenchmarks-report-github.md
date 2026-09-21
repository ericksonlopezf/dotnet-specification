```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean     | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|------:|-------:|----------:|------------:|
| &#39;Manual: x =&gt; left &amp;&amp; right&#39;            | 455.0 ns |  1.00 | 0.0219 |     560 B |        1.00 |
| &#39;EricksonLopez: ExpressionComposer.And&#39; | 252.7 ns |  0.56 | 0.0176 |     448 B |        0.80 |
| &#39;EricksonLopez: 5-way AND&#39;              | 923.0 ns |  2.03 | 0.0687 |    1736 B |        3.10 |
