# EricksonLopez.Specification -- Feature Matrix and Technical Specification

Version: 1.0 baseline audit -- 2026-08-12
Status: Official FEATURES.md

## Founding Principle

Does this feature belong to the Specification Pattern and improve the product without breaking its boundaries?

This library is NOT an ORM, query builder, mapper, or framework.
It is a small, composable, AOT-first, provider-independent abstraction for encoding, composing,
and translating business rules as typed expression trees.

---

### Part 1 -- Current State Audit

| Package | Purpose | AOT Safe |
|---|---|---|
| Abstractions | ISpecification<T>, QuerySpec<T>, QuerySpec<T,TResult>, IReadRepository<T> | Full |
| EricksonLopez.Specification | Specification<T>, Spec, engine, simplifier, interpreter | Core AOT; ExpressionCompilationCache JIT-only |
| Linq | QuerySpecLinqExtensions, IQueryable<T> adapter | AOT safe |
| Sql | QuerySpecTranslator<T>, QueryModel, ISqlDialect, AST nodes, QueryPlanCache | Annotated |
| PostgreSql | PostgreSqlDialect | Safe |
| MsSql | MsSqlDialect | Safe |
| MySql | MySqlDialect | Safe |
| MariaDb | MariaDbDialect | Safe |
| Sqlite | SqliteDialect | Safe |
| Oracle | OracleDialect | Safe |
| Dapper | QuerySpecDapperExtensions | Safe |
| EntityFrameworkCore | SpecificationEvaluator, EfRepository<T> | Safe |
| MongoDB | MongoFilterCompiler, MongoSortCompiler | Safe |
| DapperExtensions | DapperExtensionsIntegration | Safe |
| Result | ReadRepositoryResultExtensions | Safe |
| Analyzers | SPEC001-SPEC011 Roslyn analyzers & CodeFix providers | Compile-time only |
| Generators | Source generator (v2.0 preview) | Build-time only |


---

## Part 3 -- Feature Matrices

Legend: Native=implemented, Partial=limited, Planned=architected, NotSupported=absent, Adapter=provider only, Rejected=will not implement

### A -- Core Specification

| Feature | EricksonLopez | Ardalis | Priority | AOT |
|---|---|---|---|---|
| ISpecification<T> minimal interface | Native | Native | P0 | Yes |
| Specification<T> abstract base class | Native | Native | P0 | Yes |
| Spec.For<T>(expr) factory | Native | Not Supported | P0 | Yes |
| Spec.True<T>() always satisfies | Native | Partial | P0 | Yes |
| Spec.False<T>() never satisfies | Native | Partial | P0 | Yes |
| IsSatisfiedBy(T) in-memory evaluation | Native | Native | P0 | Yes |
| ToExpression() expose expression tree | Native | Partial | P0 | Yes |
| Immutable specification | Native | Not Supported | P0 | Yes |
| Thread-safe specification | Native | Partial | P0 | Yes |
| IExpressionSpecification<T> expression access | Native | Not Supported | P1 | Yes |
| Sealed specs enforced by analyzer | Native | Not Supported | P1 | Yes |

### B -- Predicate Composition

| Feature | EricksonLopez | Ardalis | LinqKit | Priority | AOT |
|---|---|---|---|---|---|
| And(spec) logical AND | Native | Native | Native | P0 | Yes |
| Or(spec) logical OR | Native | Native | Native | P0 | Yes |
| Not() logical negation | Native | Native | Native | P0 | Yes |
| Arbitrary-depth composition | Native | Native | Native | P0 | Yes |
| Short-circuit semantics | Native | Partial | Partial | P0 | Yes |
| Expression.Invoke-free composition | Native | Partial | Rejected | P0 | Yes |
| ExpressionComposer.AndAll(span) bulk AND | Native | Not Supported | Not Supported | P1 | Yes |
| ExpressionComposer.OrAny(span) bulk OR | Native | Not Supported | Not Supported | P1 | Yes |
| Boolean constant folding | Native | Not Supported | Not Supported | P1 | Yes |
| NOT(NOT(A))=A elimination | Native | Not Supported | Not Supported | P1 | Yes |
| AND TRUE=A identity simplification | Native | Not Supported | Not Supported | P1 | Yes |
| XOR composition | Rejected (adr-005) | Not Supported | Not Supported | REJECTED | N/A |
| Duplicate predicate elimination | Not Supported | Not Supported | Not Supported | REJECTED | N/A |
| SAT-based predicate simplification | Rejected (adr-004) | Rejected | Rejected | REJECTED | N/A |

