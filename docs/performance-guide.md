# Performance Guide

`EricksonLopez.Specification` was designed from the ground up to mitigate the classic bottlenecks of the Specification pattern (excessive memory allocations and expression tree recompilation).

> **Honesty principle**: Publish all numbers, including disadvantages. Trust > marketing.

## 1. Performance Philosophy

The library has THREE distinct performance tiers. Understanding which tier applies to your scenario is essential for correct expectations.

### Tier 1 — Maximum Throughput (JIT runtime, hot path)

Use: `spec.ToCompiledPredicate()` [RequiresDynamicCode]

```csharp
// One-time compilation cost, then cached by structural hash
[RequiresDynamicCode("JIT-only")]
var compiled = spec.ToCompiledPredicate();
var result = compiled(entity);  // native delegate -- near-zero overhead
```

Characteristics:
- First call: expression compilation cost (`ExpressionCompilationCache` lookup)
- Subsequent calls (same expression structure): O(1) dictionary lookup + native delegate
- Throughput: comparable to raw `Func<T,bool>` after cache hit
- Allocation: zero on cache hit
- Not available in NativeAOT

### Tier 2 — Default (AOT-compatible)

Use: `spec.IsSatisfiedBy(entity)`

```csharp
var result = spec.IsSatisfiedBy(entity);  // ExpressionInterpreter
```

Characteristics:
- No compilation -- interpreted tree walk
- **5-20x slower than Tier 1** -- this is a real, documented overhead
- Zero dynamic code -- works in NativeAOT
- Acceptable for: domain validation, business rule checking, low-frequency evaluation
- Not acceptable for: tight inner loops with millions of evaluations/second

### Tier 3 — SQL Translation Path

Use: Dapper integration via `QuerySpecTranslator<T>` + `ISqlDialect`

```csharp
var query = translator.Translate(querySpec);  // expression -> AST
var sql = dialect.Render(query.QueryModel);    // AST -> SQL string
```

Characteristics:
- Network-bound in practice (database RTT dominates)
- Translation overhead amortized vs database round-trip
- Bounded `QueryPlanCache` (LRU with 512 default capacity) eliminates translation overhead on hot Dapper paths

---

## 2. Architecture Optimizations

### AST Reuse and Hashing
When you define a `Specification<T>`, the library captures and evaluates the `BuildExpression()` method exactly once, storing it in a local instance cache. For extreme high-concurrency scenarios, the library exposes an `ExpressionHasher` that generates a structurally deterministic hash (ignores parameter names, and hashes types and constants).

### Allocation-Free `QuerySpec<T>`
All internal collections in `QuerySpec<T>` (`Criteria`, `OrderClauses`) use `ImmutableArray<T>`, which is a lightweight struct without extra heap allocation overhead compared to `ReadOnlyCollection` or `List`. For projected queries, `QuerySpec<T, TResult>` exposes a `Selector` expression (not a collection). When you chain consecutive `.Where()` calls, the GC efficiently collects the old arrays.

### Expression Simplification
The internal query compiler uses the `ExpressionSimplifier` so that if you compose complex rules that evaluate to `true && condition` or `false || condition`, the tree is reduced before being processed by your ORM (e.g., EF Core). This avoids generating suboptimal or unnecessarily long SQL execution plans in the database.

---

## 3. Allocation Targets

| Operation | Target Allocations | Notes |
|---|---|---|
| `Specification<T>` construction | 1 `Lazy<T>` | Per instance |
| `IsSatisfiedBy(entity)` (AOT/interpreted) | ~0 allocations | Stack-based interpreter walk |
| `IsSatisfiedBy(entity)` (JIT cached) | ~0 allocations | Native delegate call |
| `spec1.And(spec2)` | 1 `CompositeSpecification<T>` | New composition object |
| `ExpressionComposer.AndAll(span)` | 0 intermediate | ReadOnlySpan, no List |
| `ExpressionComposer.OrAny(span)` | 0 intermediate | ReadOnlySpan, no List |
| `QuerySpec<T>.Where(pred)` | 1 `ImmutableArray` copy | Copy-on-add semantics |
| `QuerySpec<T>.OrderBy(...)` | 1 `ImmutableArray` copy | |
| `ExpressionHasher.ComputeHash()` | 0 (struct HashCode) | ValueType accumulation |
| `ExpressionSimplifier.Simplify()` | 0 if no changes | Identity return |
| SQL `Translate()` | AST nodes (bounded) | Proportional to expression depth |
| SQL `Render()` | String allocation | Output SQL string |

---

## 4. BenchmarkDotNet Scenarios

All benchmarks live in `benchmarks/EricksonLopez.Specification.Benchmarks/`. Actual measurements will be published in `docs/benchmarks.md`.

- **Scenario A**: Single Predicate In-Memory Evaluation
- **Scenario B**: 3-Predicate AND Composition
- **Scenario C**: AndAll(10 specs) Bulk Composition
- **Scenario D**: `QuerySpec<T>` Construction
- **Scenario E**: SQL Translation
- **Scenario F**: Compiled Delegate Cache Hit Rate
- **Scenario G**: Reused Specification (Singleton Pattern)

---

## 5. Performance Anti-Patterns to Avoid

```csharp
// ANTI-PATTERN 1: New specification on every request (when it can be a singleton)
// Cost: 1 Lazy<T> allocation per request
public async Task<List<Customer>> GetActive() {
    return await _repo.ListAsync(new ActiveCustomerSpec(), ct);  // new each time
}

// BETTER: Singleton specification
private static readonly ActiveCustomerSpec _activeSpec = new();
public async Task<List<Customer>> GetActive() {
    return await _repo.ListAsync(_activeSpec, ct);
}

// ANTI-PATTERN 2: Re-translating the same spec every request in Dapper
// Cost: reflection-based closure extraction per call
// BETTER: SQL plan cache (v2.0) or cache the SqlQuery per spec type

// ANTI-PATTERN 3: Using IsSatisfiedBy in a tight loop (AOT interpreted path)
// Cost: 5-20x vs compiled delegate
foreach (var entity in millionEntities) {
    if (spec.IsSatisfiedBy(entity)) { ... }  // slow in AOT path
}

// BETTER for JIT:
var predicate = spec.ToCompiledPredicate();  // [RequiresDynamicCode]
foreach (var entity in millionEntities) {
    if (predicate(entity)) { ... }  // native delegate
}
```

---

## 6. AOT Performance Guidance

In NativeAOT environments:
- `IsSatisfiedBy()` uses `ExpressionInterpreter` — the ONLY AOT-compatible path.
- It is 5-20x slower than compiled execution.
- For domain validation (checking rules on domain objects): acceptable.
- For filtering large in-memory collections: use LINQ `.Where(spec.ToExpression())` which EF Core / LINQ will handle.
- For SQL queries via Dapper: SQL translation happens at infrastructure layer, not in-memory eval — no interpreter cost.
