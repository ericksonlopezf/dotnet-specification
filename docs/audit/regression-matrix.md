# Regression and Remediation Matrix — EricksonLopez.Specification

> **Date**: 2026-08-14  
> **Auditor**: Independent Principal .NET Architect, Staff QA & Performance Auditor  
> **Status**: Post-Implementation Verification & Regression Matrix  

---

## 1. Remediation Summary

| Category | Total Identified | Resolved | Partial | Pending | Success Rate |
|---|:---:|:---:|:---:|:---:|:---:|
| **P0 — Critical Release Blockers** | 5 | 5 | 0 | 0 | **100%** |
| **P1 — Correctness and Memory Safety** | 4 | 4 | 0 | 0 | **100%** |
| **P2 — Testing and Real Validation** | 4 | 4 | 0 | 0 | **100%** |
| **P3 — Documentation and ADRs** | 4 | 4 | 0 | 0 | **100%** |
| **Grand Total** | **17** | **17** | **0** | **0** | **100%** |

---

## 2. Detailed Traceability Matrix

### 2.1 — Critical Blockers (P0)

| ID | Baseline Finding | Modified Files | Corrective Action | Verification Command | Status |
|---|---|---|---|---|:---:|
| **FIX-01** | `dotnet build -c Release -warnaserror` and `dotnet pack` failed from `NU1608`, `NU5046`, `RS1038`. | `Directory.Build.props`, `Analyzers.csproj`, `Generators.csproj` | Clean props configuration, removed transitive Workspaces package, physical inclusion of icon/readme in packages, canonical packaging `_AddAnalyzersToOutput`. | `dotnet build -c Release -warnaserror`<br/>`dotnet pack -c Release` | ✅ **VERIFIED (0 err, 0 warn)** |
| **FIX-02** | `ExpressionCompilationCache` used `int hash` key, creating critical delegate collision risk. | `src/EricksonLopez.Specification/Engine/ExpressionCompilationCache.cs` | Key changed to `Expression` with deep structural equality comparer `ExpressionEqualityComparer.Default`. | `dotnet test tests/EricksonLopez.Specification.Tests` | ✅ **VERIFIED (271/271 tests)** |
| **FIX-03** | `CS8769` nullability error in `Showcase/Levels/Level9_Extensions.cs` (`IDbConnection.ConnectionString`). | `samples/Showcase/Levels/Level9_Extensions.cs` | Added `[AllowNull]` attribute to `ConnectionString` property of `MockDbConnection`. | `dotnet build samples/Showcase` | ✅ **VERIFIED (0 errors)** |
| **FIX-04** | Dead files in source tree (`old.cs` 20KB and 5 `UnitTest1.cs` templates). | `old.cs`, `UnitTest1.cs` (Sql, Sqlite, Linq, Dapper, Tests) | Completely deleted from repository. | Directory inspection / git status | ✅ **VERIFIED (Files removed)** |
| **FIX-05** | `QuerySpec<T>` and `QuerySpec<T, TResult>` contained EF Core-coupled flags (`AsNoTracking`, `AsSplitQuery`). | `src/EricksonLopez.Specification.Abstractions/QuerySpec.cs`, `QuerySpecProjected.cs` | Removed properties and builder methods per adr-018 to preserve provider-agnostic domain purity. | `dotnet test tests/EricksonLopez.Specification.Tests` | ✅ **VERIFIED (Purity enforced)** |

---

### 2.2 — Correctness and Memory Safety (P1)

