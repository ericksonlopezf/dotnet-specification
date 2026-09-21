# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2026-09-21

### ⚠️ Breaking Changes

- **BC-001: Relocation of `IExpressionSpecification<T>` to `EricksonLopez.Specification.Abstractions` (Binary Compatibility Preserved)**
  - **Affected API**: `EricksonLopez.Specification.IExpressionSpecification<T>`
  - **Previous State**: Resided in assembly `EricksonLopez.Specification.dll` (package `EricksonLopez.Specification`).
  - **Current State**: Relocated to assembly `EricksonLopez.Specification.Abstractions.dll` (package `EricksonLopez.Specification.Abstractions`) with runtime type forwarding (`[TypeForwardedTo]`) in `EricksonLopez.Specification`.
  - **Affected Consumers**: Fine-grained consumers referencing only Abstractions benefit from isolated contracts. Existing compiled binaries maintain full binary compatibility via type forwarders.
  - **Impact**: Zero runtime `TypeLoadException` due to `[assembly: TypeForwardedTo(...)]` in `EricksonLopez.Specification`. New code should reference `EricksonLopez.Specification.Abstractions`.
  - **Migration**: Existing binaries run seamlessly without modification. For source builds using fine-grained packages, add a reference to `EricksonLopez.Specification.Abstractions`.

- **BC-002: Removal of Transitive Dependency on `EricksonLopez.Specification` in `EricksonLopez.Specification.Linq`**
  - **Affected Package**: `EricksonLopez.Specification.Linq`
  - **Previous State**: Referenced `EricksonLopez.Specification`, transitively exposing `Specification<T>`, `Spec`, and core domain specification types.
  - **Current State**: References `EricksonLopez.Specification.Abstractions` directly; project and package dependency on `EricksonLopez.Specification` has been eliminated.
  - **Affected Consumers**: Projects referencing only `EricksonLopez.Specification.Linq` that implicitly consumed core specification classes (`Specification<T>`, `Spec.For<T>`).
  - **Impact**: Compile-time errors (`CS0246: The type or namespace name 'Specification<>' could not be found`).
  - **Migration**: Explicitly install the `EricksonLopez.Specification` NuGet package in consumer projects that construct specifications.

- **BC-003: Construction-Time Range Validation in `Spec.Between`**
  - **Affected API**: `Spec.Between<T, TProperty>(Expression<Func<T, TProperty>>, TProperty lower, TProperty upper)`
  - **Previous State**: Allowed `lower.CompareTo(upper) > 0` without throwing at construction, building an expression tree `(x >= lower && x <= upper)` that evaluated to `false` at query/evaluation time.
  - **Current State**: Validates `lower` and `upper` during factory invocation, throwing `ArgumentException` if `lower.CompareTo(upper) > 0`.
  - **Affected Consumers**: Callers passing dynamic or inverted range bounds to `Spec.Between`.
  - **Impact**: Throws `ArgumentException: Lower bound '{lower}' cannot be greater than upper bound '{upper}'.` synchronously upon calling `Spec.Between`.
  - **Migration**: Ensure `lower <= upper` before invoking `Spec.Between`, or use conditional branching/swapping when dealing with dynamic user input.

- **BC-004: Cancellation Exception Re-throw in `ReadRepositoryResultExtensions`**
  - **Affected API**: `ReadRepositoryResultExtensions` in `EricksonLopez.Specification.Result` (`FirstOrDefaultResultAsync`, `SingleOrDefaultResultAsync`, `ListResultAsync`, `GetByIdResultAsync`)
  - **Previous State**: Caught all exceptions including `OperationCanceledException` and returned `Result.Failure(Error.Failure("Database.Error", ex.Message))`.
  - **Current State**: Explicitly catches and rethrows `OperationCanceledException` to preserve cooperative cancellation semantics.
  - **Affected Consumers**: Callers awaiting `*ResultAsync` with cancellation tokens and expecting `Result.IsFailure` without handling task cancellation.
  - **Impact**: Throws unhandled `OperationCanceledException` from the returned `Task<Result<T>>` instead of returning a failed `Result`.
  - **Migration**: Catch `OperationCanceledException` at caller level or handle task cancellation according to standard .NET TAP asynchronous patterns.

