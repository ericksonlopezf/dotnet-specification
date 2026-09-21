# adr-002: No Include / ThenInclude in Specifications

## Status
Accepted

## Date
2026-08-12

**Status**: Accepted  
**Date**: 2026-08-12  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: ORM Coupling / Architectural Boundary

---

## Context

Ardalis.Specification includes `.Include()` and `.ThenInclude()` directly inside specifications (`spec.Query.Include(c => c.Orders).ThenInclude(o => o.Items)`). This is possible because Ardalis was designed exclusively for EF Core, which exposes `IIncludableQueryable<T, TProperty>`.

During the audit it was evaluated whether EricksonLopez should support this feature to reduce migration friction from Ardalis.

## Decision

**REJECTED. EricksonLopez.Specification will never include Include/ThenInclude anywhere in its public API.**

## Rationale

1. **Immediate ORM coupling.** `IIncludableQueryable<T, TProperty>` is a type from `Microsoft.EntityFrameworkCore`. Including it in the domain layer or the core library violates layer separation.

2. **Incompatible with the Dapper differentiator.** Dapper has no concept of `Include`. If `QuerySpec<T>` included includes, any Dapper SQL translation would silently ignore them — surprising and dangerous behavior.

3. **Incompatible with AOT.** EF Core's `Include` infrastructure uses reflection extensively. It is not compatible with NativeAOT without significant workarounds.

4. **Violates the Specification Pattern.** A domain specification encapsulates *which* entities satisfy a business rule. *How* related entities are loaded is an infrastructure decision, not a domain one.

5. **The problem is trivially solved in the correct layer.** Includes belong in the EF Core repository, not in the specification.

## Consequences

- **Positive**: `Specification<T>` and `QuerySpec<T>` are completely ORM-agnostic. They work equally with EF Core, Dapper, and in-memory evaluation.
- **Negative**: Ardalis users with `Include` in their specs must move that logic to the infrastructure layer. May require repository changes.
- **Mitigation**: The migration guide (`docs/migration-from-ardalis.md`, Step 2) explicitly documents this decision and the correct pattern.

## Documented Alternative

```csharp
// In the domain specification (domain layer):
public sealed class ActiveCustomer : Specification<Customer>
{
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => c => c.IsActive;
    // No Include here — this is a domain rule, not an EF Core query
}

// In the EF Core repository (infrastructure layer):
public async Task<IReadOnlyList<Customer>> ListAsync(QuerySpec<Customer> spec, CancellationToken ct)
{
    return await _dbContext.Customers
        .Include(c => c.Orders)              // Includes go here
            .ThenInclude(o => o.Items)
        .Apply(spec)                         // Spec is applied here
        .ToListAsync(ct);
}
```
