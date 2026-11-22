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
| Method                              | Mean       | Gen0   | Allocated |
|------------------------------------ |-----------:|-------:|----------:|
| &#39;EricksonLopez: Simple spec → SQL&#39;  |   484.4 ns | 0.0200 |   1.64 KB |
| &#39;EricksonLopez: Complex spec → SQL&#39; | 1,744.0 ns | 0.0496 |    4.1 KB |