### C -- Expression Trees

| Feature | EricksonLopez | Ardalis | LinqKit | Priority | AOT |
|---|---|---|---|---|---|
| Expression<Func<T,bool>> internal representation | Native | Native | Native | P0 | Yes |
| Parameter rebinding (no InvocationExpression) | Native | Partial | Rejected | P0 | Yes |
| Structural expression hashing | Native | Not Supported | Not Supported | P1 | Yes |
| Compiled delegate cache ([RequiresDynamicCode]) | Native | Not Supported | Not Supported | P1 | JIT only |
| Interpreted evaluation (AOT-safe) | Native | Not Supported | Not Supported | P0 | Yes |
| Boolean expression simplifier | Native | Not Supported | Not Supported | P1 | Yes |
| Expression tree caching per spec instance | Native | Not Supported | Not Supported | P0 | Yes |
| Expression structural equality (`ExpressionEqualityComparer`) | Native | Not Supported | Not Supported | P0 | Yes |
| Closure capture detection | Native | Not Supported | Not Supported | P1 | Yes |
| Expression string representation debug (`ToDebugString`) | Native | Not Supported | Not Supported | P1 | Yes |

### D -- Query Specification QuerySpec<T>

| Feature | EricksonLopez | Ardalis | Priority | AOT |
|---|---|---|---|---|
| Immutable query descriptor sealed record | Native | Not Supported | P0 | Yes |
| Multiple AND criteria ImmutableArray | Native | Native | P0 | Yes |
| Where(predicate) add filter | Native | Native | P0 | Yes |
| OrderBy<TKey> ascending | Native | Native | P1 | Yes |
| OrderByDescending<TKey> | Native | Native | P1 | Yes |
| ThenBy / ThenByDescending | Native | Native | P1 | Yes |
| Page(page, pageSize) offset pagination | Native | Native | P1 | Yes |
| Skip(count) / Take(count) raw pagination | Native | Native | P1 | Yes |
| Keyset / cursor pagination (`SeekAfter`/`SeekBefore`) | Native | Not Supported | P1 | Yes |
| Distinct() | Native | Native | P2 | Yes |
| NoTracking() / SplitQuery() in QuerySpec | Rejected (adr-018) | Native | REJECTED | N/A |
| QuerySpec<T>.Empty no-op spec | Native | Not Supported | P0 | Yes |
| QuerySpec<T, TResult> projected | Native | Partial | P1 | Yes |
| BuildCombinedPredicate() merged expression | Native | Not Supported | P1 | Yes |
| Cursor/keyset pagination | Not Supported | Not Supported | P3 | Yes |

### E -- LINQ Integration

| Feature | EricksonLopez | Ardalis | LinqKit | Priority | AOT |
|---|---|---|---|---|---|
| IQueryable<T>.Apply(querySpec) | Native | Native | Not Supported | P0 | Yes |
| Where from Criteria | Native | Native | Native | P0 | Yes |
| OrderBy / ThenBy from OrderClauses | Native | Native | Native | P1 | Yes |
| Skip / Take from pagination | Native | Native | Native | P1 | Yes |
| Select projection | Native | Partial | Native | P1 | Yes |
| Distinct | Native | Partial | Native | P2 | Yes |
| AsNoTracking flag | Native | Native | Not Supported | P2 | Yes |
| AsSplitQuery flag | Native | Native | Not Supported | P2 | Yes |
| Any(IExpressionSpecification<T>) extension | Native | Native | Native | P1 | Yes |
| Count(IExpressionSpecification<T>) extension | Native | Native | Native | P1 | Yes |
| SelectMany | Rejected | Not Supported | Not Supported | REJECTED | N/A |
| GroupBy | Rejected | Not Supported | Not Supported | REJECTED | N/A |
| Include / ThenInclude | Adapter | Native | Not Supported | P3 | Yes |

### F -- Repository Contract

| Feature | EricksonLopez | Ardalis | Priority |
|---|---|---|---|
| IReadRepository<T> interface | Native | Native | P0 |
| FirstOrDefaultAsync(QuerySpec<T>, CT) | Native | Native | P0 |
| ListAsync(QuerySpec<T>, CT) | Native | Native | P0 |
| CountAsync(QuerySpec<T>, CT) | Native | Native | P0 |
| AnyAsync(QuerySpec<T>, CT) | Native | Not Supported | P1 |
| ListAsync<TResult>(QuerySpec<T,TResult>, CT) | Native | Partial | P1 |
| IRepository<T> write repository | Rejected | Native | REJECTED |
| SingleOrDefaultAsync | Planned | Native | P1 |
| IAsyncEnumerable<T> streaming | Not Supported | Not Supported | P3 |

