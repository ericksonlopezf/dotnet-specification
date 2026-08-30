```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                              | Mean       | Gen0   | Allocated |
|------------------------------------ |-----------:|-------:|----------:|
| &#39;EricksonLopez: Simple spec → SQL&#39;  |   424.9 ns | 0.0749 |   1.23 KB |
| &#39;EricksonLopez: Complex spec → SQL&#39; | 2,404.3 ns | 0.2480 |   4.09 KB |
