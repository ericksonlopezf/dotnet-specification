# Baseline Audit — EricksonLopez.Specification

> **Date**: 2026-08-14  
> **Auditor**: Independent Principal .NET Architect & QA/Performance Auditor  
> **Status**: Pre-Refactor / Pre-Fix Baseline  
> **Purpose**: Record the exact initial state of the repository before applying architectural and correctness fixes.

---

## 1. Execution Environment

| Component | Version / Detail |
|---|---|
| **OS** | Windows 11 Pro (10.0.26100) / win-x64 |
| **.NET SDK** | 10.0.302 (global.json: `10.0.302`, rollForward: `latestFeature`) |
| **Runtime Target** | `net10.0` (Core, Linq, Sql, Dapper, Tests, Samples), `netstandard2.0` (Analyzers, Generators) |
| **Solution** | `EricksonLopez.Specifications.slnx` (17 projects total: 9 src, 6 tests, 2 samples, 1 benchmarks) |
| **Git Commit Base** | `6991c4a` (main branch) |

---

## 2. Project Inventory (Baseline)

### 2.1 — Source Code Projects (`/src/`)

| Project | TFM | AOT Compatible | External Dependencies |
|---|---|---|---|
| `EricksonLopez.Specification.Abstractions` | `net10.0` | `true` | None (pure BCL) |
| `EricksonLopez.Specification` (Core) | `net10.0` | `true` | `Abstractions` |
| `EricksonLopez.Specification.Linq` | `net10.0` | `true` | `Abstractions` |
| `EricksonLopez.Specification.Sql` | `net10.0` | `true` | `Abstractions` |
| `EricksonLopez.Specification.PostgreSql` | `net10.0` | `true` | `Sql`, `Abstractions` |
| `EricksonLopez.Specification.Sqlite` | `net10.0` | `true` | `Sql`, `Abstractions` |
| `EricksonLopez.Specification.Dapper` | `net10.0` | `true` | `Sql`, `Abstractions`, `Dapper 2.1.66` |
| `EricksonLopez.Specification.Analyzers` | `netstandard2.0` | N/A (Roslyn) | `Microsoft.CodeAnalysis.CSharp 4.14.0` |
| `EricksonLopez.Specification.Generators` | `netstandard2.0` | N/A (Roslyn) | `Microsoft.CodeAnalysis.CSharp 4.14.0` |

### 2.2 — Test Projects (`/tests/`)

| Project | Type | Tests |
|---|---|---|
| `EricksonLopez.Specification.Tests` | Unit tests (Core + Linq) | 267 |
| `EricksonLopez.Specification.Sql.Tests` | Unit tests (SQL AST + Translation) | 149 |
| `EricksonLopez.Specification.Sqlite.Tests` | Unit tests (SQLite dialect) | 26 |
| `EricksonLopez.Specification.Analyzers.Tests` | Unit tests (SPEC001–SPEC010) | 60 |
| `EricksonLopez.Specification.Generators.Tests` | Unit tests (Source Generator stub) | 6 |
| `EricksonLopez.Specification.Dapper.Tests` | Unit + Integration tests | 3 unit + 1 container suite |
| `EricksonLopez.Specification.PostgreSql.IntegrationTests` | Integration tests | Outside `.slnx` |

### 2.3 — Samples and Benchmarks

| Project | Type | Notes |
|---|---|---|
| `samples/Showcase` | Console Demo App | 9 demonstration levels |
| `samples/NativeAotDapper` | NativeAOT Sample | AOT publish with Dapper |
| `benchmarks/EricksonLopez.Specification.Benchmarks` | BenchmarkDotNet | 6 benchmark classes |

---

## 3. Initial Build Status

### 3.1 — Debug Build (`dotnet build EricksonLopez.Specifications.slnx`)
- **Result**: ✅ Successful build with 366 warnings (CA1707, CA1515, CA1822, etc., in tests and benchmarks).
- **Errors**: 0.

### 3.2 — Release Build with `-warnaserror` (`dotnet build -c Release -warnaserror`)
- **Result**: ❌ **FAILED** with blocking errors:
  1. `NU1608`: `Microsoft.CodeAnalysis.CSharp.Workspaces 3.8.0` requires `Common 3.8.0`, but 4.14.0 resolved in `Generators.Tests`.
  2. `NU1903`: High-severity vulnerability in transitive package `SSH.NET 2024.2.0` (via Testcontainers in `Dapper.Tests`).
  3. `CS8769`: Nullability mismatch in `samples/Showcase/Levels/Level9_Extensions.cs` line 28 (`IDbConnection.ConnectionString` set implementation).

### 3.3 — NuGet Packaging (`dotnet pack EricksonLopez.Specifications.slnx`)
- **Result**: ❌ **FAILED** with error `NU5046` across `/src/` and `/samples/` projects:
  - `Directory.Build.props` specifies `<PackageIcon>icon.png</PackageIcon>` and `<PackageReadmeFile>README.md</PackageReadmeFile>`, but physical files were missing in package inclusion (`<None Include="..." Pack="true" />`).
  - `/samples/` projects attempted packing without `<IsPackable>false</IsPackable>`.