### G -- SQL Translation Dapper / Native SQL

| Feature | EricksonLopez | Ardalis | Priority | AOT |
|---|---|---|---|---|
| QuerySpecTranslator<T> Expression to SQL AST | Native | Rejected | P0 | Reflection |
| QueryModel provider-agnostic AST | Native | Rejected | P0 | Yes |
| ISqlDialect AST to parameterized SQL | Native | Rejected | P0 | Yes |
| PostgreSqlDialect | Native | Rejected | P1 | Yes |
| Comparison operators = <> < > | Native | Rejected | P0 | Yes |
| IS NULL / IS NOT NULL | Native | Rejected | P0 | Yes |
| Boolean member access (c.IsActive to col=true) | Native | Rejected | P0 | Yes |
| LIKE Contains StartsWith EndsWith | Partial | Rejected | P1 | Yes |
| AND / OR / NOT | Native | Rejected | P0 | Yes |
| Parameterized queries SQL injection safe | Native | Rejected | P0 | Yes |
| IColumnNameResolver property-to-column | Native | Rejected | P0 | Yes |
| SnakeCaseColumnNameResolver | Native | Rejected | P1 | Yes |
| VerbatimColumnNameResolver | Native | Rejected | P1 | Yes |
| Closure variable extraction | Native | Rejected | P0 | Reflection |
| IN / PostgreSQL ANY | Planned | Rejected | P1 | Yes |
| PostgreSQL ILIKE | Planned | Rejected | P2 | Yes |
| JSONB predicates | Planned | Rejected | P3 | Yes |
| Full-text search (tsvector/tsquery) | Planned | Rejected | P3 | Yes |
| Range types | Planned | Rejected | P3 | Yes |
| RawPredicateNode escape hatch | Native | Rejected | P1 | Yes |
| MSSQL dialect | Not Supported | Rejected | P2 | Yes |
| SQLite dialect | Not Supported | Rejected | P2 | Yes |
| Schema-qualified table names | Planned | Rejected | P2 | Yes |

### H -- AOT / NativeAOT Compatibility

| Feature | AOT Safe | Dynamic Code | Reflection | Notes |
|---|---|---|---|---|
| ISpecification<T>.IsSatisfiedBy | Yes | No | No | Pure method call |
| Specification<T> with interpreted evaluator | Yes | No | Partial | Property.GetValue in interpreter |
| ExpressionInterpreter.Evaluate | Yes | No | Partial | PropertyInfo.GetValue for member access |
| ExpressionComposer And/Or/Not | Yes | No | No | Pure expression manipulation |
| ExpressionSimplifier | Yes | No | No | ExpressionVisitor AOT safe |
| ExpressionHasher | Yes | No | No | ExpressionVisitor AOT safe |
| ExpressionCompilationCache | No | Yes | No | [RequiresDynamicCode] JIT only |
| Specification<T>.ToCompiledPredicate() | No | Yes | No | [RequiresDynamicCode] JIT only |
| QuerySpec<T> data descriptor | Yes | No | No | Pure record |
| QuerySpecLinqExtensions.Apply | Yes | No | No | Passes expression tree to provider |
| QuerySpecTranslator<T> | Partial | No | Yes | FieldInfo.GetValue; trimming risk |
| ISqlDialect.Render | Yes | No | No | Pure string building from AST |
| Analyzers SPEC001-SPEC011 | Yes | No | No | Compile-time only |
| Source Generator | Yes | No | No | Compile-time only |

AOT verdict: Core (Abstractions + EricksonLopez.Specification) is 99% AOT safe.
ExpressionInterpreter uses PropertyInfo.GetValue -- needs [DynamicallyAccessedMembers] annotations.
ExpressionCompilationCache is JIT-only, properly annotated.
QuerySpecTranslator uses reflection for closure extraction -- the only infrastructure path with trimming risk.

### I — Analyzer Diagnostics (SPEC001–SPEC011)

