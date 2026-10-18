```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 6.98 GB Available
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                            | Mean       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Manual delegate&#39;                                 |   1.927 ns |  1.00 |    0.00 |      - |         - |          NA |
| &#39;EricksonLopez: IsSatisfiedBy (interpreted)&#39;      | 116.909 ns | 60.66 |    0.22 | 0.0057 |      96 B |          NA |
| &#39;EricksonLopez: IsSatisfiedBy via compiled cache&#39; | 165.931 ns | 86.10 |    0.31 | 0.0033 |      56 B |          NA |