### 3.4 — NativeAOT Publishing (`dotnet publish samples/NativeAotDapper -c Release`)
- **Result**: ❌ **FAILED** with `IL2104` and `IL3053` errors:
  - `Dapper 2.1.66` triggers trimming and AOT warnings treated as errors in Release.
  - Library code (`Abstractions`, `Core`, `Sql`, `PostgreSql`, `Dapper`) compiles cleanly, but third-party Dapper required analysis suppression for the ILC compiler.

---

## 4. Initial Test Status

| Test Suite | Passed | Failed | Skipped | Status |
|---|---|---|---|---|
| `EricksonLopez.Specification.Tests` | 267 | 0 | 0 | ✅ All passed |
| `EricksonLopez.Specification.Sql.Tests` | 149 | 0 | 0 | ✅ All passed |
| `EricksonLopez.Specification.Sqlite.Tests` | 26 | 0 | 0 | ✅ All passed |
| `EricksonLopez.Specification.Analyzers.Tests` | 60 | 0 | 0 | ✅ All passed |
| `EricksonLopez.Specification.Generators.Tests` | 6 | 0 | 0 | ✅ All passed |
| `EricksonLopez.Specification.Dapper.Tests` (unit) | 3 | 0 | 0 | ✅ Unit tests passed |
| `EricksonLopez.Specification.Dapper.Tests` (integration) | 0 | 1 | 0 | ⚠️ Blocked by container timeout |
| **Total Unit Tests** | **511** | **0** | **0** | **100% unit tests passing** |

---

## 5. Initial Benchmark Status

- **Project**: `benchmarks/EricksonLopez.Specification.Benchmarks`
- **Initial Configuration**: `[SimpleJob(RuntimeMoniker.Net90)]` — **Inconsistency Detected**: The solution compiles in .NET 10 (`net10.0`), but benchmarks requested the .NET 9.0 moniker (`Net90`), causing failures when the .NET 9 SDK/runtime was unavailable.
- **Published Results**: Missing / pending execution.

---

## 6. Confirmed Findings vs. Baseline Audit

| ID | Audit Finding | Baseline Status | Verified Evidence |
|---|---|---|---|
| **BUG-01** | CA1707 and style warnings block CI `-warnaserror` | **CONFIRMED** | 362+ warnings in test projects; `-warnaserror` broke build with `NU1608`, `NU1903`, `CS8769`. |
| **BUG-02** | `ExpressionCompilationCache` uses `int hash` as key (hash collision risk) | **CONFIRMADO** | `ExpressionCompilationCache.cs` stored `ConcurrentDictionary<int, Delegate>`. |
| **BUG-03** | `QueryPlanCache` is unbounded (potential memory leak) | **CONFIRMED** | `QueryPlanCache.cs` used unbounded `ConcurrentDictionary<CacheKey, QueryModel>`. |
| **BUG-04** | `SpecificationGenerator` was a non-functional STUB | **CONFIRMED** | `SpecificationGenerator.cs` generated empty class body with placeholder comments. |
| **BUG-05** | `Level9_Extensions.cs` CS8769 nullability error | **CONFIRMED** | CS8769 error when compiling `Showcase` in Release. |
| **BUG-06** | `ExpressionInterpreter` lacked `Conditional`, `Coalesce`, `TypeIs` support | **CONFIRMED** | `ExpressionInterpreter.cs` threw `NotSupportedException`. |
| **BUG-07** | `QuerySpecTranslator` only cached when `Criteria.Length == 1` | **CONFIRMED** | `QuerySpecTranslator.cs` set `composedCriteria` to `null` if `Criteria.Length > 1`. |
| **BUG-08** | `QuerySpec<T>` tightly coupled to EF Core (`AsNoTracking`, `AsSplitQuery`) | **CONFIRMED** | `QuerySpec.cs` contained ORM-specific properties and methods. |
| **BUG-09** | Dead code and template files (`old.cs`, `UnitTest1.cs`) | **CONFIRMED** | `old.cs` (20 KB) in root and empty `UnitTest1.cs` in test projects. |
| **BUG-10** | `dotnet pack` failure from package metadata | **CONFIRMED** | `NU5046` error during `dotnet pack` due to missing physical inclusion of `icon.png` and `README.md`. |
| **BUG-11** | NativeAOT publish failure from Dapper | **CONFIRMED** | `IL2104`/`IL3053` in `NativeAotDapper` from unsuppressed analysis in Dapper assembly. |
| **BUG-12** | `BenchmarkDotNet` targeted `RuntimeMoniker.Net90` in .NET 10 project | **CONFIRMED** | `SpecificationBenchmarks.cs` used `Net90` instead of `InProcess` / .NET 10 runtime. |

---

## 7. Baseline Conclusion

The baseline confirmed that the core algorithmic and composition engine (`Specification<T>`, `ExpressionComposer`, `ExpressionSimplifier`, SQL AST) is fundamentally sound with 511 unit tests passing, but was **not release-ready** prior to the audit fixes due to:
1. Release compilation `-warnaserror` failures.
2. `dotnet pack` packaging failures.
3. NativeAOT publishing failures on samples.
4. Critical correctness bug in `ExpressionCompilationCache`.
5. Memory leak in unbounded `QueryPlanCache`.
6. Architectural coupling to EF Core inside `QuerySpec<T>`.
7. Generator stub incorrectly advertised as functional.
8. Residual files in root and test project directories.

All baseline issues were formally identified and queued for resolution.
