# Migrating from Ardalis.Specification to EricksonLopez.Specification

This guide maps every Ardalis concept to its EricksonLopez equivalent, and explains the architectural differences.

> **Why migrate?** If you use Dapper, target NativeAOT, or want strict DDD layer separation, EricksonLopez provides capabilities Ardalis cannot. If you use EF Core exclusively for standard CRUD, evaluate the migration cost carefully — Ardalis is mature and has 18M+ downloads.

---

## Core Concept Mapping

| Ardalis | EricksonLopez | Notes |
|---------|---------------|-------|
| `Specification<T>` | `Specification<T>` | Same name. Different contract. |
| `ISpecification<T>` | `ISpecification<T>` | Similar interface. |
| `SpecificationEvaluator` | `QuerySpecLinqExtensions.Apply()` | Extension method instead of evaluator class. |
| `IRepository<T>` | ❌ (not provided) | Write repositories are not a specification concern. Implement your own. |
| `IReadRepositoryBase<T>` | `IReadRepository<T>` | Read-only contract. |
| `IRepositoryBase<T>.ListAsync(spec)` | `IReadRepository<T>.ListAsync(querySpec)` | Same purpose. |
| `IRepositoryBase<T>.CountAsync(spec)` | `IReadRepository<T>.CountAsync(querySpec)` | Same purpose. |
| `IRepositoryBase<T>.AnyAsync(spec)` | `IReadRepository<T>.AnyAsync(querySpec)` | Same purpose. |
| `IRepositoryBase<T>.FirstOrDefaultAsync(spec)` | `IReadRepository<T>.FirstOrDefaultAsync(querySpec)` | Same purpose. |
| `IRepositoryBase<T>.SingleOrDefaultAsync(spec)` | `IReadRepository<T>.SingleOrDefaultAsync(querySpec)` | Same purpose. |

---

## Step 1: Replace Ardalis Specification with EricksonLopez Specification

### Ardalis pattern
```csharp
public class ActiveCustomerSpec : Specification<Customer>
{
    public ActiveCustomerSpec()
    {
        Query.Where(c => c.IsActive)
             .OrderBy(c => c.Name)
             .Take(20);
    }
}
```

### EricksonLopez pattern
```csharp
// Domain spec — pure predicate only
public sealed class ActiveCustomer : Specification<Customer>
{
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => c => c.IsActive;
}

// Query descriptor — separate from the domain rule
var query = new ActiveCustomer()
    .ToQuerySpec()
    .OrderBy(c => c.Name)
    .Take(20);
```

**Key difference**: In EricksonLopez, ordering and pagination belong to `QuerySpec<T>`, NOT to the domain specification. The domain spec is a pure predicate.

---

## Step 2: Replace Include() calls

Ardalis supports `Include` / `ThenInclude` inside specifications. EricksonLopez **intentionally omits this** — ORM-specific features should not belong to domain specifications.

### Ardalis
```csharp
Query.Include(c => c.Orders).ThenInclude(o => o.Items);
```

### EricksonLopez — move includes to the repository/infrastructure
```csharp
// In your EF Core repository implementation:
var query = dbContext.Customers
    .Include(c => c.Orders)
    .ThenInclude(o => o.Items)
    .Apply(querySpec);
```

---

## Step 3: Replace specification-internal ordering

### Ardalis
```csharp
Query.OrderBy(c => c.Name).ThenByDescending(c => c.CreatedAt);
```

### EricksonLopez
```csharp
var query = QuerySpec<Customer>.Empty
    .And(new ActiveCustomer())
    .OrderBy(c => c.Name)
    .ThenByDescending(c => c.CreatedAt);
```

---

## Step 4: Replace IsSatisfiedBy

Ardalis does not have a standard `IsSatisfiedBy` for in-memory evaluation. EricksonLopez provides it as a first-class API:

