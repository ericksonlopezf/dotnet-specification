```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Manual LINQ&#39;                    | 831.2 μs |  1.00 |    0.00 | 3.9063 | 2.9297 |  67.83 KB |        1.00 |
| &#39;EricksonLopez: QuerySpec.Apply&#39; | 857.8 μs |  1.03 |    0.02 | 3.9063 | 1.9531 |  67.12 KB |        0.99 |
