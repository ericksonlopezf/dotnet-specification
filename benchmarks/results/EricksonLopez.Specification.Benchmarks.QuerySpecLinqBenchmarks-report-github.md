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
| Method                           | Mean     | Ratio | Allocated | Alloc Ratio |
|--------------------------------- |---------:|------:|----------:|------------:|
| &#39;Manual LINQ&#39;                    | 957.0 μs |  1.00 |  67.92 KB |        1.00 |
| &#39;EricksonLopez: QuerySpec.Apply&#39; | 712.5 μs |  0.74 |  72.94 KB |        1.07 |
