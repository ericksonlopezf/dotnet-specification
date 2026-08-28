# adr-024: ConvertKeySelector Boxing Strategy for Ordering Clauses

## Status

Accepted

## Date

2026-08-14

## Context

In `QuerySpec<T>`, developers specify ordering using typed key selectors:
```csharp
public QuerySpec<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
```
Internally, ordering clauses must be stored in an immutable collection on `QuerySpec<T>`.

## Problem

How should the heterogeneous `Expression<Func<T, TKey>>` expressions be stored across multiple chained `OrderBy` / `ThenBy` calls without introducing excessive generic type parameters on `QuerySpec<T>`?

- Storing a generic `OrderClause<T, TKey>` would require `QuerySpec<T>` to be generic over all key types (e.g. `QuerySpec<T, TKey1, TKey2>`), which is unviable.
- Storing an untyped `OrderClause<T>` with `Expression<Func<T, object?>>` requires converting value-type keys via `Expression.Convert(body, typeof(object))`.

## Options Considered

### Option A: `ConvertKeySelector` Boxing via `Expression.Convert(body, typeof(object))` — Accepted

- When `OrderBy<TKey>` is called, it constructs an `Expression<Func<T, object?>>` by wrapping the body in `Expression.Convert`.
- The SQL translator unwraps the `Convert` node when extracting property and column names (`ExtractColumnNameFromOrderSelector`).
- The LINQ adapter removes the convert node or invokes the typed ordering methods dynamically/via standard expression visitors.

### Option B: Non-generic `LambdaExpression` in `OrderClause<T>`

- Storing raw `LambdaExpression` without boxing.
- Loses type check on `Func<T, ...>` signature, requiring runtime type assertions.

## Decision

Implement Option A: Use `ConvertKeySelector` to box to `Expression<Func<T, object?>>` in `OrderClause<T>`. Unbox/unwrap cleanly inside query translators.

## Decision Drivers

- **API Ergonomics**: Clean, fluent API `QuerySpec<T>.OrderBy<TKey>(c => c.CreatedAt)` without key type pollution on `QuerySpec<T>`.
- **Translator Simplicity**: Translators inspect the AST and unwrap `UnaryExpression(Convert)` in constant time.
- **Allocation Profile**: Zero boxing happens at query execution time when translated to SQL or LINQ; boxing only exists as an AST representation node.

## Consequences

### Positive

- `QuerySpec<T>` remains simple with a single generic parameter `T`.
- Heterogeneous orderings (`OrderBy(c => c.Name).ThenBy(c => c.Age)`) can be stored in a single `ImmutableArray<OrderClause<T>>`.

### Negative

- AST has a `Convert` node that translators must be aware of and unwrap.

## Reconsideration Criteria

If a future C# feature allows type-safe heterogeneous lists without boxing AST nodes.
