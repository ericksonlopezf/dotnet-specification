```

BenchmarkDotNet v0.16.0-preview.1, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.50GHz, 1 CPU, 4 logical and 2 physical cores
Memory: 15.61 GB Total, 10.18 GB Available
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                            | Mean       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Manual delegate&#39;                                 |  0.9671 ns |  1.00 |    0.00 |      - |         - |          NA |
| &#39;EricksonLopez: IsSatisfiedBy (interpreted)&#39;      | 63.8682 ns | 66.06 |    1.50 | 0.0057 |      96 B |          NA |
| &#39;EricksonLopez: IsSatisfiedBy via compiled cache&#39; | 96.5778 ns | 99.89 |    3.16 | 0.0033 |      56 B |          NA |
