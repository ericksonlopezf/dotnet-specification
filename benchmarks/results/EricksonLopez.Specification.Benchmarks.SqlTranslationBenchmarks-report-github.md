```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                              | Mean       | Gen0   | Allocated |
|------------------------------------ |-----------:|-------:|----------:|
| &#39;EricksonLopez: Simple spec → SQL&#39;  |   406.1 ns | 0.0496 |   1.23 KB |
| &#39;EricksonLopez: Complex spec → SQL&#39; | 2,402.6 ns | 0.1640 |   4.09 KB |
