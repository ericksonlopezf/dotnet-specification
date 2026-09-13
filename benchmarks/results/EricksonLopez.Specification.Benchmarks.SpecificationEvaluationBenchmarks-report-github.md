```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                            | Mean        | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------------- |------------:|-------:|--------:|-------:|----------:|------------:|
| &#39;Manual delegate&#39;                                 |   0.2747 ns |   1.00 |    0.00 |      - |         - |          NA |
| &#39;EricksonLopez: IsSatisfiedBy (interpreted)&#39;      |  79.5112 ns | 289.41 |    0.29 | 0.0057 |      96 B |          NA |
| &#39;EricksonLopez: IsSatisfiedBy via compiled cache&#39; | 112.3809 ns | 409.06 |    0.34 | 0.0029 |      48 B |          NA |