- **BC-005: Security Namespace Restrictions in In-Memory `ExpressionInterpreter`**
  - **Affected API**: `ExpressionInterpreter` (in-memory `ISpecification<T>.IsSatisfiedBy` and interpreted LINQ)
  - **Previous State**: Invoked any method call in expressions via reflection without security sandboxing.
  - **Current State**: Blocks execution of methods whose declaring types belong to `System.Diagnostics`, `System.IO`, `System.Reflection`, or `System.Environment`.
  - **Affected Consumers**: Specifications with expressions calling diagnostic tracing (`Trace.WriteLine`), I/O path manipulation (`Path.Combine`), reflection, or environment inspection.
  - **Impact**: Throws `InvalidOperationException: Method '{method.Name}' on type '{declaringType.FullName}' is not permitted in interpreted specification evaluation for security reasons.`
  - **Migration**: Extract external I/O, diagnostic, or environment state evaluation outside of specification expression trees before constructing the specification predicate.

- **BC-006: AST Traversal Depth Limit (DoS Guard) in `ExpressionEqualityComparer` and `ExpressionHasher`**
  - **Affected API**: `ExpressionEqualityComparer.Equals` and `ExpressionHasher.Hash`
  - **Previous State**: Traversed unbounded expression tree depths until process `StackOverflowException`.
  - **Current State**: Enforces a strict maximum recursion depth of 512 nodes (`MaxDepth = 512`).
  - **Affected Consumers**: Highly complex or dynamically generated expression trees with nesting depths exceeding 512.
  - **Impact**: Throws `InvalidOperationException: Expression tree exceeds maximum supported equality/hashing depth of 512.`
  - **Migration**: Simplify or rebalance deep AST expressions into flattened or partitioned specifications to remain within the 512 depth limit.

- **BC-007: Keyset Pagination Type Guard in `QuerySpecLinqExtensions`**
  - **Affected API**: `QuerySpecLinqExtensions.Apply` keyset pagination (`Cursor`)
  - **Previous State**: Permitted any property type selector for cursor pagination, deferring validation to the underlying provider.
  - **Current State**: Validates that cursor property expressions implement `IComparable` (or nullable underlying), throwing `NotSupportedException` otherwise.
  - **Affected Consumers**: Keyset pagination queries targeting non-comparable custom scalar properties.
  - **Impact**: Throws `NotSupportedException: Keyset cursor pagination on type '{type}' is not supported. Cursor key selector must target a comparable scalar property.`
  - **Migration**: Ensure entities used in keyset cursor pagination select properties implementing `IComparable` (e.g. `int`, `long`, `Guid`, `DateTime`, `string`).

- **BC-008: AOT / Trimming Generic Annotations `[DynamicallyAccessedMembers]` on `Any<T>` and `Count<T>`**
  - **Affected API**: `QuerySpecLinqExtensions.Any<T>` and `QuerySpecLinqExtensions.Count<T>`
  - **Previous State**: Generic type parameter `T` had no trimming annotations.
  - **Current State**: `T` is annotated with `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]`.
  - **Affected Consumers**: NativeAOT or trim-enabled applications calling `Any<T>` or `Count<T>`.
  - **Impact**: Trimming analyzer warnings (`IL2091`) emitted at call sites where `T` lacks property/field annotations; breaks compilation when `TreatWarningsAsErrors=true`.
  - **Migration**: Add `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]` to calling generic classes/methods or annotate consumer DTO models.

- **BC-009: Query Filter Chaining Structure in `QuerySpecLinqExtensions.Apply`**
  - **Affected API**: `QuerySpecLinqExtensions.Apply<T>(this IQueryable<T>, QuerySpec<T>)`
  - **Previous State**: Evaluated multiple criteria by combining them into a single `ExpressionComposer.AndAll` lambda passed to a single `.Where()` call.
  - **Current State**: Applies criteria as multiple chained `.Where(criterion)` calls directly on the `IQueryable<T>`. Removed `BuildCombinedPredicate()`.
  - **Affected Consumers**: Custom LINQ providers, query interceptors, mock queryables, or test assertions inspecting the AST `MethodCallExpression` hierarchy.
  - **Impact**: Observable expression tree changes from `Where(source, combinedAndLambda)` to sequential `Where(Where(source, crit1), crit2)`.
  - **Migration**: Update any expression interceptors or test assertions expecting a single combined `AndAlso` lambda to support chained `Where` invocations.

