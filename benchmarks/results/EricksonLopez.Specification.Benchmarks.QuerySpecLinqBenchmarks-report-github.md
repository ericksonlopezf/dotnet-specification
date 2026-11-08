```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.44GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 10.2 GB Available
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|------:|-------:|-------:|----------:|------------:|
| &#39;Manual LINQ&#39;                    | 682.1 μs |  1.00 | 3.9063 | 2.9297 |  67.98 KB |        1.00 |
| &#39;EricksonLopez: QuerySpec.Apply&#39; | 553.3 μs |  0.81 | 3.9063 | 2.9297 |  73.03 KB |        1.07 |