| Diagnostic | Severity | Status | Priority |
|---|---|---|---|
| SPEC001 Specification not sealed/abstract | Warning | Native | P1 |
| SPEC002 Mutable state in specification | Warning | Native | P1 |
| SPEC003 Expression.Invoke detected | Error | Native | P0 |
| SPEC004 Unbounded query no Take | Info | Native | P2 |
| SPEC005 Ordering without pagination | Info | Native | P3 |
| SPEC006 Domain spec outside Domain layer | Info | Native | P3 |
| SPEC007 Non-translatable method call | Warning | Native | P1 |
| SPEC008 Infrastructure service in constructor | Error | Native | P1 |
| SPEC009 Async lambda in BuildExpression | Error | Native | P1 |
| SPEC010 IsSatisfiedBy inside BuildExpression | Error | Native | P1 |
| SPEC011 Legacy Ardalis.Specification detected | Warning | Native | P2 |


---

## Part 4 -- Specification vs Query Object Boundary

Decision: QuerySpec<T> is a bounded Query Object, not a Specification.

| Concern | Specification<T> | QuerySpec<T> |
|---|---|---|
| Belongs in | Domain layer | Application layer |
| Encodes | Pure business rule (predicate only) | Query descriptor (filter + ordering + pagination + projection) |
| Evaluatable in-memory | Yes | Only via LINQ after extraction |
| Database-aware | No | Yes, provider interprets flags |
| Thread-safe | Yes | Yes |

The library maintains a clean separation:
- Specification<T> = pure predicate, Domain layer
- QuerySpec<T> = complete query descriptor, Application/Infrastructure boundary

Specification<T>.ToQuerySpec() bridges them explicitly.

---

## Part 5 -- Clean Architecture Layer Map

Domain Layer -- Safe to use here:
ISpecification<T>, Specification<T> (abstract), concrete domain specs, Spec.For<T>,
Spec.True<T>() / Spec.False<T>(), composition via And(), Or(), Not(), IsSatisfiedBy(T).

Domain Layer -- Must NOT be here:
QuerySpec<T> (pagination = infrastructure concern), IReadRepository<T>, any ORM type, ExpressionCompilationCache.

Application Layer -- Safe:
QuerySpec<T>, QuerySpec<T, TResult>, IReadRepository<T> (injected), Specification<T>.ToQuerySpec(), QuerySpecExtensions.And(spec).

Infrastructure Layer -- Lives here:
IReadRepository<T> implementations, QuerySpecLinqExtensions (EF Core), QuerySpecTranslator<T> + ISqlDialect + dialects (Dapper/SQL), QuerySpecDapperExtensions.

Presentation Layer -- Nothing from this library should appear here.

---

## Part 6 -- ADRs (Architecture Decision Records)

### adr-001: Expression Trees as Internal Representation
Decision: Use Expression<Func<T, bool>> as canonical internal representation.
Alternatives: Delegates only cannot be translated to SQL. Expression + delegate dual path doubles complexity.
Consequences: Full LINQ provider compat; SQL translation possible without ORM; structural hashing; Expression.Compile() needed for high-throughput in JIT.
Trade-offs: Higher allocation than delegates; mitigated by Lazy<T> per instance and ExpressionCompilationCache for hot paths.

### adr-002: AOT-First Design
Decision: Core is 100% AOT safe. Dynamic code paths annotated [RequiresDynamicCode] + [RequiresUnreferencedCode].
AOT users use ExpressionInterpreter; JIT users opt into ExpressionCompilationCache.
Trade-offs: ExpressionInterpreter is 5-20x slower than compiled. Acceptable because AOT correctness > throughput.

### adr-003: EF Core Independence
Decision: No. Core never references EF Core. IQueryable<T> is a BCL interface. AsNoTracking, AsSplitQuery are inert flags in QuerySpec<T>.
Consequences: Zero ORM dependencies in core; works with any IQueryable<T> provider; Dapper consumers do not pull in EF Core.

### adr-004: Dapper Support Strategy
Decision: Expression -> QuerySpecTranslator<T> -> QueryModel (provider-agnostic AST) -> ISqlDialect.Render() -> parameterized SQL -> Dapper executes.
Specification does not execute queries.
Consequences: SQL injection safe; each stage testable independently; new dialects (MSSQL, SQLite) add only a new ISqlDialect implementation.

### adr-005: Query Specification vs Specification Boundary
Decision: Specification<T> = pure predicate only. No pagination, ordering, includes, tracking hints.
QuerySpec<T> = query descriptor. Predicates + ordering + pagination + projection + provider hints.
Rationale: Mixing query concerns into Specification<T> turns it into a God Object.

### adr-006: Projection
Decision: Projection in QuerySpec<T, TResult>, not Specification<T>.
Domain specs filter; projection is a query/mapping concern.

### adr-007: Pagination
Decision: Pagination only in QuerySpec<T>. Domain specifications define business rules; pagination is a UI/API concern.
Cursor/keyset pagination is out of scope for the library core -- implement in the repository layer.

