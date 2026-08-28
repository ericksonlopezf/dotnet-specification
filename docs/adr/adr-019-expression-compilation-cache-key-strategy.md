# adr-019: ExpressionCompilationCache Must Use Structural Equality, Not Hash Alone

**Status**: Accepted  
**Date**: 2026-08-14  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: Core Engine / Correctness / Performance

---

## Context

`ExpressionCompilationCache` caches compiled delegates keyed by the integer hash of the expression:

```csharp
private static readonly ConcurrentDictionary<int, Delegate> Cache = new();

internal static Func<T, bool> GetOrCompile<T>(Expression<Func<T, bool>> expression)
{
    var hash = ExpressionHasher.ComputeHash(expression);
    if (Cache.TryGetValue(hash, out var cached))
        return (Func<T, bool>)cached;

    var compiled = expression.Compile();
    Cache.TryAdd(hash, compiled);
    return (Func<T, bool>)compiled;
}
```

The code comment says: *"In the rare case of a hash collision, the second expression overwrites the first (acceptable since compilations are idempotent)."*

---

## Problem

The comment is incorrect. If two semantically different expressions produce the same hash (a hash collision), the cache will return the compiled delegate for expression A when expression B is requested. **The delegate executes the wrong logic in silence.**

"Idempotent compilations" means compiling the same expression twice produces the same delegate — this is true. But it does not mean compiling a *different* expression is equivalent. A collision between `c => c.IsActive` and `c => c.CreditLimit > 1000m` would cause `ToCompiledPredicate()` to return the wrong predicate for one of them.

This is a **correctness bug**, not a performance concern.

---

## Options Considered

### Option A — Use ExpressionEqualityComparer as the dictionary key comparer

```csharp
private static readonly ConcurrentDictionary<Expression, Delegate> Cache 
    = new(ExpressionEqualityComparer.Default);
```

This makes the key an `Expression` object, compared structurally using the existing `ExpressionEqualityComparer`. Hash collisions become impossible because the key comparison is by structural equality, not hash alone.

### Option B — Keep hash as primary key, add secondary equality check

```csharp
if (Cache.TryGetValue(hash, out var entry))
{
    if (ExpressionEqualityComparer.Default.Equals(entry.Expression, expression))
        return entry.Delegate;
    // collision — recompile
}
```

This adds overhead only on collision, preserving the fast-path performance.

### Option C — Accept the bug as "acceptable in practice"

Document the limitation and argue hash collisions are rare enough to ignore.

---

## Decision

**Accepted: Option A — Use `ExpressionEqualityComparer.Default` as the `IEqualityComparer<TKey>` of the `ConcurrentDictionary`.**

```csharp
private static readonly ConcurrentDictionary<Expression, Delegate> Cache 
    = new(ExpressionEqualityComparer.Default);
```

This is consistent with how `QueryPlanCache` works (which already uses `ExpressionEqualityComparer.Default` correctly via the `CacheKey` struct).

---

## Decision Drivers

- **Correctness > Performance**: A cache that returns wrong results is worse than no cache.
- **Existing infrastructure**: `ExpressionEqualityComparer` already exists and is correct. Using it costs nothing new.
- **Consistency**: `QueryPlanCache` already uses structural equality. The compilation cache should be consistent.
- **The "rare" argument fails at scale**: Applications with many dynamically composed specifications will have more cache entries and thus more collision risk.

---

## Consequences

### Positive

- The compilation cache is provably correct.
- No semantic errors from hash collisions.
- Consistent with `QueryPlanCache` design.

### Negative

- Slight overhead increase on cache lookups due to structural equality check (vs. integer comparison). This is negligible compared to the cost of `expression.Compile()` itself.
- `Expression` keys are reference types; the dictionary now holds references to expression trees (memory impact is minimal, as expressions are already in memory).

---

## Why Alternatives Were Rejected

**Option B**: More complex, adds a special case for an error path. Option A is simpler and provably correct.

**Option C**: Accepting known incorrect behavior in a publicly-released library is not acceptable, regardless of the statistical probability of the error occurring.

---

## Relationship With Project Philosophy

The project's primary goal includes "Correctness" as the first-listed priority. A caching layer that silently produces incorrect results violates the foundational correctness requirement.

---

## Reconsideration Criteria

If profiling evidence shows that `ExpressionEqualityComparer` is a bottleneck in the compilation cache hot path, a two-level cache (hash → bucket → structural equality) can be implemented. This optimization is premature without benchmark evidence.
