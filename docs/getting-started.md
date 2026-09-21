# Getting Started: EricksonLopez.Specification

A comprehensive guide for architects and senior engineers adopting **EricksonLopez.Specification** in production .NET enterprise applications.

---

## 🏛️ Architectural Foundations

The Specification Pattern (Evans & Fowler, DDD) formalizes business logic into first-class domain objects.

### The Problem: Rule Scattering & Query Coupling

In typical architectures:
1. **Rule Duplication**: The definition of an "Active Customer" is repeated across EF Core queries, business validation services, background jobs, and test assertions.
2. **Persistence Leaks**: UI or Application layers write inline lambdas that bind directly to database columns or navigation properties.
3. **Untestable Queries**: Inline LINQ queries cannot be unit-tested in isolation without mocking `IQueryable` or standing up a database.

### The Solution: Isolated, Composable Domain Rules

With `EricksonLopez.Specification`:
- A business rule lives in a single, sealed, immutable class in the Domain layer (`Domain/Specifications/ActiveCustomerSpecification.cs`).
- The domain class has zero dependencies on Entity Framework, Dapper, SQL, or MongoDB.
- It can be evaluated in memory (`spec.IsSatisfiedBy(entity)`) or translated to server-side SQL/NoSQL filters.
- Multiple rules compose cleanly via boolean algebra (`spec1 & spec2`, `spec1.Or(spec2)`).

---

## 🧩 Clean Architecture Layer Breakdown

```
┌─────────────────────────────────────────────────────────────┐
│                       Presentation                          │
│         Controllers / Minimal APIs / gRPC Endpoints         │
└──────────────────────────────┬──────────────────────────────┘
                               │ Invokes Mediator / Handlers
┌──────────────────────────────▼──────────────────────────────┐
│                    Application (CQRS)                       │
│   • Request: GetActiveCustomersQuery(int Page, int PageSize)│
│   • Handler: GetActiveCustomersHandler                      │
│   • Dependency: IReadRepository<Customer>                   │
│   → Combines domain specs with pagination & sorting         │
└──────────────────────────────┬──────────────────────────────┘
                               │ Uses Pure Specifications
┌──────────────────────────────▼──────────────────────────────┐
│                       Domain (Core)                         │
│   • Entities: Customer, Order                               │
│   • Rules: ActiveCustomerSpecification, VipSpecification    │
│   • Pure C# + System.Linq.Expressions                       │
│   → ZERO database or ORM dependencies                       │
└──────────────────────────────▲──────────────────────────────┘
                               │ Implements
┌──────────────────────────────┴──────────────────────────────┐
│                       Infrastructure                        │
│   • EfReadRepository<AppDbContext, Customer>                │
│   • MongoReadRepository<Customer>                           │
│   • QuerySpecTranslator & SqlDialect implementations        │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Dependency Injection Setup

### 1. Registering Domain Specifications

Domain specifications are **immutable and thread-safe**. Register them as `Singleton` or instantiate them directly:

```csharp
// Program.cs or DependencyInjection.cs
builder.Services.AddSingleton<ActiveCustomerSpecification>();
builder.Services.AddSingleton<VipCustomerSpecification>();
```

### 2. Entity Framework Core Integration

Use the official DI extensions from `EricksonLopez.Specification.EntityFrameworkCore`:

```csharp
using EricksonLopez.Specification.EntityFrameworkCore;

// Registers EF Core read repositories and evaluators:
builder.Services.AddSpecificationEntityFramework<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Register specific entity read repositories:
builder.Services.AddEfReadRepository<AppDbContext, Customer>();
builder.Services.AddEfReadRepository<AppDbContext, Order>();
```

---

## ⚡ CQRS Query Handler Implementation

Here is a production-ready MediatR / CQRS Query Handler:

```csharp
public sealed record GetVipCustomersQuery(int Page = 1, int PageSize = 20) 
    : IRequest<IReadOnlyList<CustomerSummary>>;

public sealed class GetVipCustomersHandler : IRequestHandler<GetVipCustomersQuery, IReadOnlyList<CustomerSummary>>
{
    private readonly IReadRepository<Customer> _customerRepository;
    private readonly ActiveCustomerSpecification _activeSpec;
    private readonly VipCustomerSpecification _vipSpec;

    public GetVipCustomersHandler(
        IReadRepository<Customer> customerRepository,
        ActiveCustomerSpecification activeSpec,
        VipCustomerSpecification vipSpec)
    {
        _customerRepository = customerRepository;
        _activeSpec = activeSpec;
        _vipSpec = vipSpec;
    }

    public async Task<IReadOnlyList<CustomerSummary>> Handle(
        GetVipCustomersQuery request, 
        CancellationToken cancellationToken)
    {
        // Compose domain rules with projection and pagination:
        var querySpec = new QuerySpec<Customer, CustomerSummary>()
            .And(_activeSpec)
            .And(_vipSpec)
            .OrderByDescending(c => c.TotalPurchases)
            .ThenBy(c => c.Name)
            .Page(request.Page, request.PageSize)
            .Select(c => new CustomerSummary(c.Id, c.Name, c.TotalPurchases));

        return await _customerRepository.ListAsync(querySpec, cancellationToken);
    }
}
```

---

## 🛡️ Native AOT & Zero Dynamic Code

Unlike older specification libraries that rely on `Compile()` or Reflection.Emit (which fail under Native AOT with trimming):

1. **`ExpressionInterpreter`**: In-memory evaluation (`IsSatisfiedBy`) is performed via a dedicated AST tree interpreter that walks expression nodes without generating dynamic IL code.
2. **`QuerySpecTranslator`**: SQL queries are generated through static AST mapping (`QueryModel`) and dialect renders.
3. **Trimmer-Safe Annotations**: All generic methods are annotated with `[DynamicallyAccessedMembers]` to guarantee linker preservation.

> [!NOTE]
> If your application runs on standard JIT and requires maximum throughput for millions of in-memory evaluations, you can optionally invoke `.ToCompiledPredicate()` to utilize the bounded `ExpressionCompilationCache`.

---

## 📊 Observability (OpenTelemetry)

The library ships built-in OpenTelemetry instrumentation in `EricksonLopez.Specification.Diagnostics`:

- **ActivitySource**: `"EricksonLopez.Specification"`
- **Meter**: `"EricksonLopez.Specification"`
  - `specification.evaluations`: Total count of evaluated specifications
  - `specification.compositions`: Count of composed specifications (And/Or/Not)
  - `specification.sql_translations`: Total SQL query translations
  - `specification.evaluation_duration`: Histogram measuring in-memory evaluation latency
  - `specification.sql_translation_duration`: Histogram measuring AST-to-SQL translation latency

To enable in your telemetry setup:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("EricksonLopez.Specification"))
    .WithMetrics(metrics => metrics.AddMeter("EricksonLopez.Specification"));
```