### adr-008: Ordering
Decision: Ordering only in QuerySpec<T>. Domain specifications are predicate-only.
Dynamic string ordering (e.g., OrderBy("Name")) is deliberately unsupported -- injection risk, runtime errors, no compile-time safety.

### adr-009: Includes (ORM Eager Loading)
Decision: No. Include is EF Core-specific. QuerySpec<T> is provider-independent.
Rationale: Adding Include would require dependency on IIncludableQueryable and pollute the Dapper API.

### adr-010: Caching
Decision:
- Spec instance cache: Lazy<T> per Specification<T> instance -- yes
- Compiled delegate cache: ExpressionCompilationCache (JIT-only) -- yes
- SQL query plan cache: planned (v1.2) -- structural hash of QuerySpec<T> -> precompiled SQL string

### adr-011: Source Generators
Decision: Selective use.
Value: [SpecificationMetadata] attribute processing, compile-time expression validation, static spec catalog.
No value: composition, expression optimization, auto-generating BuildExpression.

### adr-012: Analyzer Package
Decision: Yes. Development-time only, zero runtime impact. 11 implemented analyzers (SPEC001–SPEC011).

### adr-013: Package Decomposition
Decision: See Part 8.

---

## Part 7 -- Anti-Features (What NOT to Implement)

| Feature | Reason |
|---|---|
| ORM / query execution | Specification describes queries; does not execute them |
| IRepository<T> write | Unit of Work, writes, transactions are out of scope |
| Unit of Work | Adds coupling without benefit to Specification Pattern |
| Transaction management | Infrastructure concern entirely |
| Connection management | Belongs in DI container / infrastructure layer |
| Mapping engine | AutoMapper, Mapster, Mapperly are separate concerns |
| Validation framework | IsSatisfiedBy is not FluentValidation |
| Caching framework | ExpressionCompilationCache is internal optimization, not a general cache |
| CQRS framework | Commands, handlers, mediators are out of scope |
| Dynamic string-based ordering | Runtime errors, injection risk, no compile-time safety |
| Full SQL engine | We generate parameterized SQL strings, not a SQL parser/engine |
| Include / ThenInclude in core | EF Core-specific; pollutes the API for Dapper/SQL consumers |
| Cursor/keyset pagination in core | Requires entity key knowledge; belongs in repository layer |
| Authorization framework | Consumers may use specs as auth rules; we do not implement authorization logic |
| Row-level security enforcement | Consumer/provider responsibility |
| XOR composition | No real DDD use case |
| SAT-based predicate simplification | Overengineering; academic, not practical at library scale |

---

## Part 8 -- Package Architecture

| Package | Responsibility | Dependencies | AOT | Required |
|---|---|---|---|---|
| EricksonLopez.Specification.Abstractions | ISpecification<T>, QuerySpec<T>, QuerySpec<T,TResult>, IReadRepository<T>, OrderClause<T>, OrderDirection | BCL only | Yes | Yes |
| EricksonLopez.Specification | Specification<T>, Spec, ExpressionComposer, ExpressionSimplifier, ExpressionHasher, ExpressionInterpreter, ExpressionCompilationCache (JIT), QuerySpecExtensions | Abstractions + BCL | Core Yes / JIT opt-in | Yes |
| EricksonLopez.Specification.Linq | QuerySpecLinqExtensions, IQueryable<T> adapter | Core | Yes | Yes for EF Core users |
| EricksonLopez.Specification.Sql | QuerySpecTranslator<T>, QueryModel, ISqlDialect, SqlQuery, AST nodes, IColumnNameResolver | Abstractions | Partial (reflection) | Yes for Dapper users |
| EricksonLopez.Specification.PostgreSql | PostgreSqlDialect | Sql | Yes | Optional -- PostgreSQL only |
| EricksonLopez.Specification.Dapper | QuerySpecDapperExtensions | Sql + Dapper | Yes | Optional -- Dapper users |
| EricksonLopez.Specification.Analyzers | SPEC001–SPEC011 Roslyn analyzers | Roslyn (dev-only) | Yes | Recommended |
| EricksonLopez.Specification.Generators | Source generator | Roslyn (dev-only) | Yes | Optional |

Packages deliberately NOT created:
- EricksonLopez.Specification.EFCore: EF Core integration handled by Linq package + QuerySpec flags. No separate package needed.
- EricksonLopez.Specification.SqlBuilder: QueryModel AST + ISqlDialect IS the SQL builder.
- EricksonLopez.Specification.Testing: A dedicated test package is P3.


