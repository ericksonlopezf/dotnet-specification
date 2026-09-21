# adr-013: No Raw SQL / WhereRaw()

## Status
Accepted

## Date
2026-08-13

**Status**: Accepted  
**Date**: 2026-08-13  
**Deciders**: Erickson Lopez

---

## Context

Several ORM and query-builder libraries expose a `WhereRaw(string sql)` or similar API that
allows consumers to inject arbitrary SQL fragments directly into queries. During the design of
`QuerySpec<T>` and `QuerySpecTranslator<T>`, this was considered as a convenience escape hatch
for predicates that are difficult to express via expression trees.

## Problem

Specification consumers sometimes encounter expressions that the `ExpressionInterpreter` or
the dialect renderers cannot yet translate (e.g., database-specific functions, full-text search
operators, range predicates). The temptation is to add a `WhereRaw(string sql)` API to
`QuerySpec<T>` or `ISqlDialect` so consumers can bypass translation entirely.

## Options Considered

### Option A — Add `WhereRaw(string sql, object? parameters = null)` to `QuerySpec<T>`
Pros: quick workaround for untranslatable predicates.  
Cons: see "Decision" rationale below.

### Option B — Extend `ExpressionInterpreter` and dialects to cover more nodes
Pros: correct, composable, type-safe.  
Cons: more work per node type.

### Option C — Rejected; consumers fall back to Dapper's native parameter API
Pros: clean separation; Dapper already handles raw SQL well.  
Cons: consumer bypasses specification abstraction for those queries.

## Decision

**Raw SQL (`WhereRaw`, `FromRaw`, or any `string sql` escape hatch) will never be added to
this library.**

## Why

1. **Type unsafe**: Raw SQL strings are unverifiable at compile time. Any typo, column rename,
   or dialect change silently produces broken queries at runtime.

2. **SQL injection surface**: Any string interpolation path — even with parameterization — is
   an invitation for injection bugs. The library's value proposition is _type-safe, composable
   predicates_; adding raw SQL undermines it fundamentally.

3. **Breaks composability**: A `WhereRaw` fragment cannot be combined with `And()` / `Or()`
   at the expression-tree level. It is opaque to `ExpressionInterpreter`, to analyzers
   (SPEC003, SPEC007), and to future tooling like SQL plan cache (LATER-02).

4. **Breaks provider neutrality**: A raw SQL fragment is dialect-specific by nature. Adding
   it requires either per-dialect raw strings (leaking ORM details into domain specs) or
   accepting that the fragment only works on one database.

5. **Existing alternatives are sufficient**:
   - Use `Dapper` directly for queries that need raw SQL.
   - Extend the `ExpressionInterpreter` and/or the dialect to support the needed node type.
   - SPEC007 (`PotentialClientSideEvaluation`) warns consumers when an expression cannot be
     translated, preventing silent failures.

## Consequences

- Consumers who need database-specific predicates not yet supported must either:
  a) Open an issue/PR to extend expression support, or
  b) Use Dapper's native API for that specific query (full bypass — not mixed).
- The library remains free of SQL injection attack surface.
- `QuerySpec<T>` remains fully composable and dialect-agnostic.

## Rejected Alternatives

- `WhereRaw(string)` — rejected; type-unsafe, injection risk, breaks composability.
- `AppendSql(FormattableString)` — rejected; same problems, only marginally safer.

## Reconsideration Criteria

Only reconsider if a mechanism exists to statically verify the SQL fragment for type safety
and dialect compatibility at compile time — equivalent to what Roslyn's interpolated string
handlers achieve for string validation. No such mechanism exists today in .NET.
