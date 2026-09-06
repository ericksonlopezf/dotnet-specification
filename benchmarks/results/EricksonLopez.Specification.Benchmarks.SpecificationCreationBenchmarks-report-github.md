```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.400
  [Host]   : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  ShortRun : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                      | Mean       | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------------- |-----------:|------:|-------:|----------:|------------:|
| &#39;Manual lambda&#39;                             |   431.7 ns |  1.00 | 0.0477 |     800 B |        1.00 |
| &#39;EricksonLopez: Specification.ToExpression&#39; | 1,019.2 ns |  2.36 | 0.1278 |    2152 B |        2.69 |
| &#39;EricksonLopez: Spec.For factory&#39;           | 1,030.4 ns |  2.39 | 0.1278 |    2168 B |        2.71 |
