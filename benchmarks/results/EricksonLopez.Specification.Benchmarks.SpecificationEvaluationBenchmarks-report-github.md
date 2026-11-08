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
| Method                                            | Mean       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------- |-----------:|------:|--------:|-------:|----------:|------------:|
| &#39;Manual delegate&#39;                                 |  0.9394 ns |  1.00 |    0.00 |      - |         - |          NA |
| &#39;EricksonLopez: IsSatisfiedBy (interpreted)&#39;      | 63.0904 ns | 67.16 |    2.86 | 0.0057 |      96 B |          NA |
| &#39;EricksonLopez: IsSatisfiedBy via compiled cache&#39; | 90.9347 ns | 96.80 |    2.89 | 0.0033 |      56 B |          NA |
