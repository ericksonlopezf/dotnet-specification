# ARCHITECTURE DECISION RECORDS (ADR) INDEX — EricksonLopez.Specification

> **Total Records**: 28 ADRs (adr-001 through adr-028)  
> **Status**: All Accepted  
> **Location**: `docs/adr/`

---

## 1. Architectural & Design Decisions (What We Built & How)

| ADR | Title | Decision Summary | Primary Drivers |
|---|---|---|---|
| [**adr-006**](docs/adr/adr-006-specification-queryspec-separation.md) | Specification/QuerySpec Separation | Strict separation between domain predicates (`Specification<T>`) and query descriptors (`QuerySpec<T>`). | DDD Purity, Clean Architecture, Testability |
| [**adr-008**](docs/adr/adr-008-expression-trees-as-internal-representation.md) | Expression Trees as Internal Representation | Use `Expression<Func<T, bool>>` over opaque `Func<T, bool>` delegates for composability and SQL translatability. | Composability, SQL Translation, BCL Purity |
| [**adr-009**](docs/adr/adr-009-aot-first-design.md) | AOT-First Design | In-memory evaluation via `ExpressionInterpreter`; JIT paths annotated with `[RequiresDynamicCode]`. | Native AOT, Performance, Predictability |
| [**adr-010**](docs/adr/adr-010-no-efcore-in-core.md) | No EF Core in Core Package | Core has zero external dependencies; EF Core supported via LINQ `IQueryable.Apply()` adapter. | Persistence Ignorance, Zero-Dependency Core |
| [**adr-012**](docs/adr/adr-012-projection-boundary.md) | Projection Boundary | Projections handled via separate `QuerySpec<T, TResult>` with strongly-typed `Select` expression. | Type Safety, Immutability |
| [**adr-018**](docs/adr/adr-018-remove-asnotracking-splitquery-from-queryspec.md) | Remove AsNoTracking/SplitQuery from QuerySpec | Removed ORM-specific tracking flags from Application contracts. | Clean Architecture, Provider Independence |
| [**adr-019**](docs/adr/adr-019-expression-compilation-cache-key-strategy.md) | Compilation Cache Key Strategy | Use `ExpressionEqualityComparer` (deep structural equality) instead of expression hash codes. | Correctness, Collision Prevention |
| [**adr-020**](docs/adr/adr-020-source-generator-strategy.md) | Source Generator Strategy | Exclude stub generator from v1.0 NuGet release; redesign for v2.0 for AOT column resolvers. | Honesty, Quality, Release Hygiene |
| [**adr-021**](docs/adr/adr-021-querypancache-lru-bounded.md) | QueryPlanCache Bounded LRU Strategy | QueryPlanCache must be bounded to 512 entries with LRU eviction to prevent memory leaks. | Memory Safety, High Throughput |
| [**adr-022**](docs/adr/adr-022-spec-all-any-combinators.md) | Spec.All / Spec.Any Static Combinators | Implement `Spec.All<T>()` and `Spec.Any<T>()` factory methods delegating to span-based composition. | Ergonomics, DX, Low Allocation |
| [**adr-023**](docs/adr/adr-023-ispecification-in-abstractions.md) | ISpecification Placement in Abstractions vs Core | Place `ISpecification<T>` and `QuerySpec<T>` in Abstractions package for zero-dependency domain models. | Clean Architecture, Micro-packaging, AOT |
| [**adr-024**](docs/adr/adr-024-convertkeyselector-boxing-strategy.md) | ConvertKeySelector Boxing Strategy | Box key selectors via `Expression.Convert(body, typeof(object))` inside `OrderClause<T>` and unwrap in translators. | API Ergonomics, Type Safety, Simplicity |
| [**adr-025**](docs/adr/adr-025-expressionsimplifier-integration.md) | ExpressionSimplifier Auto-Integration | Automatically simplify boolean identity neutrals (`A && true = A`, `A || false = A`) in `CompositeSpecification`. | Correctness, Optimal SQL ASTs |
| [**adr-026**](docs/adr/adr-026-osherove-test-naming-convention.md) | Osherove Test Naming Convention & IDE1006/CA1707 Suppression | Adopt `Method_Scenario_Result` pattern; suppress IDE1006/CA1707 for living executable CI specs. | Observability, Living Specs, Dev Ergonomics |
| [**adr-027**](docs/adr/adr-027-mariadb-and-mysql-dialect-strategy.md) | MariaDB and MySQL Native Dialect Strategy | Native backtick quoting, parameter prefix, collection expansion, pagination, and dedicated engine differentiation. | Engine Parity, SQL Safety, Telemetry |
| [**adr-028**](docs/adr/adr-028-sql-infrastructure-layer-and-dialect-package-decomposition.md) | SQL Infrastructure Layer Isolation & Dialect Decomposition | Isolate agnostic AST/engine in Specification.Sql; decompose dialects into dedicated satellite packages (MsSql, PostgreSql, MySql, Sqlite, Oracle). | Persistence Ignorance, Clean Architecture, Symmetry |

