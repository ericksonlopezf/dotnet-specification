# MASTER FEATURE MATRIX — EricksonLopez.Specification

> **Version**: 1.0 — Post-Audit Execution (2026-08-14)  
> **Auditor/Architect**: Principal .NET Architect & DDD Specialist  
> **Repository**: `EricksonLopez.Specification` (.NET 10 / C# 13)

---

## 1. Feature Classification Taxonomy

Each feature in this matrix is categorized according to strict DDD and Clean Architecture boundaries:

* **CORE**: Fundamental to the Specification pattern. Pure domain concept, independent of any infrastructure or framework.
* **DOMAIN**: Domain layer rules and compile-time architectural governance (e.g., Roslyn Analyzers).
* **APPLICATION**: Query intention, selection, ordering, and pagination descriptors (`QuerySpec<T>`).
* **ADAPTER**: Infrastructure-specific translation and integration mechanisms (SQL dialects, LINQ, Dapper).
* **OPTIONAL**: Ergonomic convenience methods and helpers that do not alter core semantics.
* **EXPERIMENTAL**: Forward-looking features reserved for validation (e.g., v2.0 Source Generators).
* **OUT-OF-SCOPE**: Concerns belonging to other architectural patterns (e.g., Aggregations, Subqueries, Joins).
* **DEPRECATED**: Legacy concepts slated for removal.
* **REJECTED**: Explicitly evaluated and rejected by an Architectural Decision Record (ADR).

---

## 2. Priority Definitions

* **P0 (Critical)**: Non-negotiable core invariants, correctness guarantees, and fundamental APIs.
* **P1 (High)**: Major architectural capabilities, core performance hot paths, and primary ergonomics.
* **P2 (Medium)**: Quality-of-life enhancements, dialect extensions, and secondary tooling.
* **P3 (Low)**: Minor syntactic sugar, future enhancements, and low-frequency edge cases.
* **P4 (Rejected)**: Explicitly rejected by ADR; must NOT be implemented.

---

## 3. Comprehensive Feature Matrix

### 3.1 Predicate Construction & Core Specification

| Domain | Feature | Status | Classification | Priority | Package | DDD Impact | AOT Safety | Performance | Complexity | Decision |
|---|---|---|---|---|---|---|---|---|---|---|
| Core | `Specification<T>` abstract base | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ⚠️ Conditional | Zero Alloc (Cached) | Low | **KEEP** |
| Core | `ISpecification<T>` minimal contract | ✅ Implemented | CORE | P0 | Abstractions | Pure Domain | ✅ Full | Zero Alloc | Low | **KEEP** |
| Core | `IExpressionSpecification<T>` | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ⚠️ Conditional | Zero Alloc | Low | **KEEP** |
| Core | `BuildExpression()` lazy cache | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ✅ Full | Zero Alloc | Low | **KEEP** |
| Core | `IsSatisfiedBy(T)` (interpreted) | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ⚠️ Conditional (Reflection) | ~500ns / 0 B | High | **KEEP** |
| Core | `ToCompiledPredicate()` | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ❌ RequiresDynamicCode | ~15ns / 0 B (Cached) | Medium | **KEEP** |
| Core | `ToExpression()` accessor | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ✅ Full | Zero Alloc | Low | **KEEP** |
| Core | `Spec.For<T>(expr)` factory | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ✅ Full | 1 Alloc (LambdaSpec) | Low | **KEEP** |
| Core | `Spec.True<T>()` tautology | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ✅ Full | Zero Alloc (Static) | Low | **KEEP** |
| Core | `Spec.False<T>()` contradiction | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ✅ Full | Zero Alloc (Static) | Low | **KEEP** |
| Core | Binary Equality (`==`, `!=`) | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ✅ Full | Fast | Low | **KEEP** |
| Core | Relational (`>`, `>=`, `<`, `<=`) | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ✅ Full | Fast | Low | **KEEP** |
| Core | Boolean Member (`c.IsActive`) | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ✅ Full | Fast | Low | **KEEP** |
| Core | Null Checks (`c.Prop == null`) | ✅ Implemented | CORE | P0 | Specification | Pure Domain | ✅ Full | Fast | Low | **KEEP** |
| Core | String `Contains/StartsWith/EndsWith` | ✅ Implemented | CORE | P1 | Specification | Pure Domain | ✅ Full | Fast | Medium | **KEEP** |
| Core | Collection `Contains` (`IN`) | ✅ Implemented | CORE | P1 | Specification | Pure Domain | ✅ Full | Fast | Medium | **KEEP** |
| Core | Conditional Ternary (`?:`) | ✅ Implemented | CORE | P1 | Specification | Pure Domain | ✅ Full | Fast | Medium | **KEEP** |
| Core | Null Coalescing (`??`) | ✅ Implemented | CORE | P1 | Specification | Pure Domain | ✅ Full | Fast | Medium | **KEEP** |
| Core | Type Check (`is` / TypeBinary) | ✅ Implemented | CORE | P2 | Specification | Pure Domain | ✅ Full | Fast | Medium | **KEEP** |
| Core | `Between` Range Operator | ✅ Implemented | OPTIONAL | P1 | Specification | Pure Domain | ✅ Full | Fast / Native SQL | Low | **KEEP** |
| Core | Raw SQL `WhereRaw()` | ❌ Rejected | REJECTED | P4 | N/A | Anti-pattern | N/A | N/A | Low | **REJECT (adr-013)** |
| Core | Dynamic String Predicates | ❌ Rejected | REJECTED | P4 | N/A | Anti-pattern | ❌ Broken | Reflection | High | **REJECT (adr-011)** |
| Core | Async Specifications (`Task<bool>`) | ❌ Rejected | REJECTED | P4 | N/A | Anti-pattern | N/A | I/O Pollution | High | **REJECT (adr-017)** |

---

### 3.2 Composition & Boolean Algebra

| Domain | Feature | Status | Classification | Priority | Package | DDD Impact | AOT Safety | Performance | Complexity | Decision |
|---|---|---|---|---|---|---|---|---|---|---|
| Composition | `spec1.And(spec2)` (AndAlso) | ✅ Implemented | CORE | P0 | Specification | High | ✅ Full | Minimal Alloc | Low | **KEEP** |
| Composition | `spec1.Or(spec2)` (OrElse) | ✅ Implemented | CORE | P0 | Specification | High | ✅ Full | Minimal Alloc | Low | **KEEP** |
| Composition | `spec.Not()` | ✅ Implemented | CORE | P0 | Specification | High | ✅ Full | Minimal Alloc | Low | **KEEP** |
| Composition | `ExpressionComposer.And` | ✅ Implemented | CORE | P0 | Specification | High | ✅ Full | ~200ns / ~300 B | Low | **KEEP** |
| Composition | `ExpressionComposer.Or` | ✅ Implemented | CORE | P0 | Specification | High | ✅ Full | ~200ns / ~300 B | Low | **KEEP** |
| Composition | `ExpressionComposer.Not` | ✅ Implemented | CORE | P0 | Specification | High | ✅ Full | ~100ns / ~150 B | Low | **KEEP** |
| Composition | `ExpressionComposer.AndAll(Span)` | ✅ Implemented | CORE | P1 | Specification | High | ✅ Full | Minimal Alloc | Medium | **KEEP** |
| Composition | `ExpressionComposer.OrAny(Span)` | ✅ Implemented | CORE | P1 | Specification | High | ✅ Full | Minimal Alloc | Medium | **KEEP** |
| Composition | `Spec.All<T>(params specs)` | ✅ Implemented | OPTIONAL | P1 | Specification | High (DX) | ✅ Full | Span-delegated | Low | **KEEP (adr-022)** |
| Composition | `Spec.Any<T>(params specs)` | ✅ Implemented | OPTIONAL | P1 | Specification | High (DX) | ✅ Full | Span-delegated | Low | **KEEP (adr-022)** |
| Composition | Identity Simplification (`A && true = A`) | ✅ Implemented | CORE | P1 | Specification | High | ✅ Full | Zero Alloc Bypass | Low | **KEEP (adr-025)** |
| Composition | Double Negation (`!!A = A`) | ✅ Implemented | CORE | P1 | Specification | High | ✅ Full | Visitor-based | Low | **KEEP** |
| Composition | `XOR` Composition | ❌ Rejected | REJECTED | P4 | N/A | Low | N/A | N/A | Low | **REJECT (adr-005)** |
| Composition | Full SAT Solver Simplification | ❌ Rejected | REJECTED | P4 | N/A | Low | N/A | Heavy CPU | High | **REJECT (adr-004)** |

---

### 3.3 Expression Tree Engine & Caching

| Domain | Feature | Status | Classification | Priority | Package | DDD Impact | AOT Safety | Performance | Complexity | Decision |
|---|---|---|---|---|---|---|---|---|---|---|
| Engine | Parameter Rebinding (No `Invoke`) | ✅ Implemented | CORE | P0 | Specification | High | ✅ Full | Minimal Alloc | Medium | **KEEP** |
| Engine | Structural Equality Comparer | ✅ Implemented | CORE | P0 | Specification | High | ✅ Full | Deep Tree Traversal | High | **KEEP** |
| Engine | Structural Expression Hasher | ✅ Implemented | CORE | P0 | Specification | High | ✅ Full | djb2 HashCode | Medium | **KEEP** |
| Engine | `ExpressionCompilationCache` | ✅ Implemented | CORE | P0 | Specification | High | ❌ JIT Only | ConcurrentDictionary | Medium | **KEEP (adr-019)** |
| Engine | `ExpressionSimplifier` | ✅ Implemented | CORE | P1 | Specification | High | ✅ Full | ExpressionVisitor | Medium | **KEEP** |
| Engine | `ExpressionInterpreter` (AOT) | ✅ Implemented | CORE | P0 | Specification | High | ⚠️ Conditional | Zero Alloc Eval | High | **KEEP** |
| Engine | `ExpressionDebugFormatter` (AOT) | ✅ Implemented | OPTIONAL | P1 | Specification | High | ✅ Full | String Building | Low | **KEEP** |

---

### 3.4 Query Specification (`QuerySpec<T>`) & Application Layer

| Domain | Feature | Status | Classification | Priority | Package | DDD Impact | AOT Safety | Performance | Complexity | Decision |
|---|---|---|---|---|---|---|---|---|---|---|
| Application | `QuerySpec<T>` Sealed Immutable Record | ✅ Implemented | APPLICATION | P0 | Abstractions | Clean Separation | ✅ Full | Record with-mutations | Low | **KEEP (adr-006)** |
| Application | `QuerySpec<T, TResult>` with Select | ✅ Implemented | APPLICATION | P1 | Abstractions | Clean Separation | ✅ Full | Strongly-typed projection | Low | **KEEP (adr-012)** |
| Application | `QuerySpec<T>.Empty` Singleton | ✅ Implemented | APPLICATION | P0 | Abstractions | Clean Separation | ✅ Full | Zero Alloc | Low | **KEEP** |
| Application | `Where(predicate)` Filtering | ✅ Implemented | APPLICATION | P0 | Abstractions | Clean Separation | ✅ Full | ImmutableArray.Add | Low | **KEEP** |
| Application | `OrderBy<TKey>` Strongly-Typed | ✅ Implemented | APPLICATION | P0 | Abstractions | Clean Separation | ✅ Full | Boxing to object | Low | **KEEP** |
| Application | `OrderByDescending<TKey>` | ✅ Implemented | APPLICATION | P0 | Abstractions | Clean Separation | ✅ Full | Boxing to object | Low | **KEEP** |
| Application | `ThenBy<TKey>` / `ThenByDescending` | ✅ Implemented | APPLICATION | P1 | Abstractions | Clean Separation | ✅ Full | Preserved Sequence | Low | **KEEP** |
| Application | `Page(page, pageSize)` | ✅ Implemented | APPLICATION | P1 | Abstractions | Clean Separation | ✅ Full | Zero Overhead | Low | **KEEP** |
| Application | `Skip(n)` / `Take(n)` | ✅ Implemented | APPLICATION | P1 | Abstractions | Clean Separation | ✅ Full | Zero Overhead | Low | **KEEP** |
| Application | `Distinct()` | ✅ Implemented | APPLICATION | P2 | Abstractions | Clean Separation | ✅ Full | Flag | Low | **KEEP** |
| Application | `IReadRepository<T>` Minimal Port | ✅ Implemented | APPLICATION | P1 | Abstractions | Clean Separation | ✅ Full | Async Task APIs | Low | **KEEP (adr-001)** |
| Application | Spec-to-QuerySpec Implicit Bridge | ✅ Implemented | APPLICATION | P1 | Specification | High (DX) | ✅ Full | Zero Alloc | Low | **KEEP** |
| Application | Keyset / Cursor Pagination (`SeekAfter`/`SeekBefore`) | ✅ Implemented | APPLICATION | P1 | Abstractions | Clean Separation | ✅ Full | O(1) Index Seek | Medium | **KEEP** |
| Application | `Include` / `ThenInclude` in Core | ❌ Rejected | REJECTED | P4 | N/A | ORM Leak | N/A | N/A | High | **REJECT (adr-002)** |
| Application | `AsNoTracking` / `AsSplitQuery` in Core | ❌ Rejected | REJECTED | P4 | N/A | ORM Leak | N/A | N/A | Low | **REJECT (adr-018)** |
| Application | String-based `OrderBy("Prop")` | ❌ Rejected | REJECTED | P4 | N/A | Anti-pattern | ❌ Broken | Reflection | Medium | **REJECT (adr-003)** |
| Application | `GroupBy` / `SelectMany` / Aggregations | ❌ Rejected | REJECTED | P4 | N/A | Scope Creep | N/A | N/A | High | **REJECT (adr-015)** |

---

### 3.5 Infrastructure Adapters (SQL, Dialects, LINQ, Dapper)

| Domain | Feature | Status | Classification | Priority | Package | DDD Impact | AOT Safety | Performance | Complexity | Decision |
|---|---|---|---|---|---|---|---|---|---|---|
| Adapter | `QuerySpecTranslator<T>` AST | ✅ Implemented | ADAPTER | P0 | Sql | Isolated Adapter | ⚠️ Annotated | Fast Expression Walk | High | **KEEP** |
| Adapter | `QueryPlanCache` (LRU Bounded 512) | ✅ Implemented | ADAPTER | P0 | Sql | Isolated Adapter | ✅ Full | ~100ns Hit Path | Medium | **KEEP (adr-021)** |
| Adapter | `ISqlDialect` Abstraction | ✅ Implemented | ADAPTER | P0 | Sql | Isolated Adapter | ✅ Full | Zero Alloc | Low | **KEEP** |
| Adapter | `IColumnNameResolver` Abstraction | ✅ Implemented | ADAPTER | P0 | Sql | Isolated Adapter | ✅ Full | Zero Alloc | Low | **KEEP** |
| Adapter | `SnakeCaseColumnNameResolver` | ✅ Implemented | ADAPTER | P0 | Sql | Isolated Adapter | ⚠️ Runtime regex | Fast string buffer | Low | **KEEP** |
| Adapter | Compile-Time `SpecColumnResolver` Generator | ✅ Implemented | ADAPTER | P1 | Generators | Zero Reflection | ✅ Full | O(1) Switch Jump | Medium | **KEEP (adr-020)** |
| Adapter | `MsSqlDialect` (SQL Server) | ✅ Implemented | ADAPTER | P0 | MsSql | Isolated Adapter | ✅ Full | Native OFFSET/FETCH | Low | **KEEP** |
| Adapter | `PostgreSqlDialect` (FullText & Ranges) | ✅ Implemented | ADAPTER | P0 | PostgreSql | Isolated Adapter | ✅ Full | tsvector / int4range | Medium | **KEEP** |
| Adapter | `MySqlDialect` (MySQL) | ✅ Implemented | ADAPTER | P0 | MySql | Isolated Adapter | ✅ Full | Backticks / LIMIT | Low | **KEEP** |
| Adapter | `MariaDbDialect` (MariaDB) | ✅ Implemented | ADAPTER | P0 | MariaDb | Isolated Adapter | ✅ Full | Backticks / LIMIT | Low | **KEEP** |
| Adapter | `OracleDialect` (Oracle Database) | ✅ Implemented | ADAPTER | P0 | Oracle | Isolated Adapter | ✅ Full | :params / OFFSET FETCH | Low | **KEEP** |
| Adapter | `QuerySpecLinqExtensions.Apply` | ✅ Implemented | ADAPTER | P0 | Linq | Isolated Adapter | ✅ Full | Standard IQueryable | Low | **KEEP** |
| Adapter | `QuerySpecDapperExtensions` (Async) | ✅ Implemented | ADAPTER | P0 | Dapper | Isolated Adapter | ✅ Full | Zero ORM overhead | Low | **KEEP** |

---

### 3.6 Compile-Time Governance (Roslyn Analyzers & CodeFix Providers)

| Domain | Rule ID | Title | Status | Severity | Classification | Priority | Package | Decision |
|---|---|---|---|---|---|---|---|---|
| Analyzer | SPEC001 | Specification must be sealed or abstract (+ CodeFix) | ✅ Implemented | Warning | DOMAIN | P0 | Analyzers | **KEEP** |
| Analyzer | SPEC002 | Specification cannot contain mutable state | ✅ Implemented | Warning | DOMAIN | P0 | Analyzers | **KEEP** |
| Analyzer | SPEC003 | Detect InvocationExpression in expressions | ✅ Implemented | Error | DOMAIN | P0 | Analyzers | **KEEP** |
| Analyzer | SPEC004 | Unbounded query detected (missing Take/Page) | ✅ Implemented | Info | DOMAIN | P1 | Analyzers | **KEEP** |
| Analyzer | SPEC005 | Ordering without pagination warning | ✅ Implemented | Info | DOMAIN | P2 | Analyzers | **KEEP** |
| Analyzer | SPEC006 | Domain specification defined outside Domain | ✅ Implemented | Info | DOMAIN | P2 | Analyzers | **KEEP** |
| Analyzer | SPEC007 | Non-translatable method in BuildExpression | ✅ Implemented | Warning | DOMAIN | P1 | Analyzers | **KEEP** |
| Analyzer | SPEC008 | Infrastructure dependency in Spec ctor | ✅ Implemented | Warning | DOMAIN | P0 | Analyzers | **KEEP** |
| Analyzer | SPEC009 | Async lambda in BuildExpression | ✅ Implemented | Error | DOMAIN | P0 | Analyzers | **KEEP** |
| Analyzer | SPEC010 | IsSatisfiedBy call inside BuildExpression | ✅ Implemented | Error | DOMAIN | P0 | Analyzers | **KEEP** |
| Analyzer | SPEC011 | Legacy Ardalis.Specification detected (+ CodeFix) | ✅ Implemented | Info | DOMAIN | P1 | Analyzers | **KEEP** |