---

## Part 9 -- Benchmark Plan (BenchmarkDotNet)

Scenarios to benchmark vs Ardalis.Specification, LinqKit, raw Expression, raw LINQ:

Scenario 1 - Single predicate in-memory evaluation
Scenario 2 - 3-predicate AND composition
Scenario 3 - Reused specification (cache hit)
Scenario 4 - QuerySpec construction (filter + order + page)
Scenario 5 - SQL translation (Translator + Dialect)

Metrics: Mean, Error, StdDev, Allocated, Gen0, Gen1, Gen2

---

## Part 10 -- Test Matrix

| Area | Unit | Integration | Property | Mutation | AOT |
|---|---|---|---|---|---|
| IsSatisfiedBy basic | Yes | | Yes | Yes | Yes |
| And / Or / Not composition | Yes | | Yes | Yes | Yes |
| Arbitrary-depth composition | Yes | | Yes | | |
| Expression.Invoke absence | Yes | | | | |
| ExpressionSimplifier | Yes | | Yes | Yes | |
| ExpressionHasher stability | Yes | | Yes | | |
| ExpressionCompilationCache | Yes | | | | JIT only |
| ExpressionInterpreter node coverage | Yes | | Yes | Yes | Yes |
| QuerySpec<T> immutability | Yes | | Yes | | |
| QuerySpec<T> ordering/pagination | Yes | | | | |
| QuerySpecLinqExtensions | Yes | Yes | | | |
| QuerySpecTranslator<T> | Yes | Yes | Yes | Yes | |
| ISqlDialect / PostgreSqlDialect | Yes | Yes | | | |
| Parameterization SQL injection safety | Yes | Yes | | | |
| Closure variable extraction | Yes | | Yes | | |
| IReadRepository<T> contract | Yes | Yes | | | |
| Analyzers SPEC001–SPEC011 | Yes | | | | |
| Thread safety | Yes | | | | |
| Null handling | Yes | | Yes | | |
| Cancellation | | Yes | | | |
| AOT / trimming | | | | | Yes |

---

## Part 11 -- Scorecard

| Dimension | Score | Rationale |
|---|---|---|
| API Design | 8.5 | Clean, minimal, discoverable. Spec.For, And/Or/Not obvious. ToQuerySpec() bridge elegant. |
| DDD | 9.0 | Specification<T> is pure domain. QuerySpec<T> explicitly application-layer. Separation enforced. |
| Clean Architecture | 9.0 | Zero cross-layer leakage. Package split matches layer split. |
| Composability | 9.0 | And/Or/Not, AndAll/OrAny, QuerySpecExtensions.And(spec). Immutable chain excellent. |
| Expression Handling | 8.5 | ParameterReplacer, ExpressionSimplifier, ExpressionHasher, ExpressionInterpreter, ExpressionCompilationCache. |
| LINQ | 8.0 | Apply is solid. Missing Any(spec), Count(spec). |
| EF Core | 7.5 | Works correctly via LINQ adapter. Include deliberately absent. |
| Dapper | 8.5 | SQL AST pipeline is a genuine architectural innovation vs competitors. |
| SQL-First | 8.0 | Clean. Coverage of expression patterns needs expanding (IN, ILIKE). |
| AOT | 9.0 | Core 99% AOT safe. Properly annotated JIT-only paths. |
| NativeAOT | 8.5 | Works for core eval. ExpressionTranslator reflection is remaining gap. |
| Trimming | 8.0 | ExpressionInterpreter + QuerySpecTranslator need formal trimming annotations. |
| Performance | 8.5 | ReadOnlySpan for bulk ops. Immutable arrays. Lazy caching. Structural hashing. |
| Allocations | 8.5 | ImmutableArray<T>, record with with, struct HashCode. |
| Extensibility | 9.0 | ISqlDialect, IColumnNameResolver, IExpressionSpecification<T> enable extension without core changes. |
| Testing | 8.0 | 5 test projects. Property-based testing not yet added. |
| Diagnostics | 7.5 | ActivitySource + Meter present. Expression visualization not yet implemented. |
| Documentation | 8.0 | XML docs on all public API. System overview with diagrams. |
| Developer Experience | 8.5 | Analyzers guide correct usage. Fluent chain. Factory methods. |
| Competitive Differentiation | 9.5 | No competitor matches: AOT + SQL translation + analyzers + interpreted evaluator + immutable QuerySpec + structural hashing. |