---

## 2. Rejected Feature Decisions (What We Will NOT Build)

| ADR | Title | Rejected Feature | Rationale for Rejection |
|---|---|---|---|
| [**adr-001**](docs/adr/adr-001-no-write-repository.md) | No Write Repository | `IWriteRepository<T>` / `Add/Update/Delete` | Specification is a selection concept, not a state-mutation framework. |
| [**adr-002**](docs/adr/adr-002-no-include-theninclude.md) | No Include/ThenInclude in Core | `Include()` / `ThenInclude()` | Navigation loading is an ORM implementation detail, not a domain specification. |
| [**adr-003**](docs/adr/adr-003-no-dynamic-string-ordering.md) | No Dynamic String Ordering | `OrderBy("PropertyName")` | String-based property access introduces reflection overhead, SQL injection risks, and breaks AOT. |
| [**adr-004**](docs/adr/adr-004-no-sat-simplification.md) | No SAT-Based Simplification | Full SAT solver for boolean trees | NP-complete complexity, high CPU overhead, and potential semantic alteration risks. |
| [**adr-005**](docs/adr/adr-005-no-xor-composition.md) | No XOR/NAND/NOR Composition | `spec.Xor(other)` | SQL dialects lack native XOR support; rare domain use cases do not justify complexity. |
| [**adr-007**](docs/adr/adr-007-no-fluentvalidation-integration.md) | No FluentValidation Integration | Tight coupling with FluentValidation | Validation produces error message collections; Specifications evaluate business truth. |
| [**adr-011**](docs/adr/adr-011-no-dynamic-string-queries.md) | No Dynamic String-Based Queries | Dynamic LINQ string parsing | Destroys type safety, prevents compile-time refactoring, and breaks Native AOT. |
| [**adr-013**](docs/adr/adr-013-no-raw-sql.md) | No Raw SQL / `WhereRaw()` | Raw SQL string injection in specs | Bypasses dialect translation, creates SQL injection vulnerabilities, and breaks provider independence. |
| [**adr-014**](docs/adr/adr-014-no-dynamic-reflection-queries.md) | No Dynamic Reflection Queries | Reflection-driven property filters | Heavy performance degradation, breaks trimming, and violates compile-time safety. |
| [**adr-015**](docs/adr/adr-015-no-groupby-aggregation-selectmany.md) | No GroupBy / Aggregation / SelectMany | `GroupBy`, `Sum`, `SelectMany` in specs | Aggregation is an analytical query concern, not a domain filtering specification. |
| [**adr-016**](docs/adr/adr-016-no-auto-generated-buildexpression.md) | No Auto-generated `BuildExpression` | Spec-from-attributes generator | Overengineering; manual `BuildExpression` is explicit, readable, and refactor-friendly. |
| [**adr-017**](docs/adr/adr-017-no-async-specifications.md) | No Async Specifications | `Task<bool> IsSatisfiedByAsync()` | Specifications express conditions over data; they must not become I/O execution pipelines. |
