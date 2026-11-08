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
| Method                              | Mean       | Gen0   | Allocated |
|------------------------------------ |-----------:|-------:|----------:|
| &#39;EricksonLopez: Simple spec → SQL&#39;  |   312.4 ns | 0.1001 |   1.64 KB |
| &#39;EricksonLopez: Complex spec → SQL&#39; | 1,382.1 ns | 0.2499 |    4.1 KB |
