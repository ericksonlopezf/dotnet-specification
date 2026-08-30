```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                            | Mean        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------- |------------:|-------:|--------:|-------:|----------:|------------:|
| &#39;Manual delegate&#39;                                 |   0.5167 ns |   1.00 |    0.01 |      - |         - |          NA |
| &#39;EricksonLopez: IsSatisfiedBy (interpreted)&#39;      | 101.0142 ns | 195.52 |    1.09 | 0.0057 |      96 B |          NA |
| &#39;EricksonLopez: IsSatisfiedBy via compiled cache&#39; | 141.7960 ns | 274.45 |    2.73 | 0.0029 |      48 B |          NA |
