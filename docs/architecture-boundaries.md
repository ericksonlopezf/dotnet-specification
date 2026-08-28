# ARCHITECTURE BOUNDARIES — EricksonLopez.Specification

> **Version**: 1.0 — Post-Audit 2026-08-14  
> **Purpose**: Defines the explicit, non-negotiable architectural boundaries of each package.  
> **Authority**: This document supersedes README claims in case of conflict. ADRs take precedence over this document for individual decisions.

---

## 1. The Boundary Rule

```
EricksonLopez.Specification.Core
        │
        │  depends ONLY on
        │
        └── System.* (BCL)
            └── System.Linq.Expressions
```

No external NuGet package may be a dependency of the Core (`EricksonLopez.Specification`) or Abstractions (`EricksonLopez.Specification.Abstractions`) packages. Ever.

---

## 2. Package Boundary Map

```
┌─────────────────────────────────────────────────────────┐
│                     DOMAIN LAYER                        │
│                                                         │
│  Specification<T>        ← pure predicate               │
│  Spec (factory)                                         │
│  ISpecification<T>                                      │
│  IExpressionSpecification<T>                            │
│                                                         │
│  Package: EricksonLopez.Specification                   │
│  Dependencies: BCL only (System.Linq.Expressions)       │
│  External NuGet: NONE                                   │
└─────────────────────────────────────────────────────────┘
                          │
                          │ depends on
                          ▼
┌─────────────────────────────────────────────────────────┐
│                  CONTRACTS LAYER                        │
│                                                         │
│  ISpecification<T>                                      │
│  IExpressionSpecification<T>                            │
│  QuerySpec<T>              ← query descriptor           │
│  QuerySpec<T, TResult>                                  │
│  IReadRepository<T>                                     │
│                                                         │
│  Package: EricksonLopez.Specification.Abstractions      │
│  Dependencies: BCL only                                 │
│  External NuGet: NONE                                   │
└─────────────────────────────────────────────────────────┘
                          │
                          │ both depend on Abstractions
                          │
          ┌───────────────┼───────────────────────┐
          ▼               ▼                       ▼
┌──────────────┐  ┌──────────────────┐  ┌─────────────────┐
│ APPLICATION  │  │  INFRASTRUCTURE  │  │  BUILD-TIME     │
│   LAYER      │  │  SQL LAYER       │  │  LAYER          │
│              │  │                  │  │                 │
│ QuerySpec<T> │  │ QueryModel AST   │  │ Roslyn Analyzers│
│ (in Abstr.)  │  │ ISqlDialect      │  │ (SPEC001-010)   │
│              │  │ QuerySpecTrans-  │  │                 │
│              │  │ lator<T>         │  │ Source Generator│
│              │  │                  │  │ (v2.0+)         │
│ Package:     │  │ Package:         │  │                 │
│ Abstractions │  │ Sql              │  │ Package:        │
│ Dependencies:│  │ Depends:         │  │ Analyzers       │
│ BCL only     │  │ Abstractions     │  │ Generators      │
└──────────────┘  └──────────────────┘  └─────────────────┘
                          │
                          │ dialect implementations depend on Sql
                          │
          ┌───────────────┼───────────────────────┐
          ▼               ▼                       ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────────┐
│ LINQ ADAPTER │  │ DAPPER       │  │ DIALECT ADAPTERS  │
│              │  │ ADAPTER      │  │                   │
│ Apply()      │  │ QueryAsync() │  │ PostgreSqlDialect │
│ Any(spec)    │  │ CountAsync() │  │ MsSqlDialect      │
│ Count(spec)  │  │ AnyAsync()   │  │ SqliteDialect     │
│              │  │              │  │                   │
│ Package:     │  │ Package:     │  │ Packages:         │
│ Linq         │  │ Dapper       │  │ PostgreSql        │
│ Depends:     │  │ Depends:     │  │ (Sql Server in    │
│ Abstractions │  │ Sql +        │  │ Sql package)      │
│              │  │ Dapper lib   │  │ Sqlite            │
└──────────────┘  └──────────────┘  └──────────────────┘
```

---

## 3. Dependency Rules (Non-Negotiable)

