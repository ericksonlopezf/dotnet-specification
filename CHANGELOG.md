# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2026-08-28

### 💥 Breaking Changes

- **BC-001 (Purity & ORM Decoupling):** Removed `AsNoTracking` property and `.NoTracking()` fluent method from `QuerySpec<T>` and `QuerySpec<T, TResult>` ([ADR-018](docs/adr/adr-018-remove-asnotracking-splitquery-from-queryspec.md)).
  - *Impact:* Query descriptors are now strictly provider-agnostic.
  - *Migration:* Apply `.AsNoTracking()` directly to your `IQueryable<T>` or `DbContext.Set<T>()` in the Infrastructure repository prior to calling `.Apply(querySpec)`.

- **BC-002 (Purity & ORM Decoupling):** Removed `AsSplitQuery` property and `.SplitQuery()` fluent method from `QuerySpec<T>` and `QuerySpec<T, TResult>` ([ADR-018](docs/adr/adr-018-remove-asnotracking-splitquery-from-queryspec.md)).
  - *Impact:* Query splitting is strictly an EF Core infrastructure concern and no longer leaks into domain query descriptors.
  - *Migration:* Apply `.AsSplitQuery()` on your `IQueryable<T>` in the repository data access implementation.

- **BC-003 (Memory & Collision Safety):** Replaced integer hash caching in `ExpressionCompilationCache` with deep structural AST equality (`ExpressionEqualityComparer.Default`) ([ADR-019](docs/adr/adr-019-expression-compilation-cache-key-strategy.md)).
  - *Impact:* Guarantees mathematical collision immunity for dynamically compiled delegates.
  - *Migration:* No code changes required. Consumers can optionally configure `ExpressionCompilationCache.Capacity` (default: 512).

- **BC-004 (DoS & Resource Protection):** Converted `QueryPlanCache` from an unbounded dictionary to a thread-safe, bounded LRU cache with an $O(1)$ eviction policy ([ADR-021](docs/adr/ADR-021-querypancache-lru-bounded.md)).
  - *Impact:* Prevents memory exhaustion / OutOfMemoryException in long-running processes with dynamic query shapes.
  - *Migration:* Configure `QueryPlanCache.Capacity` at application startup if retaining more than 512 concurrent query plans is necessary.

- **BC-005 (Package Decomposition):** Extracted `MsSqlDialect` from `EricksonLopez.Specification.Sql` into the dedicated NuGet package `EricksonLopez.Specification.MsSql` ([ADR-028](docs/adr/adr-028-sql-infrastructure-layer-and-dialect-package-decomposition.md)).
  - *Impact:* Modularizes database engine support to minimize consumer binary footprint.
  - *Migration:* Reference `<PackageReference Include="EricksonLopez.Specification.MsSql" Version="1.0.0" />` and import `using EricksonLopez.Specification.MsSql;`.

- **BC-006 (API Deprecation):** Deprecated `Spec.InRange` in favor of `Spec.Between` ([ADR-022](docs/adr/adr-022-spec-all-any-combinators.md)).
  - *Impact:* Emits compiler warning `CS0618`.
  - *Migration:* Replace calls to `Spec.InRange(prop, min, max)` with `Spec.Between(prop, min, max)`.

- **BC-007 (Package Segregation):** Segregated functional `Result<T>` repository execution extensions into the standalone package `EricksonLopez.Specification.Result`.
  - *Impact:* Core abstractions maintain zero dependencies on functional result envelopes.
  - *Migration:* Install `EricksonLopez.Specification.Result` and import `using EricksonLopez.Specification.Result;` when utilizing `ListResultAsync` or `FirstOrDefaultResultAsync`.

- **BC-008 (Compile-Time Quality Gates):** Enabled Roslyn Analyzer rules `SPEC001` through `SPEC011`.
  - *Impact:* Unsealed specifications, mutable state, `Expression.Invoke`, and async lambdas in expressions generate build errors under `TreatWarningsAsErrors=true`.
  - *Migration:* Mark all specification classes as `sealed`, eliminate state mutations, and ensure expressions are pure and synchronous.

- **BC-009 (Native AOT Invariants):** Added `[DynamicallyAccessedMembers]` constraints to generic type parameters on `Specification<T>` and `Spec` factories ([ADR-009](docs/adr/adr-009-aot-first-design.md)).
  - *Impact:* Ensures linker metadata preservation for in-memory evaluation without dynamic IL generation.
  - *Migration:* Annotate custom generic specification factory methods with `[DynamicallyAccessedMembers]`.

- **BC-010 (DDD Paradigm Shift):** Enforced complete separation between Domain Predicates (`Specification<T>`) and Application Query Descriptors (`QuerySpec<T>`) ([ADR-006](docs/adr/adr-006-specification-queryspec-separation.md)).
  - *Impact:* `Specification<T>` represents purely synchronous business predicates (`Candidate -> bool`). Presentation and persistence concerns (`Include`, `OrderBy`, `Page`, `Select`) must be defined on `QuerySpec<T>`.
  - *Migration:* Refer to the comprehensive migration recipes in [`MIGRATION.md`](MIGRATION.md).

---

### ✨ Features

- **Domain Specifications (`Specification<T>`):** Pure, thread-safe, Native AOT-compatible domain predicates with in-memory interpretation (`IsSatisfiedBy`) and invoke-free AST composition (`And`, `Or`, `Not`).
- **Application Query Descriptors (`QuerySpec<T>`):** Immutable query definitions supporting filters (`Where`), ordering (`OrderBy`, `ThenBy`), offset pagination (`Page`, `Skip`, `Take`), keyset/cursor pagination (`SeekAfter`, `SeekBefore`, `WithCursor`), projections (`Select`), text search (`Search`), and diagnostic tagging (`TagWith`).
- **SQL AST Translation Engine (`EricksonLopez.Specification.Sql`):** Provider-agnostic AST translator with parameterized query rendering and bounded LRU caching.
- **SQL Dialects:** Dedicated dialect packages for `PostgreSql`, `MsSql`, `Sqlite`, `MySql`, `MariaDb`, and `Oracle`.
- **LINQ & ORM Integrations:** Zero-allocation query composition over `IQueryable<T>` (`EricksonLopez.Specification.Linq`), Entity Framework Core (`EricksonLopez.Specification.EntityFrameworkCore`), Dapper (`EricksonLopez.Specification.Dapper`), DapperExtensions (`EricksonLopez.Specification.DapperExtensions`), and MongoDB (`EricksonLopez.Specification.MongoDB`).
- **Roslyn Analyzers & Code Fixes:** 11 custom build-time analyzers (`SPEC001`–`SPEC011`) with automated code fixes for legacy Ardalis migration and sealed specification enforcement.
- **Compile-Time Source Generators (`EricksonLopez.Specification.Generators`):** Zero-reflection column name resolvers (`[SpecColumnResolver]`) and strongly-typed ordering helpers (`[Spec]`).

---

[1.0.0]: https://github.com/ericksonlopezf/dotnet-specification/releases/tag/v1.0.0