```csharp
var spec = new ActiveCustomer();

// In-memory evaluation — AOT-safe
bool satisfies = spec.IsSatisfiedBy(customer);

// Use in unit tests without any infrastructure
Assert.True(spec.IsSatisfiedBy(activeCustomer));
Assert.False(spec.IsSatisfiedBy(inactiveCustomer));
```

---

## Step 5: Replace SpecificationEvaluator with Apply()

### Ardalis (EF Core)
```csharp
var result = await _specificationEvaluator.GetQuery(dbContext.Customers, spec).ToListAsync();
```

### EricksonLopez (EF Core)
```csharp
using EricksonLopez.Specification.Linq;

var result = await dbContext.Customers.Apply(querySpec).ToListAsync();
```

---

## Step 6: Replace Repository pattern

### Ardalis
```csharp
public class CustomerRepository : EfRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext dbContext) : base(dbContext) { }
}
```

### EricksonLopez — implement IReadRepository<T> directly
```csharp
public sealed class EfCustomerRepository : IReadRepository<Customer>
{
    private readonly AppDbContext _dbContext;
    public EfCustomerRepository(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<Customer>> ListAsync(
        QuerySpec<Customer> specification, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Apply(specification)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> FirstOrDefaultAsync(
        QuerySpec<Customer> specification, CancellationToken cancellationToken = default)
        => await _dbContext.Customers.Apply(specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<Customer?> SingleOrDefaultAsync(
        QuerySpec<Customer> specification, CancellationToken cancellationToken = default)
        => await _dbContext.Customers.Apply(specification).SingleOrDefaultAsync(cancellationToken);

    public async Task<int> CountAsync(
        QuerySpec<Customer> specification, CancellationToken cancellationToken = default)
        => await _dbContext.Customers.Apply(specification).CountAsync(cancellationToken);

    public async Task<bool> AnyAsync(
        QuerySpec<Customer> specification, CancellationToken cancellationToken = default)
        => await _dbContext.Customers.Apply(specification).AnyAsync(cancellationToken);

    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        QuerySpec<Customer, TResult> specification, CancellationToken cancellationToken = default)
        => await _dbContext.Customers.Apply(specification).ToListAsync(cancellationToken);
}
```

---

## Feature Coverage Comparison

| Feature | Ardalis | EricksonLopez | Notes |
|---------|---------|---------------|-------|
| Domain spec (predicate only) | ❌ (mixed with query) | ✅ | Architectural purity |
| In-memory IsSatisfiedBy | ❌ | ✅ | Pure unit testing |
| NativeAOT compatibility | ❌ | ✅ | Designed for it |
| Dapper SQL translation | ❌ | ✅ | Core differentiator |
| PostgreSQL dialect | ❌ | ✅ | LIKE, ILIKE, ANY |
| SQL Server dialect | ❌ | ✅ | TOP N, OFFSET/FETCH |
| EF Core IQueryable | ✅ | ✅ | Via .Apply() |
| Include / ThenInclude | ✅ | ❌ (by design) | Move to repository |
| Write repository | ✅ | ❌ (by design) | Implement yourself |
| Roslyn analyzers | ❌ | ✅ (SPEC001–SPEC011) | Compile-time enforcement (SPEC011 detects Ardalis specs) |
| Expression.Invoke-free | ✅ | ✅ | |
| Immutable query descriptor | ❌ (mutable) | ✅ (sealed record) | Thread-safe caching |
| Dynamic string ordering | ✅ | ❌ (by design) | Type-unsafe anti-pattern |

---

## What EricksonLopez Does NOT Provide (by design)

- **Write repository (`IRepository<T>`)**: Not a specification concern. Implement `IRepository<T>` yourself in the infrastructure layer.
- **Include / ThenInclude in specs**: ORM-specific. Move to the EF Core repository implementation.
- **Dynamic string ordering**: Type-unsafe, SQL-injection risk in SQL contexts. Use strongly typed `.OrderBy(c => c.Name)`.
- **FluentValidation integration**: Different framework. Compose them yourself at the application layer.

These are intentional omissions that keep the library focused and DDD-correct.
