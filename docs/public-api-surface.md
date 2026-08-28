# Public API Surface Reference

This document provides a comprehensive technical inventory of the public API surface for all packages in `EricksonLopez.Specification`.

---

## 1. `EricksonLopez.Specification.Abstractions`

### `ISpecification<T>`
- **Contract**: Minimal marker and predicate interface for domain specifications.
- **Signatures**:
  - `Expression<Func<T, bool>> ToExpression();`
  - `bool IsSatisfiedBy(T entity);`

### `QuerySpec<T>` and `QuerySpec<T, TResult>`
- **Contract**: Immutable record representing a full query intent (filters, ordering, pagination, cursor).
- **Core Factory**: `QuerySpec<T>.Empty`
- **Builder Methods**:
  - `QuerySpec<T> Where(Expression<Func<T, bool>> predicate)` — adds an AND filter predicate
  - `QuerySpec<T> TagWith(string tag)` — attaches a diagnostic query tag
  - `QuerySpec<T> Search(string searchPhrase, params Expression<Func<T, string?>>[] propertySelectors)` — case-sensitive OR full-text search across string properties
  - `QuerySpec<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)`
  - `QuerySpec<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)`
  - `QuerySpec<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)` — alias for `OrderBy`
  - `QuerySpec<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)` — alias for `OrderByDescending`
  - `QuerySpec<T> Page(int page, int pageSize)` — 1-based offset pagination (calculates `Skip = (page-1)*pageSize`)
  - `QuerySpec<T> Take(int count)`
  - `QuerySpec<T> Skip(int count)`
  - `QuerySpec<T> Distinct()`
  - `QuerySpec<T> SeekAfter<TKey>(Expression<Func<T, TKey>> keySelector, TKey cursorValue, int take)` — keyset pagination forward
  - `QuerySpec<T> SeekBefore<TKey>(Expression<Func<T, TKey>> keySelector, TKey cursorValue, int take)` — keyset pagination backward
  - `QuerySpec<T> WithCursor<TKey>(Expression<Func<T, TKey>> keySelector, TKey cursorValue, CursorDirection direction, int take)` — generic cursor pagination
  - `QuerySpec<T, TResult> Select<TResult>(Expression<Func<T, TResult>> selector)` — projects to typed result (available on `QuerySpec<T, TResult>`)
- **Extension Methods** (via `QuerySpecExtensions`, requires `using EricksonLopez.Specification`):
  - `QuerySpec<T> Where<T>(this QuerySpec<T> querySpec, IExpressionSpecification<T> specification)` — adds a domain specification as AND filter (accepts `Specification<T>` and `Spec.For<T>()`)
  - `QuerySpec<T> And<T>(this QuerySpec<T> querySpec, Specification<T> specification)` — combines with a domain specification
  - `Expression<Func<T, bool>>? BuildCombinedPredicate<T>(this QuerySpec<T> querySpec)` — returns single AND-composed predicate
  - `bool HasOrdering<T>(this QuerySpec<T> querySpec)` — whether any ordering clause is defined
  - `bool HasPagination<T>(this QuerySpec<T> querySpec)` — whether Skip or Take is defined
  - `bool HasCriteria<T>(this QuerySpec<T> querySpec)` — whether any filter predicate is defined

### `IReadRepository<T>`
- **Contract**: Pure asynchronous read repository abstraction.
- **Methods**:
  - `Task<IReadOnlyList<T>> ListAsync(QuerySpec<T> specification, CancellationToken ct = default);`
  - `Task<IReadOnlyList<TResult>> ListAsync<TResult>(QuerySpec<T, TResult> specification, CancellationToken ct = default);`
  - `Task<T?> FirstOrDefaultAsync(QuerySpec<T> specification, CancellationToken ct = default);`
  - `Task<T?> SingleOrDefaultAsync(QuerySpec<T> specification, CancellationToken ct = default);`
  - `Task<int> CountAsync(QuerySpec<T> specification, CancellationToken ct = default);`
  - `Task<bool> AnyAsync(QuerySpec<T> specification, CancellationToken ct = default);`

---

## 2. `EricksonLopez.Specification` (Core Engine)