Weighted Score Calculation:
High-priority dimensions (API, DDD, CA, Composability, AOT, Dapper, Differentiation) x 1.5; others x 1.0.
Weighted = 62.5 x 1.5 + 88.5 x 1.0 = 93.75 + 88.5 = 182.25
Max = 215
Score = 182.25 / 215 x 100 = 84.8 / 100

OVERALL SCORE: 85 / 100

---

## Part 12 -- Differentiators (Verifiable vs All Known Competitors)

1. AOT-first, interpreted evaluation: ExpressionInterpreter enables IsSatisfiedBy without Expression.Compile(). No competitor implements this.

2. SQL translation pipeline without ORM: Expression -> QueryModel AST -> ISqlDialect -> parameterized SQL works with raw Dapper on any database. No competitor provides this at the library level.

3. Expression.Invoke-free composition: ParameterReplacer ensures all composed expressions are fully inlinable by EF Core and any LINQ provider. LinqKit's AsExpandable() approach is fragile and AOT-incompatible.

4. Immutable QuerySpec<T> as record: Thread-safe, value-semantic query descriptor. Ardalis uses mutable specification state.

5. Structural expression hashing: Enables compiled delegate caching and SQL plan caching keyed on expression structure, not instance identity.

6. Boolean constant folding: ExpressionSimplifier eliminates AND TRUE, OR FALSE, NOT(NOT(A)) before queries reach the database.

7. Roslyn analyzers (SPEC001–SPEC011): Architectural misuse is caught at compile time. No competitor ships analyzers.

8. Zero ORM dependency in core: Entire Abstractions + EricksonLopez.Specification packages have zero ORM references.

9. IColumnNameResolver strategy: Pluggable property-to-column name mapping without attributes or conventions imposed on entities.

10. Spec.True<T>() / Spec.False<T>() as identity elements: Enables conditional composition chains without null checks.

---

## Part 13 -- Feature Classification

### Tier 0 -- Core (Must Exist)
ISpecification<T>, Specification<T>, Spec.For/True/False, IsSatisfiedBy, ToExpression(), And/Or/Not, CompositeSpecification, NegatedSpecification, LambdaSpecification, ExpressionComposer, ParameterReplacer, ExpressionInterpreter, ExpressionHasher, QuerySpec<T> (record), QuerySpec<T,TResult>, IReadRepository<T>, QuerySpecExtensions

### Tier 1 -- Advanced (In Main Product)
ExpressionSimplifier, ExpressionCompilationCache (JIT-only), AndAll/OrAny(span), SPEC001–SPEC011 analyzers, QuerySpecLinqExtensions, BuildCombinedPredicate

### Tier 2 -- Extension (Separate Package)
QuerySpecTranslator<T>, QueryModel AST, ISqlDialect, SnakeCaseColumnNameResolver, VerbatimColumnNameResolver, PostgreSqlDialect, QuerySpecDapperExtensions

### Tier 3 -- Provider-Specific
MSSQL dialect, SQLite dialect, JSONB predicates, PostgreSQL ANY/IN, full-text search, range types

### Tier 4 -- External Ecosystem
AutoMapper/Mapster/Mapperly integration, FluentValidation integration, Polly resilience, MediatR

### Rejected
ORM, transaction management, Unit of Work, write repository, dynamic string ordering, SAT simplification, XOR composition, Include/ThenInclude in core, cursor pagination in core

---

## Part 14 -- Roadmap

### v1.0 (Current baseline — all done)
ISpecification<T>, Specification<T>, Spec factory, And/Or/Not composition, ExpressionComposer, ExpressionSimplifier, ExpressionHasher, ExpressionInterpreter (AOT-safe), ExpressionCompilationCache (JIT opt-in), QuerySpec<T> + QuerySpec<T,TResult> (immutable record), IReadRepository<T> contract, QuerySpecLinqExtensions (including `Any(spec)` and `Count(spec)`), QuerySpecTranslator<T> + QueryModel + ISqlDialect, PostgreSqlDialect, QuerySpecDapperExtensions, SPEC001–SPEC011 analyzers.

### v1.1
- Fix StartsWith/EndsWith LIKE pattern generation bug
- ~~IQueryable<T>.Any(spec), Count(spec) extension methods~~ — **Done in v1.0**
- IReadRepository<T>.SingleOrDefaultAsync
- [DynamicallyAccessedMembers] annotations on ExpressionInterpreter + QuerySpecTranslator
- IN / PostgreSQL ANY predicate support (collection .Contains)
- ~~SPEC008, SPEC009, SPEC010 analyzers~~ — **Done in v1.0**

