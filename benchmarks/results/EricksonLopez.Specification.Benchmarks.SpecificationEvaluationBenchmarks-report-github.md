```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                            | Mean        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------- |------------:|-------:|--------:|-------:|----------:|------------:|
| &#39;Manual delegate&#39;                                 |   0.5889 ns |   1.00 |    0.00 |      - |         - |          NA |
| &#39;EricksonLopez: IsSatisfiedBy (interpreted)&#39;      |  97.7964 ns | 166.07 |    0.51 | 0.0038 |      96 B |          NA |
| &#39;EricksonLopez: IsSatisfiedBy via compiled cache&#39; | 149.8081 ns | 254.39 |    0.40 | 0.0019 |      48 B |          NA |
