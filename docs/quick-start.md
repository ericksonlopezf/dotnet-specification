# Quick Start Guide: EricksonLopez.Specification

Get up and running with **EricksonLopez.Specification** in under 5 minutes.

---

## 1. Installation

Install the core abstractions and engine via the .NET CLI or Package Manager:

```bash
# Core Domain Specification Engine (100% Native AOT Compatible)
dotnet add package EricksonLopez.Specification

# Core Abstractions (Lightweight interfaces for domain & application layers)
dotnet add package EricksonLopez.Specification.Abstractions
```

### Optional Infrastructure Packages

```bash
# LINQ & IQueryable extensions
dotnet add package EricksonLopez.Specification.Linq

# Entity Framework Core 9+ Repository & Evaluator
dotnet add package EricksonLopez.Specification.EntityFrameworkCore

# High-performance SQL AST Translator (PostgreSQL, SQL Server, SQLite, MySQL, MariaDB, Oracle)
dotnet add package EricksonLopez.Specification.Sql
dotnet add package EricksonLopez.Specification.PostgreSql # or MsSql, Sqlite, MySql, MariaDb, Oracle

# Micro-ORM & NoSQL Integrations
dotnet add package EricksonLopez.Specification.Dapper
dotnet add package EricksonLopez.Specification.MongoDB
dotnet add package EricksonLopez.Specification.Result
```

---

## 2. Create Your First Domain Specification

In Domain-Driven Design (DDD), business rules should live in pure domain classes, completely decoupled from persistence frameworks.

```csharp
using System.Linq.Expressions;
using EricksonLopez.Specification;

public sealed class ActiveCustomerSpecification : Specification<Customer>
{
    protected override Expression<Func<Customer, bool>> BuildExpression()
    {
        return customer => customer.IsActive;
    }
}

public sealed class PremiumCustomerSpecification : Specification<Customer>
{
    private readonly decimal _minimumPurchases;

    public PremiumCustomerSpecification(decimal minimumPurchases = 10)
    {
        _minimumPurchases = minimumPurchases;
    }

    protected override Expression<Func<Customer, bool>> BuildExpression()
    {
        return customer => customer.TotalPurchases >= _minimumPurchases;
    }
}
```

---

## 3. In-Memory Evaluation (100% Native AOT Safe)

Evaluate rules directly against entity instances without compiling IL or generating dynamic code:

```csharp
var customer = new Customer 
{ 
    Name = "Alice", 
    IsActive = true, 
    TotalPurchases = 15 
};

var activeSpec = new ActiveCustomerSpecification();

// Evaluated via the AOT-safe ExpressionInterpreter:
bool isEligible = activeSpec.IsSatisfiedBy(customer); // Returns true
```

---

## 4. Compose Specifications

Combine domain rules using boolean logic. The library supports standard methods, C# operators (`&`, `|`, `!`), and short-circuit evaluation (`&&`, `||`):

```csharp
var activeSpec = new ActiveCustomerSpecification();
var premiumSpec = new PremiumCustomerSpecification(10);

// Using standard combinator methods:
Specification<Customer> targetSpec1 = activeSpec.And(premiumSpec);
Specification<Customer> targetSpec2 = activeSpec.Or(premiumSpec);
Specification<Customer> targetSpec3 = activeSpec.Not();

// Using natural C# operators:
Specification<Customer> combined = activeSpec & premiumSpec;
Specification<Customer> alternative = activeSpec | premiumSpec;
Specification<Customer> inverted = !activeSpec;

// Short-circuit composition:
Specification<Customer> shortCircuit = activeSpec && premiumSpec;

// Combining multiple specifications via Span / Enumerable:
Specification<Customer> allOfThese = Spec.All(activeSpec, premiumSpec);
Specification<Customer> anyOfThese = Spec.Any(activeSpec, premiumSpec);
```

---

## 5. Build Complete Queries with `QuerySpec<T>`

`QuerySpec<T>` represents query intent (filters, ordering, pagination, cursor pagination, and projection):

```csharp
using EricksonLopez.Specification;

// Declarative query builder
var query = QuerySpec<Customer>.Empty
    .And(new ActiveCustomerSpecification())
    .Where(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-30))
    .OrderByDescending(c => c.TotalPurchases)
    .ThenBy(c => c.Name)
    .Page(page: 1, pageSize: 20);
```

---

## 6. Execute with LINQ / EF Core

Apply specifications directly onto any `IQueryable<T>` DbSet:

```csharp
using EricksonLopez.Specification.Linq;

// In your CQRS Query Handler or Repository:
public async Task<List<Customer>> Handle(GetActiveCustomersQuery query, CancellationToken ct)
{
    return await _dbContext.Customers
        .Apply(query.Spec)
        .ToListAsync(ct);
}
```

### Direct IQueryable & IEnumerable Extensions

```csharp
// Direct IQueryable filtering with specification:
IQueryable<Customer> filtered = _dbContext.Customers.Where(activeSpec);
bool anyVip = _dbContext.Customers.Any(premiumSpec);
int totalActive = _dbContext.Customers.Count(activeSpec);

// In-Memory IEnumerable filtering:
IEnumerable<Customer> cachedList = GetCachedCustomers();
List<Customer> activeOnly = cachedList.Where(activeSpec).ToList();
```

---

## 7. Next Steps

- Explore the [Getting Started Guide](getting-started.md) for full architecture and DI integration.
- Read the [Cookbook](cookbook.md) for ready-to-use patterns (keyset pagination, projections, MongoDB, Dapper).
- Review the [API Reference](api-reference.md) for complete signature documentation.