- **BC-010: Query Filter Application in `MongoSpecificationEvaluator.ApplySpecification`**
  - **Affected API**: `MongoSpecificationEvaluator.ApplySpecification<TDocument>(this IFindFluent<TDocument, TDocument>, QuerySpec<TDocument>)`
  - **Previous State**: Ignored `specification.Criteria` completely, applying only `Sort`, `Skip`, and `Limit`.
  - **Current State**: Builds `FilterDefinition<TDocument>` from `specification.Criteria` and sets `findFluent.Filter` (combining via `And` if filter already exists).
  - **Affected Consumers**: Consumers relying on `ApplySpecification` in MongoDB data access layers.
  - **Impact**: Queries that previously returned unconstrained documents now return only documents satisfying specification criteria. If external code already added custom filters, they will be combined via logical `AND`.
  - **Migration**: Review MongoDB queries using `ApplySpecification` to ensure specification criteria match expected filter criteria. Remove manual redundant filter definitions if previously applied as workarounds.

- **BC-011: AST Evaluation Depth Limit (DoS Guard) in In-Memory `ExpressionInterpreter`**
  - **Affected API**: `ExpressionInterpreter.Evaluate` (and in-memory evaluation via `ISpecification<T>.IsSatisfiedBy`)
  - **Previous State**: Evaluated arbitrary expression tree depths recursively until process `StackOverflowException`.
  - **Current State**: Enforces a strict maximum recursion depth of 512 nodes (`MaxDepth = 512`), throwing `InvalidOperationException`.
  - **Affected Consumers**: In-memory domain specification evaluations with deeply nested expression ASTs exceeding 512 nodes.
  - **Impact**: Throws `InvalidOperationException: Expression tree exceeds maximum supported evaluation depth of 512.` synchronously during evaluation.
  - **Migration**: Restructure or flatten deeply nested composite specifications, or utilize pre-compiled delegates (`ToCompiledPredicate()`) in JIT environments if deep AST recursion is required.

### Added

- `ExpressionDebugFormatterRegistry` in `EricksonLopez.Specification.Abstractions`: Global thread-safe registry providing pluggable expression AST debug formatting across NativeAOT and JIT environments.
- `ReadRepositoryResultExtensions` in `EricksonLopez.Specification.Result`: Functional result query methods (`ListResultAsync`, `FirstOrDefaultResultAsync`, `SingleOrDefaultResultAsync`, `GetByIdResultAsync`) over `IReadRepository<T>` returning `Result<T>`. Included in the primary build solution for official NuGet distribution.
- C# logical operators on `Specification<T>`: overloaded `&`, `|`, `!`, `true`, `false`, `BitwiseAnd`, `BitwiseOr`, and `LogicalNot`.
- Multi-targeting expansion across all library projects: `.NET 8.0` and `.NET 9.0` support alongside `.NET 10.0`.
- New `Spec.Between` overload supporting nullable property selectors (`Expression<Func<T, TProperty?>>`).
- Collection-based `Spec.All` and `Spec.Any` overloads accepting `IEnumerable<Specification<T>>`.
- LINQ extension overloads in `QuerySpecLinqExtensions`: `Where`, `All`, `FirstOrDefault` over `IQueryable<T>` with `IExpressionSpecification<T>`, and `Where`, `Any`, `All`, `Count`, `FirstOrDefault` over `IEnumerable<T>` with `ISpecification<T>`.
- Fluent `collection.Find(specification)` extension method in `MongoSpecificationEvaluator`.
- `stryker-result-config.json` for dedicated mutation testing of `EricksonLopez.Specification.Result`.
- Comprehensive test suites: `AdversarialRegressionTests`, `ConcurrencyAuditTests`, `FuzzingEngineTests`, `ReadRepositoryResultExtensionsTests`, and `SpecificationLinqExtensionsTests`.

### Changed

- Hardened in-memory `ExpressionInterpreter` and `ExpressionEqualityComparer` node traversal for adversarial expression patterns.
- Parameter re-binding and caching fix in `QuerySpecTranslator`: cached query plans now correctly reparameterize current query arguments rather than retaining stale parameter values.


---

## [1.0.0] - 2026-08-28

### Added

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

[Unreleased]: https://github.com/ericksonlopezf/dotnet-specification/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/ericksonlopezf/dotnet-specification/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/ericksonlopezf/dotnet-specification/releases/tag/v1.0.0