### `Specification<T>`
- **Base Class**: Abstract base for all domain specifications.
- **Required Override**: `protected abstract Expression<Func<T, bool>> BuildExpression();`
- **Methods**:
  - `bool IsSatisfiedBy(T candidate)` — in-memory evaluation via `ExpressionInterpreter` (AOT-safe)
  - `Expression<Func<T, bool>> ToExpression()` — returns the underlying lambda
  - `Func<T, bool> ToCompiledPredicate()` — JIT-only compiled delegate (`[RequiresDynamicCode]`)
  - `string ToDebugString()` — AOT-safe string representation of the predicate tree
  - `Specification<T> And(Specification<T> other)` — logical AND composition
  - `Specification<T> Or(Specification<T> other)` — logical OR composition
  - `Specification<T> Not()` — logical NOT negation

### `Spec` (Static Factory & Combinators)
- `Spec.For<T>(Expression<Func<T, bool>> predicate)` — wraps an inline lambda as a specification
- `Spec.True<T>()` — always-true specification (`_ => true`)
- `Spec.False<T>()` — always-false specification (`_ => false`)
- `Spec.All<T>(params Specification<T>[] specifications)` — combines all specs with AND logic (span-optimized)
- `Spec.Any<T>(params Specification<T>[] specifications)` — combines all specs with OR logic (span-optimized)
- `Spec.Between<T, TProperty>(Expression<Func<T, TProperty>> property, TProperty lower, TProperty upper)` — between range predicate
- `Spec.FullText<T>(Expression<Func<T, string>> property, string searchTerm)` — full-text search predicate (case-sensitive `string.Contains`)
- `Spec.InRange<T, TValue>(Expression<Func<T, TValue>> property, TValue lower, TValue upper)` — range containment predicate (inclusive)

### `ExpressionComposer`
- **Purpose**: Invoke-free expression composition via `ParameterReplacer`.
- **Methods**: `And<T>()`, `Or<T>()`, `Not<T>()`, `AndAll<T>(ReadOnlySpan<...>)`, `OrAny<T>(ReadOnlySpan<...>)`.

### `ExpressionSimplifier`
- **Purpose**: Constant folding and boolean identity simplification (e.g., `true && A` -> `A`, `!(!A)` -> `A`).
- **Signature**: `public static Expression<Func<T, bool>> Simplify<T>(Expression<Func<T, bool>> expression)`

### `ExpressionHasher` & `ExpressionEqualityComparer`
- **Purpose**: Deep structural AST equality and hashing ignoring parameter names.
- **Signatures**: `ExpressionHasher.ComputeHash(Expression expr)`, `ExpressionEqualityComparer.Default`.

### `ExpressionInterpreter`
- **Purpose**: Native AOT-safe in-memory expression tree evaluator without dynamic IL emit.
- **Signature**: `public static bool Evaluate<T>(Expression<Func<T, bool>> expression, T candidate)`

---

## 3. `EricksonLopez.Specification.Linq`

### `QuerySpecLinqExtensions`
- `IQueryable<T> Apply<T>(this IQueryable<T> source, QuerySpec<T> spec)` — applies criteria, ordering, and pagination
- `IQueryable<TResult> Apply<T, TResult>(this IQueryable<T> source, QuerySpec<T, TResult> spec)` — applies query with projection
- `bool Any<T>(this IQueryable<T> source, IExpressionSpecification<T> specification)` — any matching element (synchronous, for IQueryable)
- `int Count<T>(this IQueryable<T> source, IExpressionSpecification<T> specification)` — count matching elements (synchronous, for IQueryable)

---

## 4. `EricksonLopez.Specification.Sql`

### `QuerySpecTranslator<T>`
- `QueryModel Translate(QuerySpec<T> querySpec)` — parses `QuerySpec<T>` into a provider-agnostic SQL AST (`QueryModel`).

### `QueryPlanCache`
- Thread-safe, bounded LRU query plan cache (default capacity: 512 entries).
- Property: `public static int Capacity { get; set; }` — configures maximum number of cached plans (default: 512; minimum: 1)
- Property: `public static int Count { get; }` — current number of cached plans
- Method: `bool TryGetPlan(Expression criteria, string tableName, out QueryModel plan)` — attempts to retrieve a cached plan
- Method: `void SetPlan(Expression criteria, string tableName, QueryModel plan)` — stores a translated plan in the cache
- Method: `void Clear()` — removes all cached plans

