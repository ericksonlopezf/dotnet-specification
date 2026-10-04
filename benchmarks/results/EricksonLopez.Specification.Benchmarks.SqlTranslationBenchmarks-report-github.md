```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 8.57 GB Available
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                              | Mean       | Gen0   | Allocated |
|------------------------------------ |-----------:|-------:|----------:|
| &#39;EricksonLopez: Simple spec → SQL&#39;  |   544.5 ns | 0.1001 |   1.64 KB |
| &#39;EricksonLopez: Complex spec → SQL&#39; | 2,334.5 ns | 0.2480 |    4.1 KB |