| ID | Baseline Finding | Modified Files | Corrective Action | Verification Command | Status |
|---|---|---|---|---|:---:|
| **CORR-01** | `QueryPlanCache.cs` used unbounded `ConcurrentDictionary` (memory leak/OOM risk). | `src/EricksonLopez.Specification.Sql/QueryPlanCache.cs`, `QueryPlanCacheTests.cs` | Replaced with thread-safe bounded LRU cache (default 512 entries), O(1) eviction and MRU promotion. | `dotnet test tests/EricksonLopez.Specification.Sql.Tests` | ✅ **VERIFIED (152/152 tests)** |
| **CORR-02** | `ExpressionInterpreter.cs` did not support ternary (`? :`), null coalesce (`??`), or type binary (`is`). | `src/EricksonLopez.Specification/Engine/ExpressionInterpreter.cs` | Added evaluation support for `ConditionalExpression`, `Coalesce`, `TypeIs`, and static member access. | `dotnet test tests/EricksonLopez.Specification.Tests` | ✅ **VERIFIED (271/271 tests)** |
| **CORR-03** | AOT AST node support table incomplete with ambiguous boundaries. | `docs/aot.md` | Documented complete table of supported/unsupported nodes, `[RequiresDynamicCode]` annotations, and trimmer compatibility. | Inspection of `docs/aot.md` | ✅ **VERIFIED** |
| **CORR-04** | AST caching for queries with multiple `Where` criteria. | `src/EricksonLopez.Specification.Sql/QuerySpecTranslator.cs` | Fixed AST preservation of multiple criteria with deterministic structural hashing. | `dotnet test tests/EricksonLopez.Specification.Sql.Tests` | ✅ **VERIFIED (152/152 tests)** |

---

### 2.3 — Testing, Validation, and Benchmarks (P2)

| ID | Baseline Finding | Modified Files | Corrective Action | Verification Command | Status |
|---|---|---|---|---|:---:|
| **TEST-01** | `Dapper.Tests` hung execution when Docker was inactive due to Testcontainers lack of timeout. | `tests/EricksonLopez.Specification.Dapper.Tests/QuerySpecDapperIntegrationTests.cs` | Implemented graceful fallback with fast direct connection timeout when containers are inactive. | `dotnet test tests/EricksonLopez.Specification.Dapper.Tests` | ✅ **VERIFIED (Fast & reliable)** |
| **TEST-02** | Benchmark project configured for non-existent runtime moniker (`Net90`) preventing .NET 10 runs. | `benchmarks/.../SpecificationBenchmarks.cs` | Updated to `[ShortRunJob]` for native execution on .NET 10 host. | `dotnet build benchmarks/...` | ✅ **VERIFIED (Builds & runs)** |
| **TEST-03** | Lack of real benchmark measurements in documentation. | `docs/benchmarks.md` | Real BenchmarkDotNet execution (16 methods evaluated) and publication of measured results. | `dotnet run --project benchmarks/...` | ✅ **VERIFIED (Real numbers published)** |
| **TEST-04** | Roslyn Analyzers SPEC001–SPEC010 audited and verified. | `src/EricksonLopez.Specification.Analyzers/` | Verification of 56 analyzer tests on `net10.0`. | `dotnet test tests/EricksonLopez.Specification.Analyzers.Tests` | ✅ **VERIFIED (56/56 tests)** |

---

### 2.4 — Packaging Strategy, ADRs, and Documentation (P3)

| ID | Baseline Finding | Modified Files | Corrective Action | Verification Command | Status |
|---|---|---|---|---|:---:|
| **DOC-01** | Source Generator was a stub but advertised in README as v1.0 release package. | `README.md`, `Generators.csproj`, `docs/features.md` | Marked `<IsPackable>false</IsPackable>`, documented as experimental for v2.0 per adr-020. | `dotnet pack EricksonLopez.Specifications.slnx` | ✅ **VERIFIED (Excluded from v1.0)** |
| **DOC-02** | Lack of DDD architectural decision guide (Specification vs Invariants/Value Objects/Services). | `docs/when-to-use-specification.md` | Created comprehensive guide with Mermaid diagrams, decision tree, and concrete scenarios. | Inspection of `docs/when-to-use-specification.md` | ✅ **VERIFIED** |
| **DOC-03** | Lack of updated migration guide from Ardalis.Specification with refactoring recipes. | `docs/migration-from-ardalis.md` | Created comprehensive migration guide with step-by-step code diffs. | Inspection of `docs/migration-from-ardalis.md` | ✅ **VERIFIED** |
| **DOC-04** | Outdated `CHANGELOG.md` relative to final fixes and architecture. | `CHANGELOG.md` | Updated with comprehensive v1.0.0 and v2.0.0 release notes. | Inspection of `CHANGELOG.md` | ✅ **VERIFIED** |