| Rule | Rationale |
|---|---|
| `Abstractions` has ZERO external dependencies | Contracts must be reference-able by any project without transitive dependencies |
| `Core` has ZERO external dependencies | Domain predicates must be portable to any runtime |
| `Linq` depends on `Abstractions` only (not `Core`) | The LINQ adapter only needs the contract, not the engine |
| `Sql` depends on `Abstractions` only | SQL translation operates on `QuerySpec<T>`, not on `Specification<T>` |
| `Dapper` depends on `Sql` and `Dapper` NuGet | The Dapper adapter bridges the SQL layer and Dapper's `IDbConnection` |
| Dialect packages depend on `Sql` only | Dialects render `QueryModel` — they don't know about specifications |
| `Analyzers` depends on Roslyn APIs only | Compile-time tools must not reference runtime packages |
| `Generators` depends on Roslyn APIs only | Same constraint as Analyzers |

---

## 4. Forbidden Dependencies

These dependencies are explicitly prohibited and must be enforced via CI (`<PrivateAssets>all</PrivateAssets>` in test/sample projects):

| Package | May NOT depend on |
|---|---|
| `Abstractions` | Any external NuGet package, EF Core, Dapper, any ORM |
| `Core` | Any external NuGet package, EF Core, Dapper, any ORM |
| `Linq` | `Core` (may only use `Abstractions`); EF Core; Dapper |
| `Sql` | `Core`, EF Core, Dapper, any ORM |
| Dialect packages | EF Core, Dapper, application frameworks |

---

## 5. What Belongs Where

### `Abstractions` — Contract definitions

✅ Allowed:
- `ISpecification<T>` — the domain predicate interface
- `IExpressionSpecification<T>` — the expression-exposing interface
- `QuerySpec<T>` — the immutable query descriptor (filter + order + pagination)
- `QuerySpec<T, TResult>` — the projected query descriptor
- `IReadRepository<T>` — the read-side repository contract
- `OrderClause<T>`, `OrderDirection` — ordering value objects

❌ Not allowed:
- Implementation code
- EF Core types
- ORM references
- SQL types

---

### `Core` (`EricksonLopez.Specification`) — Domain engine

✅ Allowed:
- `Specification<T>` abstract base class
- `CompositeSpecification<T>` — composition result
- `Spec` — static factory (`Spec.For<T>`, `Spec.True<T>`, `Spec.False<T>`)
- `ExpressionComposer` — Invoke-free And/Or/Not/AndAll/OrAny
- `ExpressionSimplifier` — boolean constant folding
- `ExpressionHasher` — structural hashing for cache keys
- `ExpressionInterpreter` — AOT-safe tree evaluation
- `ExpressionCompilationCache` — JIT-only compiled delegate cache
- `ExpressionEqualityComparer` — structural expression equality
- `ParameterReplacer` — internal parameter rebinding
- `SpecificationDiagnostics` — OpenTelemetry meters/activities

❌ Not allowed:
- SQL types or AST
- ORM types
- Network I/O
- Async evaluation of external resources

---

### `Linq` — IQueryable adapter

✅ Allowed:
- `QuerySpecLinqExtensions.Apply<T>(IQueryable<T>, QuerySpec<T>)`
- `QuerySpecLinqExtensions.Apply<T, TResult>(IQueryable<T>, QuerySpec<T, TResult>)`
- `source.Any(spec)`, `source.Count(spec)` extensions
- `BuildCombinedPredicate` internal helper

❌ Not allowed:
- EF Core-specific APIs (use EF Core adapter in consumer code)
- SQL generation
- Direct database calls

---

### `Sql` — Expression→SQL translation

✅ Allowed:
- `QuerySpecTranslator<T>` — expression tree to `QueryModel` AST
- `QueryModel` + all `SqlPredicateNode` subtypes
- `ISqlDialect` + `SqlQuery`
- `QueryPlanCache` — bounded LRU SQL plan cache
- `IColumnNameResolver` + `SnakeCaseColumnNameResolver` + `VerbatimColumnNameResolver`
- `MsSqlDialect` — SQL Server rendering

❌ Not allowed:
- Direct database connections
- Dapper calls
- EF Core calls
- Application logic

---

### `Dapper` — Dapper execution adapter

✅ Allowed:
- `connection.QueryAsync<T>(spec, translator, dialect)`
- `connection.QueryFirstOrDefaultAsync<T>(spec, translator, dialect)`
- `connection.CountAsync<T>(spec, translator, dialect)`
- `connection.AnyAsync<T>(spec, translator, dialect)`

