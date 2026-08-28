# adr-006: Strict Separation of `Specification<T>` (predicate) and `QuerySpec<T>` (query descriptor)

**Status**: Accepted  
**Date**: 2026-08-12  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: Core Architecture / DDD

---

## Context

The most fundamental architectural decision of the library: should the domain predicate and the query descriptor (ordering, pagination, projection) live in the same class or in separate classes?

Ardalis.Specification puts everything in a single class:

```csharp
// Ardalis — everything mixed together
public class ActiveCustomerSpec : Specification<Customer>
{
    public ActiveCustomerSpec()
    {
        Query.Where(c => c.IsActive)
             .OrderBy(c => c.Name)
             .Take(20)
             .Include(c => c.Orders);
    }
}
```

The audit validated whether EricksonLopez should follow this pattern or maintain the separation.

## Decision

**CONFIRMED. The separation between `Specification<T>` (pure predicate) and `QuerySpec<T>` (immutable query descriptor) is a non-negotiable design invariant.**

- `Specification<T>`: only `BuildExpression()`. Zero ordering. Zero pagination. Zero projection. Zero ORM.
- `QuerySpec<T>`: immutable sealed record. Ordering, pagination, projection, filters. Zero business rules.

## Rationale

1. **A class should have only one reason to change (SRP).** The "active customer" rule does not change when the customer list pagination changes. These are independent changes for independent reasons.

2. **Domain specifications are reusable across multiple queries.** `ActiveCustomer` can be used in: "list active customers paginated", "check if any active exist", "count active by country", "notify active premium". Each use has different ordering/pagination. If they were in the same class, you would need N classes for N combinations.

3. **Testing without infrastructure.** `spec.IsSatisfiedBy(customer)` requires no DbContext, IQueryable, or any infrastructure. This is only possible because `Specification<T>` is a pure predicate.

4. **Immutability of `QuerySpec<T>` prevents concurrency bugs.** As a sealed record, it is thread-safe by design. In Ardalis, the specification object is mutable — two threads can modify the same object.

5. **Eliminates the temptation to inject services into the domain.** If `Specification<T>` has no infrastructure constructor, it is structurally impossible to inject a `DbContext` or `IRepository` into it. SPEC008 enforces this at compile time.

6. **Compatible with Dapper without design changes.** A `QuerySpec<T>` can be directly translated to SQL because it is a data-only descriptor. If ordering/pagination were in `Specification<T>` alongside the predicate, the SQL translator would need to handle the opacity of the domain object.

## Consequences

- **Positive**: Testable specifications without infrastructure. Maximum reuse. Thread safety. DDD-correct.
- **Negative**: Learning curve for Ardalis users who assume the specification contains everything. Requires explanatory documentation.
- **Mitigation**: Migration guide documents the pattern. The cookbook shows both objects in use together.

## Canonical Pattern

```csharp
// Domain (predicate only)
public sealed class ActivePremiumCustomer : Specification<Customer>
{
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => c => c.IsActive && c.TotalPurchases >= 1_000m;
}

// Application (combines predicate + query descriptor)
var query = new ActivePremiumCustomer()
    .ToQuerySpec()
    .OrderByDescending(c => c.TotalPurchases)
    .Take(50);

// Domain — pure unit test, zero infrastructure
Assert.True(new ActivePremiumCustomer().IsSatisfiedBy(customer));
```
