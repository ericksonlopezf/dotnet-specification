# Level 10: Enterprise Architecture — Clean Architecture, CQRS, and DDD

## Overview

Level 10 demonstrates how the Specification Pattern integrates with Clean Architecture, CQRS, and DDD. All IReadRepository<T> methods are exercised.

## IReadRepository<T> — Complete API

| Method | Description |
|---|---|
| ListAsync(QuerySpec<T>) | All matching entities |
| ListAsync<TResult>(QuerySpec<T,TResult>) | Typed projections |
| FirstOrDefaultAsync(QuerySpec<T>) | First match or null |
| SingleOrDefaultAsync(QuerySpec<T>) | Exactly one or null |
| GetByIdAsync<TId>(id) | By primary key |
| CountAsync(QuerySpec<T>) | Scalar count |
| AnyAsync(QuerySpec<T>) | Existence check |

## EfSpecificationEvaluator

`csharp
// Build IQueryable from QuerySpec
IQueryable<Customer> query = EfSpecificationEvaluator.GetQuery(dbContext.Customers, spec);

// Via interface
ISpecificationEvaluator evaluator = EfSpecificationEvaluator.Default;
`

## EfReadRepository Variants

`csharp
// Single DbContext (simple applications)
public class CustomerRepository : EfReadRepository<AppDbContext, Customer>
{
    public CustomerRepository(AppDbContext context) : base(context) {}
}

// Two-parameter (multiple DbContexts in one app)
public class CustomerRepo : EfReadRepository<CatalogDbContext, Customer> { }
`

## Projection Pattern

`csharp
var spec = new QuerySpec<Customer, CustomerSummary>()
    .Where(c => c.IsActive)
    .OrderByDescending(c => c.TotalPurchases)
    .Select(c => new CustomerSummary(c.Id, c.Name, c.TotalPurchases));

IReadOnlyList<CustomerSummary> summaries = await repo.ListAsync(spec);
`

## DI Registration Best Practices

| Component | Lifetime | Reason |
|---|---|---|
| Specification<T> subclasses | Singleton | Immutable, thread-safe |
| IReadRepository<T> / EfReadRepository | Scoped | Matches DbContext lifetime |
| QueryPlanCache | Singleton (static) | Process-wide LRU cache |

## Running Example

See [Level10_EnterpriseArchitecture.cs](../../samples/Showcase/Levels/Level10_EnterpriseArchitecture.cs).
