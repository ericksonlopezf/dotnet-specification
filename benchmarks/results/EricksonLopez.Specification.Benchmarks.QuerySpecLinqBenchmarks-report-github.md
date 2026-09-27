```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 8.55 GB Available
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|------:|-------:|-------:|----------:|------------:|
| &#39;Manual LINQ&#39;                    | 831.0 μs |  1.00 | 3.9063 | 2.9297 |  68.08 KB |        1.00 |
| &#39;EricksonLopez: QuerySpec.Apply&#39; | 714.8 μs |  0.86 | 3.9063 | 2.9297 |  73.03 KB |        1.07 |
