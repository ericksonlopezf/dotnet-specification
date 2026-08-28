# adr-025: ExpressionSimplifier Integration in CompositeSpecification

## Status

Accepted

## Date

2026-08-14

## Context

`ExpressionSimplifier` exists and correctly simplifies constant boolean expressions:
- `(true) AND A` → `A`
- `A AND (true)` → `A`
- `(false) OR A` → `A`
- `NOT(NOT(A))` → `A`

However, it is NOT called automatically in the composition pipeline. `Spec.True<T>().And(realSpec)` produces a composite expression with a constant `true` as the left operand — this is semantically correct but unnecessarily verbose for the expression engine, the interpreter, and the SQL translator.

## Problem

```csharp
// Developer writes a fold pattern:
Specification<Customer> combined = Spec.True<Customer>();
foreach (var filter in activeFilters)
    combined = combined.And(filter);

// The resulting expression tree is:
// (true AND filter1) AND filter2 → not simplified
// Should be: filter1 AND filter2
```

The SQL translator and interpreter handle this correctly, but each `true` constant generates unnecessary processing.

## Options Considered

### Option A: Always simplify in BuildExpression — Rejected

Applying `ExpressionSimplifier.Simplify()` on every composition adds an `ExpressionVisitor` traversal on every `.And()` / `.Or()`. For hot paths composing 100+ specifications, this overhead is measurable.

### Option B: Simplify only when a constant is detected — Accepted

Before calling `ExpressionSimplifier.Simplify()`, check if either operand's body is a `ConstantExpression`. If neither operand is constant, skip simplification entirely — zero overhead for the common case.

### Option C: Do not integrate (status quo)

- Simplification remains opt-in via explicit `ExpressionSimplifier.Simplify()` calls
- The fold pattern with `Spec.True<T>()` produces sub-optimal trees

## Decision

Implement Option B in `CompositeSpecification.BuildExpression()`:

```csharp
if (leftExpr.Body is ConstantExpression || rightExpr.Body is ConstantExpression)
    return ExpressionSimplifier.Simplify(composed);
return composed;
```

## Decision Drivers

- **Correctness**: Does not change semantics — simplification is provably safe for boolean algebra identities
- **Performance**: Zero overhead for the common case (no constants)
- **DDD**: Transparent to consumers
- **AOT**: `ExpressionVisitor`-based simplifier is fully AOT-safe

## Consequences

### Positive

- `Spec.True<T>().And(spec)` → produces a clean expression equivalent to `spec.ToExpression()`
- Fold patterns using `Spec.True<T>()` as accumulator identity element now produce optimal trees
- SQL translator generates cleaner WHERE clauses

### Negative

- Slight additional cost when one operand IS a constant (one ExpressionVisitor traversal)
- `CompositeSpecification` now has a dependency on `ExpressionSimplifier` (both are in the same assembly — acceptable)

## Why Alternatives Were Rejected

- Option A was rejected because the overhead on every composition is unjustifiable
- Option C was rejected because the fold pattern with True<T>() is a natural and common pattern that should work optimally without explicit simplification calls

## Relationship With Project Philosophy

Boolean algebra identity simplification (A AND TRUE = A) is a semantic correctness improvement, not feature creep. It makes the engine behave predictably for the expected developer patterns.

## Reconsideration Criteria

If profiling shows measurable overhead from the ConstantExpression check itself (unlikely — it's a single `is` pattern match).