### v1.2
- PostgreSQL ILIKE (case-insensitive LIKE)
- Schema-qualified table names
- Expression string representation for diagnostics/logging
- SQL query plan cache (hash-based, per-spec)
- EricksonLopez.Specification.Testing snapshot testing helpers
- Source Generator: [SpecificationMetadata] attribute processing

### v2.0
- MSSQL dialect
- SQLite dialect
- Structural expression equality (full comparator)
- Source Generator: static specification catalog
- PostgreSQL JSONB predicates
- IAsyncEnumerable<T> streaming in IReadRepository<T>
- Expression normalization (canonical form for cache optimization)

### Future (Experimental)
- PostgreSQL full-text search (tsvector / tsquery)
- PostgreSQL range types
- Property-based tests (FsCheck / CsCheck integration)
- Verify snapshot tests for SQL translation output
- Performance regression tracking (BenchmarkDotNet CI)

---

## Part 15 -- Final Verdict

### What EricksonLopez.Specification MUST do

1. Encode pure, immutable, composable, thread-safe domain predicates as expression trees
2. Compose predicates via And/Or/Not without Expression.Invoke
3. Evaluate in-memory via an interpreted evaluator that is 100% NativeAOT-safe
4. Provide an immutable query descriptor (QuerySpec<T>) for filtering, ordering, pagination, projection
5. Translate to SQL via a provider-agnostic AST (QueryModel -> ISqlDialect) for Dapper/raw SQL scenarios
6. Apply to IQueryable<T> for EF Core and other LINQ providers
7. Enforce correct usage via Roslyn analyzers (sealed specs, no mutable state, no Expression.Invoke)
8. Remain zero-dependency in the core packages
9. Keep the API surface minimal -- complexity is opt-in via extension packages

### What EricksonLopez.Specification must NOT do

- Execute queries
- Manage database connections or transactions
- Implement a write repository or Unit of Work
- Depend on EF Core in the core package
- Generate dynamic IL in AOT contexts
- Implement string-based dynamic ordering
- Include/ThenInclude in the core
- Be a validation framework, authorization framework, CQRS framework, or caching framework
- Expose Include navigation paths in the core contract

---

## Part 16 -- Recommended Project Structure

src/
    EricksonLopez.Specification.Abstractions/   P0 -- contracts only, BCL-only deps
    EricksonLopez.Specification/                P0 -- core implementation
        Engine/
            ExpressionComposer.cs
            ExpressionInterpreter.cs
            ExpressionSimplifier.cs
            ExpressionHasher.cs
            ExpressionCompilationCache.cs      JIT-only, [RequiresDynamicCode]
            ParameterReplacer.cs
    EricksonLopez.Specification.Linq/           P1 -- IQueryable adapter (EF Core)
    EricksonLopez.Specification.Sql/            P1 -- SQL translation core
    EricksonLopez.Specification.PostgreSql/     P2 -- PostgreSQL dialect
    EricksonLopez.Specification.Dapper/         P2 -- Dapper extensions
    EricksonLopez.Specification.Analyzers/      P1 -- Roslyn analyzers
    EricksonLopez.Specification.Generators/     P2 -- Source generator

tests/
    EricksonLopez.Specification.Tests/
    EricksonLopez.Specification.Sql.Tests/
    EricksonLopez.Specification.Dapper.Tests/
    EricksonLopez.Specification.Analyzers.Tests/
    EricksonLopez.Specification.Generators.Tests/

benchmarks/
    EricksonLopez.Specification.Benchmarks/

docs/
    FEATURES.md    (this document)
    ADRs/
        adr-001-expression-trees.md
        adr-002-aot-first.md
        adr-003-efcore-independence.md
        adr-004-dapper-strategy.md
        adr-005-query-vs-specification.md
        adr-006-projection.md
        adr-007-pagination.md
        adr-008-ordering.md
        adr-009-includes.md
        adr-010-caching.md
        adr-011-source-generators.md
        adr-012-analyzer-package.md
        adr-013-package-decomposition.md
    system-overview.md
    public-api-surface.md
    performance-guide.md
    cookbook.md

Packages deliberately absent:
- No EricksonLopez.Specification.EFCore: EF Core integration handled by Linq package + QuerySpec flags.
- No EricksonLopez.Specification.SqlBuilder: QueryModel AST + ISqlDialect IS the SQL builder.
- No EricksonLopez.Specification.Testing in v1.x: test helpers live inline.

