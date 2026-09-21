# API Reference: EricksonLopez.Specification

Official Microsoft Learn-style reference for the public API surface of **EricksonLopez.Specification**.

---

## Table of Contents

1. [Specification&lt;T&gt;](#specificationt)
2. [Spec (Static Combinators)](#spec-static-combinators)
3. [ExpressionDebugFormatterRegistry](#expressiondebugformatterregistry)
4. [QuerySpec&lt;T&gt; and QuerySpec&lt;T, TResult&gt;](#queryspect-and-queryspect-tresult)
5. [QuerySpecExtensions](#queryspecextensions)
6. [QuerySpecLinqExtensions](#queryspeclinqextensions)
7. [IReadRepository&lt;T&gt;](#ireadrepositoryt)
8. [ReadRepositoryResultExtensions](#readrepositoryresultextensions)
9. [QuerySpecTranslator&lt;T&gt; and SQL Dialects](#queryspectranslatort-and-sql-dialects)
10. [QueryPlanCache](#queryplancache)
11. [ExpressionCompilationCache](#expressioncompilationcache)
12. [Expression Engine &amp; Diagnostics](#expression-engine--diagnostics)
13. [MongoDB Integrations](#mongodb-integrations)
14. [Dapper Integrations](#dapper-integrations)

---

## Specification&lt;T&gt;

Namespace: `EricksonLopez.Specification`  
Assembly: `EricksonLopez.Specification.dll`

The abstract base class for all domain specifications encapsulating a business predicate for type `T`.

### Signatures & Members

```csharp
public abstract class Specification<[DynamicallyAccessedMembers(...)] T> : ISpecification<T>, IExpressionSpecification<T>
{
    protected abstract Expression<Func<T, bool>> BuildExpression();
    public bool IsSatisfiedBy(T candidate);
    public Expression<Func<T, bool>> ToExpression();
    public Func<T, bool> ToCompiledPredicate();
    public string ToDebugString();
    public QuerySpec<T> ToQuerySpec();
    public QuerySpec<T> ToQuerySpec(QuerySpec<T> baseQuerySpec);

    public Specification<T> And(Specification<T> other);
    public Specification<T> Or(Specification<T> other);
    public Specification<T> Not();

    public static implicit operator QuerySpec<T>(Specification<T> specification);

    public static Specification<T> operator &(Specification<T> left, Specification<T> right);
    public static Specification<T> BitwiseAnd(Specification<T> left, Specification<T> right);
    public static Specification<T> operator |(Specification<T> left, Specification<T> right);
    public static Specification<T> BitwiseOr(Specification<T> left, Specification<T> right);
    public static Specification<T> operator !(Specification<T> specification);
    public static Specification<T> LogicalNot(Specification<T> specification);
    public static bool operator true(Specification<T> specification);
    public static bool operator false(Specification<T> specification);
}
```

### Methods

#### `IsSatisfiedBy(T candidate)`
- **Parameters**: `candidate`: The entity instance to evaluate.
- **Return**: `true` if the candidate satisfies the specification predicate; otherwise, `false`.
- **Exceptions**: `ArgumentNullException` if `candidate` is `null`.
- **Remarks**: Evaluated in-memory via `ExpressionInterpreter.Evaluate`. 100% Native AOT safe; does not perform IL emit or reflection compilation.
- **When to use**: Validating entities in domain models, CQRS command handlers, or unit test assertions.
- **When NOT to use**: Querying a database (use `ToQuerySpec()` or LINQ extensions instead).

#### `ToExpression()`
- **Return**: The underlying `Expression<Func<T, bool>>`.
- **Remarks**: The expression is initialized once lazily via `BuildExpression()` and cached for the lifetime of the instance. Thread-safe.

#### `ToCompiledPredicate()`
- **Return**: A compiled `Func<T, bool>` delegate.
- **Attributes**: `[RequiresDynamicCode("Compiles the specification expression to a delegate at runtime. Not compatible with Native AOT.")]`, `[RequiresUnreferencedCode("Expression compilation may require types that are trimmed.")]`
- **Performance**: High throughput for JIT runtimes evaluating millions of entities in loops. Uses `ExpressionCompilationCache`.

#### `operator &`, `operator |`, `operator !`
- **Parameters**: Left and right specifications to compose.
- **Return**: A composite `CompositeSpecification<T>` or `NegatedSpecification<T>`.
- **Remarks**: Rewrites parameter expressions via `ParameterReplacer` without inserting `Expression.Invoke`.

---

## Spec (Static Combinators)

Namespace: `EricksonLopez.Specification`  
Assembly: `EricksonLopez.Specification.dll`

Static factory class providing combinators, ranges, and search utilities.

### Methods

#### `For<T>(Expression<Func<T, bool>> predicate)`
- **Parameters**: `predicate`: The boolean lambda expression.
- **Return**: A `LambdaSpecification<T>`.
- **Exceptions**: `ArgumentNullException` if `predicate` is null.
- **When to use**: One-off predicates or prototyping.
- **When NOT to use**: Core business rules that require dedicated domain classes and isolated unit tests.

#### `True<T>()` and `False<T>()`
- **Return**: Neutral identity specifications (`c => true` and `c => false`).
- **Remarks**: `spec.And(Spec.True<T>())` yields `spec`. Essential for conditional query filters.

#### `All<T>(params Specification<T>[] specifications)` / `All<T>(IEnumerable<Specification<T>> specifications)`
- **Parameters**: Collection of specifications.
- **Return**: A single specification representing logical AND across all inputs. Span-optimized.
- **Remarks**: Passing 0 elements returns `Spec.True<T>()`; 1 element returns that element directly.

#### `Any<T>(params Specification<T>[] specifications)` / `Any<T>(IEnumerable<Specification<T>> specifications)`
- **Parameters**: Collection of specifications.
- **Return**: A single specification representing logical OR across all inputs. Span-optimized.
- **Remarks**: Passing 0 elements returns `Spec.False<T>()`; 1 element returns that element directly.

#### `Between<T, TProperty>(Expression<Func<T, TProperty>> selector, TProperty lower, TProperty upper)`
- **Parameters**:
  - `selector`: Property selector expression.
  - `lower`: Inclusive lower bound.
  - `upper`: Inclusive upper bound.
- **Return**: Specification asserting `lower <= property && property <= upper`.
- **Exceptions**:
  - `ArgumentNullException` if `selector` is null.
  - `ArgumentException` if `lower.CompareTo(upper) > 0`.

#### `Between<T, TProperty>(Expression<Func<T, TProperty?>> selector, TProperty lower, TProperty upper)`
- **Parameters**: Property selector for nullable struct property `TProperty?`.
- **Return**: Specification asserting `property != null && lower <= property.Value && property.Value <= upper`.

---

## ExpressionDebugFormatterRegistry

Namespace: `EricksonLopez.Specification`  
Assembly: `EricksonLopez.Specification.Abstractions.dll`

Cross-layer registry for configuring human-readable debug formatting of expression trees.

### Members

```csharp
public static class ExpressionDebugFormatterRegistry
{
    public static Func<Expression, string> Formatter { get; set; }
    public static string Format(Expression expression);
}
```

- **Remarks**: In `EricksonLopez.Specification`, the static constructor automatically registers `ExpressionDebugFormatter.Format` as the default formatter.

---

## QuerySpec&lt;T&gt; and QuerySpec&lt;T, TResult&gt;

Namespace: `EricksonLopez.Specification`  
Assembly: `EricksonLopez.Specification.Abstractions.dll`

Immutable records describing relational and non-relational query intent.

### Methods

| Method | Signature | Description |
|---|---|---|
| `Where` | `QuerySpec<T> Where(Expression<Func<T, bool>> predicate)` | Appends an AND filter criterion |
| `TagWith` | `QuerySpec<T> TagWith(string tag)` | Sets diagnostic SQL comment tag |
| `Search` | `QuerySpec<T> Search(string phrase, params Expression<Func<T, string?>>[] selectors)` | Multi-column OR search |
| `OrderBy` | `QuerySpec<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)` | Primary ascending sort |
| `OrderByDescending` | `QuerySpec<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)` | Primary descending sort |
| `ThenBy` | `QuerySpec<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)` | Secondary ascending sort |
| `ThenByDescending` | `QuerySpec<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)` | Secondary descending sort |
| `Page` | `QuerySpec<T> Page(int page, int pageSize)` | 1-based pagination (`Skip = (page-1)*pageSize, Take = pageSize`) |
| `Take` | `QuerySpec<T> Take(int count)` | Limits returned rows |
| `Skip` | `QuerySpec<T> Skip(int count)` | Offsets returned rows |
| `Distinct` | `QuerySpec<T> Distinct()` | Sets `IsDistinct = true` (`SELECT DISTINCT`) |
| `SeekAfter` | `QuerySpec<T> SeekAfter<TKey>(Expression<Func<T, TKey>> keySelector, TKey cursor, int take)` | Keyset pagination forward |
| `SeekBefore` | `QuerySpec<T> SeekBefore<TKey>(Expression<Func<T, TKey>> keySelector, TKey cursor, int take)` | Keyset pagination backward |
| `WithCursor` | `QuerySpec<T> WithCursor<TKey>(Expression<Func<T, TKey>> keySelector, TKey cursorValue, CursorDirection direction, int take)` | Sets keyset cursor directly with explicit direction |
| `Select` | `QuerySpec<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector)` | Projects to `TResult` |

### QuerySpec&lt;T&gt; / QuerySpec&lt;T, TResult&gt; — Read-only Properties

| Property | Type | Description |
|---|---|---|
| `Criteria` | `ImmutableArray<Expression<Func<T, bool>>>` | AND-combined filter predicates |
| `OrderClauses` | `ImmutableArray<OrderClause<T>>` | Ordering clauses in sequence |
| `Selector` | `Expression<Func<T, TResult>>?` | Projection selector (`QuerySpec<T, TResult>` only) |
| `SkipCount` | `int?` | Number of records to skip for offset pagination |
| `TakeCount` | `int?` | Maximum records to return |
| `IsDistinct` | `bool` | Whether to eliminate duplicate results |
| `Tag` | `string?` | Diagnostic query comment |
| `Cursor` | `CursorClause<T>?` | Keyset pagination cursor |
| `Empty` | `static QuerySpec<T>` | Singleton empty specification (no constraints) |

---

## QuerySpecExtensions

Namespace: `EricksonLopez.Specification`  
Assembly: `EricksonLopez.Specification.dll`

Extension methods for combining `QuerySpec<T>` with domain specifications and inspecting query state.

- `QuerySpec<T> And<T>(this QuerySpec<T> querySpec, Specification<T> specification)` — Appends a domain specification predicate as an AND filter.
- `QuerySpec<T> Where<T>(this QuerySpec<T> querySpec, IExpressionSpecification<T> specification)` — Same as `And` but accepts the base `IExpressionSpecification<T>` interface (works with `Spec.For<T>()` results and all subclasses).
- `Expression<Func<T, bool>>? BuildCombinedPredicate<T>(this QuerySpec<T> querySpec)` — Combines all criteria into a single AND predicate using `ExpressionComposer.AndAll`. Returns `null` if no criteria are defined.
- `bool HasOrdering<T>(this QuerySpec<T> querySpec)` — Returns `true` if at least one `OrderClause` is defined.
- `bool HasPagination<T>(this QuerySpec<T> querySpec)` — Returns `true` if `SkipCount` or `TakeCount` is set.
- `bool HasCriteria<T>(this QuerySpec<T> querySpec)` — Returns `true` if at least one filter predicate exists.

---

## QuerySpecLinqExtensions

Namespace: `EricksonLopez.Specification.Linq`  
Assembly: `EricksonLopez.Specification.Linq.dll`

High-performance LINQ extensions for `IQueryable<T>` and in-memory `IEnumerable<T>`.

### `IQueryable<T>` Extensions

- `IQueryable<T> Apply<T>(this IQueryable<T> source, QuerySpec<T> spec)`: Applies criteria, ordering, and pagination to an IQueryable.
- `IQueryable<TResult> Apply<T, TResult>(this IQueryable<T> source, QuerySpec<T, TResult> spec)`: Applies criteria, ordering, pagination, and projection.
- `bool Any<T>(this IQueryable<T> source, QuerySpec<T> spec)`: Checks existence using `QuerySpec`.
- `int Count<T>(this IQueryable<T> source, QuerySpec<T> spec)`: Counts matching elements.
- `IQueryable<T> Where<T>(this IQueryable<T> source, IExpressionSpecification<T> spec)`: Direct specification filtering.
- `bool All<T>(this IQueryable<T> source, IExpressionSpecification<T> spec)`: Tests if all elements satisfy specification.
- `T? FirstOrDefault<T>(this IQueryable<T> source, IExpressionSpecification<T> spec)`: Returns first match or default.

### `IEnumerable<T>` In-Memory Extensions

- `IEnumerable<T> Where<T>(this IEnumerable<T> source, ISpecification<T> spec)`: Filters in-memory sequence via `IsSatisfiedBy`.
- `bool Any<T>(this IEnumerable<T> source, ISpecification<T> spec)`: Determines if any element matches.
- `bool All<T>(this IEnumerable<T> source, ISpecification<T> spec)`: Determines if all elements match.
- `int Count<T>(this IEnumerable<T> source, ISpecification<T> spec)`: Counts matching items.
- `T? FirstOrDefault<T>(this IEnumerable<T> source, ISpecification<T> spec)`: First matching item or default.

---

## IReadRepository&lt;T&gt;

Namespace: `EricksonLopez.Specification`  
Assembly: `EricksonLopez.Specification.Abstractions.dll`

Pure asynchronous read-only repository contract.

```csharp
public interface IReadRepository<T>
{
    Task<IReadOnlyList<T>> ListAsync(QuerySpec<T> spec, CancellationToken ct = default);
    Task<IReadOnlyList<TResult>> ListAsync<TResult>(QuerySpec<T, TResult> spec, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(QuerySpec<T> spec, CancellationToken ct = default);
    Task<T?> SingleOrDefaultAsync(QuerySpec<T> spec, CancellationToken ct = default);
    Task<int> CountAsync(QuerySpec<T> spec, CancellationToken ct = default);
    Task<bool> AnyAsync(QuerySpec<T> spec, CancellationToken ct = default);
    Task<T?> GetByIdAsync<TId>(TId id, CancellationToken ct = default);
}
```

---

## ReadRepositoryResultExtensions

Namespace: `EricksonLopez.Specification.Result`  
Assembly: `EricksonLopez.Specification.Result.dll`

Extends `IReadRepository<T>` with functional `Result<T>` envelopes (`EricksonLopez.Result`).

- `Task<Result<T>> FirstOrDefaultResultAsync<T>(this IReadRepository<T> repo, QuerySpec<T> spec, CancellationToken ct = default)`: Returns `Result.Success(entity)` or `Result.Failure(Error.NotFound)` if not found.
- `Task<Result<IReadOnlyList<T>>> ListResultAsync<T>(this IReadRepository<T> repo, QuerySpec<T> spec, CancellationToken ct = default)`: Returns `Result.Success(list)` or `Result.Failure(Error.Failure)`.
- `Task<Result<T>> SingleOrDefaultResultAsync<T>(this IReadRepository<T> repo, QuerySpec<T> spec, CancellationToken ct = default)`: Returns `Result.Failure(Error.Conflict)` if multiple entities match.
- `Task<Result<T>> GetByIdResultAsync<T, TId>(this IReadRepository<T> repo, TId id, CancellationToken ct = default)`: Returns `Result.Success(entity)` or `Result.Failure(Error.NotFound)`.

> [!IMPORTANT]
> `OperationCanceledException` is preserved and rethrown immediately across all methods to ensure cooperative task cancellation.

---

## QuerySpecTranslator&lt;T&gt; and SQL Dialects

Namespace: `EricksonLopez.Specification.Sql`  
Assembly: `EricksonLopez.Specification.Sql.dll`

Parses `QuerySpec<T>` into a provider-agnostic SQL Abstract Syntax Tree (`QueryModel`) and renders native engine SQL.

### Supported Dialects

- `PostgreSqlDialect.Default` (`EricksonLopez.Specification.PostgreSql`)
- `MsSqlDialect.Default` (`EricksonLopez.Specification.MsSql`)
- `SqliteDialect.Default` (`EricksonLopez.Specification.Sqlite`)
- `MySqlDialect.Default` (`EricksonLopez.Specification.MySql`)
- `MariaDbDialect.Default` (`EricksonLopez.Specification.MariaDb`)
- `OracleDialect.Default` (`EricksonLopez.Specification.Oracle`)

---

## QueryPlanCache

Namespace: `EricksonLopez.Specification.Sql`  
Assembly: `EricksonLopez.Specification.Sql.dll`

Thread-safe bounded LRU cache for translated `QueryModel` query plans.

- `Capacity`: Maximum cached plans (default 512, configurable via property setter).
- `Count`: Number of currently cached plans.
- `Clear()`: Evicts all cached entries.
- **Cache Key**: `CacheKey` struct combining table name and expression structural equality via `ExpressionEqualityComparer.Default.Equals(...)`. Two specifications with structurally identical expressions for the same table share the same cache entry.

> [!NOTE]
> `QueryPlanCache` is a static, process-wide cache. In applications with highly dynamic specification composition (e.g. user-defined filters), consider tuning `QueryPlanCache.Capacity` to prevent memory pressure.

---

## ExpressionCompilationCache

Namespace: `EricksonLopez.Specification`  
Assembly: `EricksonLopez.Specification.dll`

Thread-safe bounded LRU cache for compiled expression delegates. **JIT-only** — all methods annotated `[RequiresDynamicCode]`.

- `Capacity`: Maximum cached delegates (default 512, configurable). Setting a smaller value evicts the least recently used entries immediately.
- `CachedCount`: Number of compiled delegates currently held in the cache.
- `GetOrCompile<T>(Expression<Func<T, bool>> expression)`: Returns a cached compiled delegate or compiles and caches one. Annotated `[RequiresDynamicCode]` and `[RequiresUnreferencedCode]`.
- **Cache Key**: The expression tree compared via `ExpressionEqualityComparer.Default` (deep structural AST equality). Prevents wrong-delegate returns on hash collision.

> [!CAUTION]
> `ExpressionCompilationCache` is not usable in Native AOT applications. Use `spec.IsSatisfiedBy(candidate)` instead, which evaluates via `ExpressionInterpreter`.

---

## Expression Engine & Diagnostics

### `ExpressionInterpreter`
- `Evaluate<T>(Expression<Func<T, bool>> expression, T candidate)`: AOT-safe evaluation. The sole public entry point — no `Interpret()` public method exists.

### `ExpressionSimplifier`
- `Simplify<T>(Expression<Func<T, bool>> expression)`: Constant folding and boolean identity optimization (`A && true` → `A`, `!(!A)` → `A`).

### `SpecificationDiagnostics`
- `ActivitySource ActivitySource`: Distributed tracing instrumentation source.
- `Meter Meter`: OpenTelemetry metrics source (name: `"EricksonLopez.Specification"`).
- `Counter<long> SpecificationsCreated` — meter name: `specification.created`
- `Counter<long> SpecificationsEvaluated` — meter name: `specification.evaluated`
- `Counter<long> SpecificationsComposed` — meter name: `specification.composed`
- `Counter<long> SpecificationsCompiled` — meter name: `specification.compiled`
- `Counter<long> ExpressionCacheHits` — meter name: `specification.expression.cache.hits`
- `Counter<long> ExpressionCacheMisses` — meter name: `specification.expression.cache.misses`
- `Counter<long> SqlTranslations` — meter name: `specification.sql.translations`
- `Histogram<double> SqlTranslationDuration` — meter name: `specification.sql.translation.duration` (unit: `ms`)

---

## MongoDB Integrations

Namespace: `EricksonLopez.Specification.MongoDB`  
Assembly: `EricksonLopez.Specification.MongoDB.dll`

- `MongoSpecificationEvaluator.GetFilter<TDocument>(QuerySpec<TDocument> spec)`: Compiles to `FilterDefinition<TDocument>`.
- `MongoSpecificationEvaluator.GetSort<TDocument>(QuerySpec<TDocument> spec)`: Compiles to `SortDefinition<TDocument>`.
- `IMongoCollection<TDocument>.Find(QuerySpec<TDocument> spec)`: Executes fluent find query with filters, sorting, and pagination.

---

## Dapper Integrations

Namespace: `EricksonLopez.Specification.Dapper`  
Assembly: `EricksonLopez.Specification.Dapper.dll`

Extension methods on `System.Data.IDbConnection`:
- `QueryAsync<T>(this IDbConnection cnn, QuerySpec<T> spec, ISqlDialect dialect, ...)`
- `QueryFirstOrDefaultAsync<T>(this IDbConnection cnn, QuerySpec<T> spec, ISqlDialect dialect, ...)`
- `CountAsync<T>(this IDbConnection cnn, QuerySpec<T> spec, ISqlDialect dialect, ...)`
- `AnyAsync<T>(this IDbConnection cnn, QuerySpec<T> spec, ISqlDialect dialect, ...)`
