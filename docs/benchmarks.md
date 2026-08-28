# Benchmarks — EricksonLopez.Specification

> **Runtime**: .NET 10.0.10 (10.0.1026.32716), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI  
> **Benchmark Engine**: BenchmarkDotNet v0.14.0  
> **Environment**: Windows 11 Pro, .NET SDK 10.0.302  
> **Status**: Verified Real Measurements (Release Build, InProcess/ShortRun)

---

## 1. Executive Performance Summary

| Metric | Result | Industry Context |
|---|---|---|
| **Binary Expression Composition (`And`)** | **93.51 ns** / 448 B | **2× faster** than manual AST lambda construction (184.71 ns / 560 B) |
| **In-Memory Interpreted Validation (AOT)** | **44.64 ns** / 96 B | **Zero dynamic IL compilation**; 100% Native AOT compatible |
| **Compiled Cached Validation (JIT)** | **64.60 ns** / 48 B | O(1) hash table lookup with structural AST equality |
| **Simple Spec → SQL Translation** | **108.63 ns** / 1.11 KB | Sub-microsecond parameter-bound AST rendering |
| **Complex Spec → SQL Translation** | **412.62 ns** / 3.13 KB | Multi-filter + sort + offset paging SQL generation |
| **`QuerySpec.Apply` LINQ Overhead** | **1.02×** vs Hand-Written LINQ | <2% overhead on `IQueryable<T>` execution |

---

## 2. Benchmark Suites & Measurements

### 2.1 — Expression Composition Performance

Compares combining two predicates (`c => c.IsActive` and `c => !c.IsDeleted`) via `ExpressionComposer.And` versus manual dynamic lambda construction.

```
BenchmarkDotNet v0.14.0, Windows 11
.NET SDK 10.0.302, .NET 10.0.10 X64 RyuJIT AVX-512
```

| Method | Mean | Ratio | Gen0 | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|
| **Manual: `x => left && right`** | 184.71 ns | 1.00 | 0.0110 | 560 B | 1.00 |
| **EricksonLopez: `ExpressionComposer.And`** | **93.51 ns** | **0.51** | **0.0088** | **448 B** | **0.80** |
| **EricksonLopez: 5-way `AND`** | 343.08 ns | 1.86 | 0.0343 | 1,736 B | 3.10 |

> **Key Takeaway**: `ExpressionComposer.And` outperforms manual lambda creation by **49% in time** and **20% in memory allocation** thanks to optimized single-pass parameter rebinding via `ParameterReplacer`.

---

### 2.2 — Specification In-Memory Evaluation Performance

Evaluates a composite specification (`ActiveCustomerSpec.And(NotDeletedSpec)`) against a candidate object.

| Method | Mean | Median | Gen0 | Allocated | Description |
|---|---:|---:|---:|---:|---|
| **Manual delegate** | 0.0017 ns | 0.0004 ns | - | - | Direct compiled C# delegate (JIT inlined) |
| **EricksonLopez: `IsSatisfiedBy` (interpreted)** | **44.64 ns** | **44.63 ns** | 0.0019 | **96 B** | **AOT-Safe (Zero IL emitted)** |
| **EricksonLopez: `IsSatisfiedBy` (compiled cache)** | **64.60 ns** | **64.49 ns** | 0.0010 | **48 B** | JIT Cached Delegate |

> **Key Takeaway**: In-memory interpreted evaluation executes in just **44.6 nanoseconds**, enabling sub-microsecond validation on Native AOT without needing `Expression.Compile()`.

---

### 2.3 — SQL AST Translation & Rendering Performance

Measures translating a `QuerySpec<T>` into a SQL string and parameter dictionary for PostgreSQL (`PostgreSqlDialect`).

| Method | Mean | Gen0 | Allocated | Notes |
|---|---:|---:|---:|---|
| **EricksonLopez: Simple spec → SQL** | **108.63 ns** | 0.0225 | 1.11 KB | Single filter (`WHERE is_active = $1`) |
| **EricksonLopez: Complex spec → SQL** | **412.62 ns** | 0.0634 | 3.13 KB | 3 filters + `ORDER BY` + `LIMIT/OFFSET` |

> **Key Takeaway**: Query translation to native SQL AST takes under **0.5 microseconds**, making it virtually free compared to database network latency (~1-5 ms).

---

### 2.4 — LINQ Provider Overhead (`QuerySpec.Apply`)

Measures applying a `QuerySpec<T>` with filtering, sorting, and paging against an `IQueryable<T>` data source of 1,000 entities.

| Method | Mean | Ratio | Gen0 | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|
| **Manual LINQ** | 688.7 μs | 1.00 | 0.9766 | 67.96 KB | 1.00 |
| **EricksonLopez: `QuerySpec.Apply`** | **702.2 μs** | **1.02** | 0.9766 | 67.12 KB | **0.99** |

> **Key Takeaway**: `QuerySpec.Apply` introduces **negligible overhead (~1.9%)** and zero additional heap allocations compared to raw LINQ queries.

---

### 2.5 — Span-Based Predicate Composition (`AndAll`)

Compares bulk composition of 5 predicates via `ReadOnlySpan` versus chained `.And()` calls.

| Method | Mean | Ratio | Gen0 | Allocated | Alloc Ratio |
|---|---:|---:|---:|---:|---:|
| **Chained `.And()` × 4** | 520.0 ns | 1.00 | 0.0391 | 1.94 KB | 1.00 |
| **`AndAll(ReadOnlySpan)` × 5** | **477.6 ns** | **0.92** | 0.0391 | 1.94 KB | **1.00** |
| **Chained `Spec.And()` × 4** | 2,066.3 ns | 3.97 | 0.1068 | 5.41 KB | 2.79 |

> **Key Takeaway**: `ExpressionComposer.AndAll` using `ReadOnlySpan` is **8% faster** than chaining individual `.And()` calls when combining multiple specifications dynamically.

---

## 3. Reproduction Instructions

To run these benchmarks on your local machine:

```bash
# Build the benchmark project in Release mode
dotnet build benchmarks/EricksonLopez.Specification.Benchmarks/EricksonLopez.Specification.Benchmarks.csproj -c Release

# Execute all benchmark suites
dotnet run --project benchmarks/EricksonLopez.Specification.Benchmarks/EricksonLopez.Specification.Benchmarks.csproj -c Release -- --filter *
```
