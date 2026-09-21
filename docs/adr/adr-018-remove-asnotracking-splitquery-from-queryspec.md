# adr-018: Remove AsNoTracking and AsSplitQuery from QuerySpec<T>

## Status
Accepted

## Date
2026-08-14

**Status**: Accepted  
**Date**: 2026-08-14  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: Core Architecture / DDD / Clean Architecture

---

## Context

`QuerySpec<T>` currently exposes two properties:

```csharp
public bool AsNoTracking { get; init; }
public bool AsSplitQuery { get; init; }
```

These were added to allow consumers to express EF Core query optimization hints within the
specification descriptor.

---

## Problem

`QuerySpec<T>` is described as a "provider-agnostic, immutable query descriptor." However:

1. `AsNoTracking` is an EF Core concept. Its meaning for Dapper, SQLite raw queries, or in-memory LINQ is undefined.
2. `AsSplitQuery` is **exclusively** EF Core. No other provider has an equivalent concept.
3. Neither property is consumed by `QuerySpecTranslator<T>` (the SQL adapter).
4. Neither property is applied by `QuerySpecLinqExtensions.Apply()` (the LINQ adapter).
5. Both properties are dead code in all current adapters.

This creates a **false abstraction**: the descriptor claims to be generic but contains EF Core-specific concerns that no provider respects.

---

## Options Considered

### Option A — Remove from QuerySpec<T> (breaking change)

Delete both properties. Document that EF Core hints must be applied at the call site:

```csharp
dbContext.Set<Customer>().AsNoTracking().Apply(querySpec).ToListAsync()
```

### Option B — Move to EF Core adapter extension

Keep the concept but in a EF Core-specific extension type:

```csharp
public static class QuerySpecEfCoreExtensions
{
    public static IQueryable<T> Apply<T>(
        this IQueryable<T> source,
        QuerySpec<T> spec,
        bool asNoTracking = false,
        bool asSplitQuery = false) { ... }
}
```

### Option C — Keep as optional hints documented as EF-Core-only

Document that these properties are EF Core-only hints and are ignored by other adapters.

---

## Decision

**Accepted: Option A — Remove from `QuerySpec<T>`.**

The properties will be removed from the record definition. The migration path for EF Core consumers is to apply `AsNoTracking()` directly on the `IQueryable<T>` before calling `Apply()`.

---

## Decision Drivers

- **DDD purity**: `QuerySpec<T>` is an Application-layer data descriptor. Persistence optimization hints (tracking, split queries) are Infrastructure concerns that should not leak upward.
- **Minimal API surface**: Dead properties that no adapter reads are confusion, not value.
- **Honesty**: A "provider-agnostic" type that contains EF Core-only properties is architecturally dishonest.
- **Principle of least surprise**: Dapper consumers setting `AsNoTracking(true)` and seeing no effect is a confusing developer experience.

---

## Consequences

### Positive

- `QuerySpec<T>` is genuinely provider-agnostic.
- No dead properties in the public API.
- EF Core consumers get explicit, visible control over tracking behavior at the call site.
- Cleaner migration to non-EF-Core providers (Dapper, SQLite).

### Negative

- Breaking change for consumers who use `AsNoTracking()` or `SplitQuery()` on `QuerySpec<T>`.
- Requires updating the migration guide.

---

## Why Alternatives Were Rejected

**Option B**: Adds complexity without solving the core problem. The descriptor still doesn't contain these hints, but now there's an overloaded `Apply()` — two different signatures with different semantics.

**Option C**: Documents the leaking abstraction as intentional. This is the worst option — it codifies architectural incorrectness.

---

## Migration Strategy

For EF Core consumers:

```csharp
// Before
var spec = QuerySpec<Customer>.Empty
    .Where(new ActiveCustomerSpec().ToExpression())
    .AsNoTracking();

await dbContext.Customers.Apply(spec).ToListAsync();

// After
var spec = QuerySpec<Customer>.Empty
    .And(new ActiveCustomerSpec());  // .And() is the extension method accepting Specification<T>

await dbContext.Customers.AsNoTracking().Apply(spec).ToListAsync();
```

The after pattern is arguably cleaner: the EF Core concern is expressed in EF Core's API, not in the domain query descriptor.

---

## Relationship With Project Philosophy

The project philosophy states: "Specification is a domain concept for expressing reusable, composable business predicates and selection intent without coupling the Domain to persistence infrastructure."

Persistence tracking and query splitting are persistence infrastructure concerns. They do not belong in the query intent descriptor.

---

## Reconsideration Criteria

If a future .NET provider ecosystem develops a universal concept equivalent to `AsNoTracking` (i.e., not just an EF Core concept), this decision can be revisited with a properly designed abstraction. Until then, the decision stands.
