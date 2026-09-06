```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                  | Mean     | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------------------------- |---------:|------:|-------:|----------:|------------:|
| &#39;Manual: x =&gt; left &amp;&amp; right&#39;            | 349.5 ns |  1.00 | 0.0334 |     560 B |        1.00 |
| &#39;EricksonLopez: ExpressionComposer.And&#39; | 180.4 ns |  0.52 | 0.0267 |     448 B |        0.80 |
| &#39;EricksonLopez: 5-way AND&#39;              | 665.6 ns |  1.90 | 0.1030 |    1736 B |        3.10 |