### Column Resolvers
- `IColumnNameResolver` — interface mapping property names to database columns.
- `SnakeCaseColumnNameResolver` — converts `CreatedAt` to `created_at`.
- `VerbatimColumnNameResolver` — retains original property names.

---

## 5. SQL Dialect Providers

All dialects implement `ISqlDialect` and provide `Default` singleton instances:

| Dialect Class | Assembly | SQL Parameter Syntax | Identifier Quoting | Pagination Syntax |
|---|---|:---:|:---:|:---:|
| `PostgreSqlDialect.Default` | `PostgreSql` | `$1, $2, ...` | `"column"` | `LIMIT n OFFSET m` |
| `MsSqlDialect.Default` | `MsSql` | `@p0, @p1, ...` | `[column]` | `OFFSET n ROWS FETCH NEXT m ROWS ONLY` |
| `MySqlDialect.Default` | `MySql` | `@p0, @p1, ...` | ``` `column` ``` | `LIMIT offset, limit` |
| `MariaDbDialect.Default` | `MariaDb` | `@p0, @p1, ...` | ``` `column` ``` | `LIMIT offset, limit` |
| `SqliteDialect.Default` | `Sqlite` | `@p0, @p1, ...` | `"column"` | `LIMIT n OFFSET m` |
| `OracleDialect.Default` | `Oracle` | `:p0, :p1, ...` | `"COLUMN"` | `OFFSET n ROWS FETCH NEXT m ROWS ONLY` |

---

## 6. Micro-ORM & Database Integrations

### `EricksonLopez.Specification.Dapper`
- `QueryAsync<T>(this IDbConnection, QuerySpec<T>, ISqlDialect, IColumnNameResolver, string tableName, CancellationToken)`
- `QueryFirstOrDefaultAsync<T>(...)`
- `ExecuteScalarAsync<T>(...)`

### `EricksonLopez.Specification.EntityFrameworkCore`
- `SpecificationEvaluator` — applies `QuerySpec<T>` onto `IQueryable<T>` DbSets.
- `EfRepository<T>` / `EfReadRepository<T>` — concrete `IReadRepository<T>` implementations over `DbContext`.

### `EricksonLopez.Specification.MongoDB`
- `MongoFilterCompiler` — compiles `ISpecification<T>` into MongoDB `FilterDefinition<T>`.
- `MongoSortCompiler` — compiles ordering clauses into `SortDefinition<T>`.

### `EricksonLopez.Specification.DapperExtensions`
- `ReadRepositoryDapperExtensions` — executes specifications over internal `IUnitOfWork` sessions.

### `EricksonLopez.Specification.Result`
- `ListResultAsync<T>()`, `FirstOrDefaultResultAsync<T>()` — executes specifications returning functional `Result<T>` envelopes.

---

## 7. `EricksonLopez.Specification.Analyzers` (Diagnostics)

| Rule ID | Severity | Description | Fix Provider |
|---|---|---|---|
| `SPEC001` | Warning | Concrete specifications must be `sealed` | `SpecificationSealedCodeFixProvider` |
| `SPEC002` | Warning | Specifications must not contain mutable state or public setters | N/A |
| `SPEC003` | Error | `Expression.Invoke` prohibited in specifications | N/A |
| `SPEC004` | Info | Queries without pagination flag unbounded table scans | N/A |
| `SPEC005` | Info | `OrderBy` applied without pagination limits | N/A |
| `SPEC006` | Info | Domain specifications declared in Infrastructure layer | N/A |
| `SPEC007` | Warning | Non-translatable methods inside `BuildExpression` | N/A |
| `SPEC008` | Error | Infrastructure service injection in specification constructor | N/A |
| `SPEC009` | Error | Async lambda expressions inside `BuildExpression` | N/A |
| `SPEC010` | Error | Direct `IsSatisfiedBy` invocation inside `BuildExpression` | N/A |
| `SPEC011` | Warning | Inheritance from legacy `Ardalis.Specification` detected | `ArdalisMigrationCodeFixProvider` |
