# adr-015: No GroupBy / Aggregation / SelectMany in Specifications

## Status
Accepted

## Date
2026-08-13

**Status**: Accepted  
**Date**: 2026-08-13  
**Deciders**: Erickson Lopez

---

## Context

`Specification<T>` and `QuerySpec<T>` are designed to express _filtering, ordering, and
pagination_ over a homogeneous set of `T` entities. During API design, the following
operations were considered for inclusion:

- `GroupBy(expr)` — group entities by a key
- Aggregation methods: `Count()`, `Sum()`, `Average()`, `Max()`, `Min()` in-spec
- `SelectMany(expr)` — flatten collections (join/expand)
- Projection: `Select<TResult>(expr)` — transform the shape of results

## Problem

These operations change the _output shape_ of a query. A `GroupBy` turns `IEnumerable<T>`
into `IEnumerable<IGrouping<TKey, T>>`. A `SelectMany` turns `IEnumerable<T>` into
`IEnumerable<TElement>`. These are fundamentally different concerns from filtering a list
of `T` entities.

## Options Considered

### Option A — Add `GroupBy<TKey>(Expression<Func<T, TKey>>)` to `QuerySpec<T>`
Pros: richer query capabilities in one API.  
Cons: changes return type; requires `QuerySpec<T, TKey>` variant; complicates SQL rendering.

### Option B — Add aggregation results to `QuerySpec<T>` (Count, Sum in-spec)
Pros: single trip aggregation.  
Cons: aggregation results are scalar, not `IEnumerable<T>`. API becomes incoherent.

### Option C — Add `SelectMany<TResult>` for navigation property expansion
Pros: JOIN-like behavior without raw SQL.  
Cons: changes result shape; requires SQL JOIN generation; cross-entity concern.

### Option D — Keep `QuerySpec<T>` as a pure filter/order/page descriptor
Pros: clean, composable, AOT-compatible, single-concern API.  
Cons: consumers must implement grouping/aggregation manually.

## Decision

**GroupBy, aggregations (Sum, Average, etc.), and SelectMany will never be part of
`Specification<T>` or `QuerySpec<T>`.**

`QuerySpec<T>` is, and will remain, a _filter + order + pagination_ descriptor that returns
`IEnumerable<T>`. Any operation that changes the output shape is a different concern and
belongs in the repository implementation or in a purpose-built query object.

## Why

1. **Single Responsibility**: A specification answers "which T entities match?" Group-by
   answers "how are the T entities distributed?" These are different questions with different
   return types. Mixing them in one type breaks the principle and complicates the API surface.

2. **SQL complexity explosion**: Supporting GROUP BY requires generating `SELECT ... GROUP BY ...
   HAVING ...` in SQL dialects, tracking aggregate projections, and resolving TResult types.
   This doubles the SQL rendering complexity for a small minority of use cases.

3. **AOT / expression tree incompatibility**: `SelectMany` and `GroupBy` use lambda overloads
   with multiple generic type parameters. The SQL generation path (`QuerySpecTranslator<T>`)
   would need to handle result-type variance, making AOT annotation significantly harder.

4. **Wrong layer for aggregation**: Aggregation (COUNT, SUM) belongs in a dedicated read-model
   query or in a repository method (`CountAsync(spec)`, `SumAsync(spec, selector)`), not
   inside the specification that describes _which entities_ to work with.

5. **SelectMany = JOINs**: `SelectMany` semantically maps to SQL JOINs or lateral queries.
   JOIN generation requires knowledge of relationships, foreign keys, and join conditions —
   territory that belongs in the ORM or in a purpose-built SQL builder (e.g., SqlBuilder).

## Consequences

- Consumers who need aggregations implement dedicated repository methods:
  `int CountAsync(QuerySpec<T> spec)`, `decimal SumAsync(QuerySpec<T> spec, Expression<Func<T, decimal>> selector)`
- Consumers who need JOINs or cross-entity queries use raw Dapper, EF Core LINQ, or
  EricksonLopez.SqlBuilder directly.
- `QuerySpec<T>` remains type-stable: input is always `IEnumerable<T>`, output is always `IEnumerable<T>`.

## Rejected Alternatives

- `GroupBy<TKey>` on `QuerySpec<T>` — rejected; changes output type, breaks composability.
- `SelectMany<TResult>` — rejected; requires JOIN semantics beyond the spec's scope.
- In-spec aggregation methods — rejected; scalar results are incompatible with the `T` result shape.
- `QuerySpec<T, TResult>` with projection — partially accepted for `QuerySpec<T, TResult>` (v1.0),
  but GroupBy/aggregation remain excluded even in that variant.

## Reconsideration Criteria

Reconsider only if a compelling, type-safe pattern emerges for expressing aggregation as a
composable, AOT-compatible specification that does not change the semantics of the result type.
No current .NET feature enables this.
