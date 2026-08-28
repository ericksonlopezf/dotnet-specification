# adr-022: Spec.All / Spec.Any Static Combinators

## Status

Accepted

## Date

2026-08-14

## Context

`ExpressionComposer.AndAll(ReadOnlySpan<Expression<Func<T,bool>>>)` and `OrAny` already exist for composing multiple expression trees into a single expression. However, `Specification<T>` objects can only be composed pairwise via `.And()` / `.Or()`. When a developer wants to compose 3 or more specifications, they must chain: `a.And(b).And(c)`.

## Problem

```csharp
// Current (awkward for 3+ specs)
var spec = new ActiveSpec().And(new PremiumSpec()).And(new HasCreditSpec());

// Desired (natural for collections)
var spec = Spec.All(new ActiveSpec(), new PremiumSpec(), new HasCreditSpec());
```

Without `Spec.All/Any`, composing a runtime-determined list of specifications (e.g., from configuration or policy rules) requires manual accumulation via fold, which is error-prone.

## Options Considered

### Option A: `Spec.All(params Specification<T>[])` — Accepted

- Converts specifications to expressions via `ToExpression()`
- Delegates to `ExpressionComposer.AndAll(ReadOnlySpan<...>)` for minimal allocations
- Returns `Spec.True<T>()` for empty (neutral AND element)
- Returns single spec unchanged for length == 1

### Option B: Extension methods on `IEnumerable<Specification<T>>`

- Flexible but harder to discover
- No `params` syntax, requires explicit collection

### Option C: Do not implement (reject)

- Forces awkward chaining for multi-spec scenarios

## Decision

Implement Option A: `Spec.All<T>(params Specification<T>[])` and `Spec.Any<T>(params Specification<T>[])` as static factory methods on the `Spec` class.

## Decision Drivers

- **DDD**: Pure composition — no new responsibilities
- **Performance**: Delegates to existing `ExpressionComposer.AndAll(Span<>)` — zero additional allocations vs manual chaining
- **API simplicity**: `params` syntax is ergonomic and discoverable
- **AOT**: Fully AOT-safe — uses the same expression tree manipulation as `.And()`
- **Type safety**: Fully generic, no reflection

## Consequences

### Positive

- Natural, ergonomic API for composing 3+ specifications
- Works with runtime-determined specification collections
- Zero allocation overhead for the common case

### Negative

- `params` allocates a temporary array when called with explicit arguments (not spread)
- For performance-critical scenarios with large collections, `ReadOnlySpan<Expression<...>>` via `ExpressionComposer` directly is still preferred

## Why Alternatives Were Rejected

- Option B was rejected because `params` is more discoverable and ergonomic for the common case
- Option C was rejected because the ergonomic gap is real and the implementation cost is trivial

## Relationship With Project Philosophy

This is pure predicate composition — the core value of the library. It does not add responsibilities; it adds ergonomics.

## Reconsideration Criteria

If a strong case is made for `IEnumerable<Specification<T>>` overloads for LINQ compatibility.
