```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|------:|-------:|----------:|------------:|
| &#39;Manual LINQ&#39;                    | 1.062 ms |  1.00 | 1.9531 |  67.89 KB |        1.00 |
| &#39;EricksonLopez: QuerySpec.Apply&#39; | 1.067 ms |  1.00 | 1.9531 |  66.99 KB |        0.99 |
