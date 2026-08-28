# adr-003: No Dynamic String-Based Ordering

**Status**: Accepted  
**Date**: 2026-08-12  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: Type Safety / Security

---

## Context

Ardalis.Specification and several Dynamic LINQ libraries allow dynamic ordering by property name as a string: `Query.OrderBy("Name")` or `Query.OrderBy("TotalPurchases DESC")`. This is convenient for grids and DataTables where the user can choose the sort column.

The audit evaluated whether `QuerySpec<T>` should support this pattern.

## Decision

**REJECTED. EricksonLopez.Specification will not implement dynamic string ordering. The only ordering API is strongly typed: `.OrderBy(c => c.Name)`.**

## Rationale

1. **Breaks compile-time type safety.** `OrderBy("Naem")` compiles perfectly. The error appears at runtime, possibly in production. `OrderBy(c => c.Naem)` does not compile.

2. **SQL injection risk in SQL context.** `QuerySpecTranslator<T>` generates SQL directly. A sort string accepted from the user that reaches `ORDER BY {columnName}` would be a critical vulnerability. The type-safe `Expression<Func<T, object?>>` eliminates this attack vector entirely — only properties that exist in the model can be sorted.

3. **Requires reflection or Dynamic LINQ.** Dynamic LINQ uses `Expression.Dynamic` or runtime compilation — incompatible with NativeAOT. Property reflection requires `[DynamicallyAccessedMembers]` throughout the entire chain.

4. **The real use case belongs in the presentation layer.** If the user selects a column in a grid, the translation logic `userColumn → strongly typed expression` must live in the presentation/application layer, not in the domain specification.

5. **Anti-pattern for DDD.** Domain predicates should not depend on external strings. A domain specification must be deterministic and safe.

## Consequences

- **Positive**: Zero SQL injection risk from column names. Compilation fails instead of runtime. AOT-compatible by design.
- **Negative**: Grids requiring dynamic ordering by user selection need a switch/dictionary in the application layer.
- **Mitigation**: Pattern documented in the cookbook.

## Documented Alternative

```csharp
// In the application layer:
public QuerySpec<Customer> BuildQuery(string sortColumn, bool descending)
{
    var query = QuerySpec<Customer>.Empty.Where(c => c.IsActive);

    return (sortColumn, descending) switch
    {
        ("name", false)  => query.OrderBy(c => c.Name),
        ("name", true)   => query.OrderByDescending(c => c.Name),
        ("total", false) => query.OrderBy(c => c.TotalPurchases),
        ("total", true)  => query.OrderByDescending(c => c.TotalPurchases),
        _                => query.OrderBy(c => c.Id)  // default
    };
}
```

This pattern is type-safe, compilable, AOT-compatible, and trivially testable.
