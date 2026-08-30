```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                           | Mean     | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------------------- |---------:|------:|-------:|-------:|----------:|------------:|
| &#39;Manual LINQ&#39;                    | 1.094 ms |  1.00 | 3.9063 | 1.9531 |  67.83 KB |        1.00 |
| &#39;EricksonLopez: QuerySpec.Apply&#39; | 1.117 ms |  1.02 | 3.9063 | 1.9531 |  67.01 KB |        0.99 |
