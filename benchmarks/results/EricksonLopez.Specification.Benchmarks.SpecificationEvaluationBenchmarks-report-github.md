```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                            | Mean        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------- |------------:|-------:|--------:|-------:|----------:|------------:|
| &#39;Manual delegate&#39;                                 |   0.2752 ns |   1.00 |    0.01 |      - |         - |          NA |
| &#39;EricksonLopez: IsSatisfiedBy (interpreted)&#39;      |  80.8686 ns | 293.84 |    2.15 | 0.0057 |      96 B |          NA |
| &#39;EricksonLopez: IsSatisfiedBy via compiled cache&#39; | 115.6158 ns | 420.10 |    3.08 | 0.0029 |      48 B |          NA |
