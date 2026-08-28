# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

---

## [1.0.0] - 2026-08-28

### 🚀 Added

- **Initial Release** of the `EricksonLopez.Specification` ecosystem:
  - **Domain Specifications (`EricksonLopez.Specification` / `EricksonLopez.Specification.Abstractions`):**
    - Pure, immutable, and thread-safe domain predicates via `Specification<T>` and `ISpecification<T>`.
    - 100% Native AOT-safe in-memory evaluation engine (`ExpressionInterpreter`) without runtime IL generation.
    - Parameter-rebinding AST composition (`And`, `Or`, `Not`, `AndAll`, `OrAny`) without `Expression.Invoke`.
    - Fluent specification factory (`Spec.For<T>`, `Spec.True<T>`, `Spec.False<T>`, `Spec.All`, `Spec.Any`).
    - Opt-in AST structural equality cache (`ExpressionCompilationCache`) with `ExpressionEqualityComparer.Default`.
  - **Application Query Descriptors (`QuerySpec<T>` / `QuerySpec<T, TResult>`):**
    - Immutable value descriptors encapsulating filtering (`Where`), projection (`Select`), ordering (`OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`), and diagnostic query tagging (`TagWith`).
    - First-class pagination support for both standard offset (`Page`, `Skip`, `Take`) and keyset/cursor seek pagination (`SeekAfter`, `SeekBefore`, `WithCursor`).
  - **SQL AST Translation Engine (`EricksonLopez.Specification.Sql`):**
    - Provider-agnostic LINQ expression-to-SQL AST translator (`QuerySpecTranslator<T>`) rendering parameterized SQL query models (`QueryModel`).
    - Bounded LRU query plan caching (`QueryPlanCache`) and structural expression hashing (`ExpressionHasher`).
    - Extensible column mapping via `IColumnNameResolver` (with built-in PascalCase, SnakeCase, and custom resolvers).
  - **SQL Database Dialects:**
    - Dedicated dialect packages for **PostgreSQL** (`EricksonLopez.Specification.PostgreSql`), **Microsoft SQL Server** (`EricksonLopez.Specification.MsSql`), **MySQL** (`EricksonLopez.Specification.MySql`), **MariaDB** (`EricksonLopez.Specification.MariaDb`), **SQLite** (`EricksonLopez.Specification.Sqlite`), and **Oracle Database** (`EricksonLopez.Specification.Oracle`).
  - **ORM & Data Access Integrations:**
    - **Entity Framework Core & LINQ** (`EricksonLopez.Specification.EntityFrameworkCore`, `EricksonLopez.Specification.Linq`): `IQueryable<T>.Apply(querySpec)` extension, generic `IReadRepository<T>` implementation, and Dependency Injection extensions.
    - **Dapper & Micro-ORMs** (`EricksonLopez.Specification.Dapper`, `EricksonLopez.Specification.DapperExtensions`): Parameterized SQL execution extensions (`QueryAsync`, `CountAsync`, `AnyAsync`) over `IDbConnection`.
    - **MongoDB** (`EricksonLopez.Specification.MongoDB`): Direct specification translation to native MongoDB `FilterDefinition<T>` and `SortDefinition<T>`.
    - **Railway-Oriented Result Extensions** (`EricksonLopez.Specification.Result`): Functional query extensions (`ListResultAsync`, `FirstOrDefaultResultAsync`) integrated with `EricksonLopez.Result`.
  - **Compile-Time Roslyn Analyzers & Code Fixes (`EricksonLopez.Specification.Analyzers`):**
    - 11 static diagnostic analyzers (`SPEC001` through `SPEC011`) enforcing sealed specifications, immutability, pure domain expressions, non-translatable method checks, and migration from legacy patterns.
    - Automated CodeFix providers for `SPEC001` (`sealed` modifier) and `SPEC011` (Ardalis migration).
  - **Incremental Source Generators (`EricksonLopez.Specification.Generators`):**
    - Compile-time zero-reflection column name resolvers (`[SpecColumnResolver]`) and strongly-typed specification ordering helpers.
  - **Enterprise Observability & Quality:**
    - Native OpenTelemetry instrumentation via dedicated `ActivitySource` and `Meter` instruments.
    - Comprehensive test suite with $\ge 99\%$ Stryker.NET mutation testing score and full Native AOT trimming verification.

---

[1.0.0]: https://github.com/ericksonlopezf/dotnet-specification/releases/tag/v1.0.0