❌ Not allowed:
- SQL generation (delegate to `Sql`)
- Domain logic
- Transaction management beyond `IDbTransaction` passthrough

---

### `Analyzers` — Compile-time guardrails

✅ Allowed:
- SPEC001–SPEC010 diagnostic analyzers
- Diagnostic descriptors
- Code fix providers (future)

❌ Not allowed:
- Runtime dependencies
- Any reference to Core, Sql, or adapter packages

---

## 6. Explicit Boundaries for Extension

The following concerns are **outside all packages** and must remain the consumer's responsibility:

| Concern | Why it belongs to the consumer |
|---|---|
| `IRepository<T>` implementations | Infrastructure concern; the library provides the contract `IReadRepository<T>` only |
| Change tracking | ORM-specific; not a specification concern |
| Unit of Work | Transaction coordination; not a specification concern |
| Eager loading (Include/ThenInclude) | ORM graph loading; not a selection predicate |
| Result caching | Application caching policy; not specification evaluation |
| Authorization filtering | Security concern that should compose specifications, not be embedded in them |
| Pagination metadata (total count, page info) | Belongs to `EricksonLopez.Pagination` or consumer |
| Mapping/projection of results | Belongs to consumer or mapper library |
| CQRS dispatch | Belongs to mediator/dispatcher |
| Domain events | Belongs to domain event system |

---

## 7. Integration With Other EricksonLopez Libraries

| Library | Integration type | Rationale |
|---|---|---|
| `EricksonLopez.Result` | **No direct integration** | `IsSatisfiedBy` returns `bool`. Adding `Result<bool>` introduces an opinionated dependency into the domain core. Consumers may wrap calls if needed. |
| `EricksonLopez.Pagination` | **Extension package** | `QuerySpec<T>` provides `Skip`/`Take`. If `EricksonLopez.Pagination` provides `PageRequest` → `QuerySpec` adapters, they belong in a separate `EricksonLopez.Specification.Pagination` package. |
| `EricksonLopez.DomainPrimitive` | **No integration** | Domain primitives and specifications solve different problems. Specifications may operate on entities that use domain primitives, but there is no library-level coupling. |
| `EricksonLopez.SqlBuilder` | **Adapter package** | If a `SqlBuilder` library exists, a `EricksonLopez.Specification.SqlBuilder` adapter could bridge `QueryModel` → `SqlBuilder` syntax. Not in v1.0. |
| `EricksonLopez.Mapper` | **No integration** | Projection via `QuerySpec<T, TResult>.Select()` is the specification's projection surface. Mapping is the mapper's concern. |
| `EricksonLopez.Mediator` | **No integration** | CQRS dispatch is not a specification concern. Handlers may use specifications internally. |
| `EricksonLopez.SharedKernel` | **No integration** | Shared kernel types should not be a specification dependency. If common types are needed, they move to `Abstractions`. |

---

## 8. The "No" List — Things That Will Never Belong

The following have been explicitly evaluated and rejected. Each has an ADR:

| Feature | ADR | Reason |
|---|---|---|
| Write repository (`IRepository<T>`) | adr-001 | Specification is read-oriented; write operations have no specification predicate |
| Include/ThenInclude | adr-002 | Navigation loading is ORM graph concern, not predicate concern |
| String-based ordering | adr-003 | Type safety violation; runtime errors |
| SAT-based simplification | adr-004 | Algorithmic complexity without real-world value |
| XOR composition | adr-005 | Zero documented use cases; composable from AND/OR/NOT |
| FluentValidation integration | adr-007 | Different problem domain; would create bidirectional dependency |
| Dynamic LINQ / string queries | adr-011, adr-014 | SQL injection risk; type safety violation |
| Raw SQL in public API | adr-013 | Security; bypasses the type-safe expression pipeline |
| GroupBy/Aggregations | adr-015 | OLAP concerns; not selection predicates |
| Auto-generated BuildExpression | adr-016 | Violates domain ownership of business rules |
| Async specifications | adr-017 | Anti-pattern DDD; predicates are synchronous |
| ORM hints in QuerySpec | adr-018 | Provider-agnostic type cannot contain ORM-specific hints |

---

*Last updated: 2026-08-14 — Post-audit restructuring*
