```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 8.62 GB Available
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean       | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |-----------:|------:|-------:|-------:|----------:|------------:|
| &#39;Manual LINQ&#39;                    | 1,061.5 μs |  1.00 | 3.9063 | 1.9531 |  68.07 KB |        1.00 |
| &#39;EricksonLopez: QuerySpec.Apply&#39; |   920.5 μs |  0.87 | 3.9063 | 2.9297 |  73.18 KB |        1.08 |
