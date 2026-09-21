# adr-017: No Async Specifications (Reject IAsyncSpecification<T>)

## Status
Rejected

## Date
2026-08-14

**Status**: Accepted  
**Date**: 2026-08-14  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: Core Architecture / DDD

---

## Context

An `IAsyncSpecification<T>` interface with `Task<bool> IsSatisfiedByAsync(T entity)` has been
proposed for scenarios where a specification's evaluation requires database or external service access —
for example, checking whether a customer is blocked in a third-party credit system before allowing an order.

---

## Problem

Should `Specification<T>` support asynchronous evaluation via `Task<bool>` or `ValueTask<bool>`?

---

## Options Considered

### Option A — Add `IAsyncSpecification<T>` to Core

```csharp
public interface IAsyncSpecification<T>
{
    Task<bool> IsSatisfiedByAsync(T entity, CancellationToken ct = default);
}
```

### Option B — Add as optional separate package

`EricksonLopez.Specification.Async` that implements the interface separately from the domain core.

### Option C — Reject entirely

Document that async evaluation is outside the scope of the Specification Pattern and belongs to Domain Services.

---

## Decision

**REJECTED. `IAsyncSpecification<T>` will not be added to the library in any form.**

---

## Decision Drivers

- **DDD**: A `Specification<T>` expresses a condition — a predicate over a value. Predicates are synchronous by nature. A predicate that performs I/O is no longer a predicate — it is a service.
- **Single Responsibility**: Mixing synchronous domain predicates with I/O execution violates SRP.
- **API simplicity**: Adding async variants doubles the composition API surface without architectural benefit.
- **AOT**: `Task<bool>` introduces allocations incompatible with the zero-overhead AOT goal.
- **Type safety**: Specifications that require infrastructure access cannot be used for in-memory evaluation (`IsSatisfiedBy`) — defeating the primary value of the pattern.

---

## Consequences

### Positive

- Domain layer remains free of I/O.
- `IsSatisfiedBy(T)` remains a guaranteed synchronous, pure evaluation.
- Specifications remain composable without async contamination.
- Testing remains infrastructure-free.

### Negative

- Developers who want to check external conditions before accepting a domain action must use Domain Services or Application Service orchestration instead.

---

## Why Alternatives Were Rejected

**Option A**: Allows infrastructure coupling at the domain level. Once a specification can do I/O, the guarantee that `IsSatisfiedBy` is pure is lost. The domain layer becomes implicitly coupled to databases and services.

**Option B**: A separate package creates a parallel specification hierarchy that is incompatible with the core `Specification<T>` composition engine (`And`, `Or`, `Not`). You cannot AND a sync spec with an async spec without forcing one path.

---

## Correct Pattern for Async Validation

When external data is required before applying a business rule:

```csharp
// Application Service — not Specification
public class PlaceOrderService
{
    private readonly ICreditService _creditService;

    public async Task<Result> PlaceOrderAsync(Order order, CancellationToken ct)
    {
        // Resolve external state BEFORE creating the specification
        var creditStatus = await _creditService.GetStatusAsync(order.CustomerId, ct);
        
        // Now apply a PURE, synchronous specification
        var spec = new OrderEligibleSpec(creditStatus.Limit);
        if (!spec.IsSatisfiedBy(order))
            return Result.Failure("Order does not meet credit requirements.");
        
        // ...
    }
}
```

---

## Relationship With Project Philosophy

The project philosophy defines Specification as a "domain concept for expressing reusable, composable business predicates." Predicates are, by definition, functions from domain values to boolean. They do not perform side effects, I/O, or asynchronous operations.

---

## Migration Strategy

Not applicable — this feature has never been implemented.

## Reconsideration Criteria

This decision should be reconsidered if:
1. A core use case is identified where async evaluation cannot be replaced by domain service orchestration.
2. The .NET runtime introduces zero-allocation async primitives that eliminate the overhead concern.

Neither condition is expected to occur in practice.
