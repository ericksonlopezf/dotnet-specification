// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification.EntityFrameworkCore;
using EricksonLopez.Specification.Showcase.Domain;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase.Levels;

// ─────────────────────────────────────────────────────────────────────────────
// CAPA DE DOMINIO / APLICACIÓN
// Contratos definidos en el core del dominio — sin dependencias de infraestructura.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Level 10 — Enterprise Architecture.
/// Demonstrates how the specification pattern integrates with Clean Architecture, CQRS, and DDD:
/// domain specifications live in domain layer, repositories in infrastructure,
/// handlers in application layer.
/// Covers: all methods of <see cref="IReadRepository{T}"/>,
/// <see cref="QuerySpec{T,TResult}"/> in enterprise architecture, and CQRS patterns.
/// </summary>
public sealed class Level10_EnterpriseArchitecture : ILevel
{
    private readonly ILogger<Level10_EnterpriseArchitecture> _logger;

    /// <inheritdoc/>
    public string Name => "Level 10 — Enterprise Architecture";

    /// <inheritdoc/>
    public string Description => "Clean Architecture + CQRS + DDD. Complete IReadRepository<T> API with enterprise projections.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level10_EnterpriseArchitecture(ILogger<Level10_EnterpriseArchitecture> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        // ─────────────────────────────────────────────────────────────────
        // Infrastructure: Application Handler only depends on IReadRepository<T>
        // ─────────────────────────────────────────────────────────────────
        IReadRepository<Customer> repo = new MockCustomerRepository();

        // ─────────────────────────────────────────────────────────────────
        // 1. IReadRepository<T>.ListAsync — complete query
        // ─────────────────────────────────────────────────────────────────
        var listSpec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => c.TotalPurchases > 10)
            .OrderByDescending(c => c.TotalPurchases)
            .Page(page: 1, pageSize: 5);

        var customers = await repo.ListAsync(listSpec);
        _logger.LogInformation("[ListAsync] Active VIP customers: {N}", customers.Count);
        foreach (var c in customers)
            _logger.LogInformation("  - {Name}: {Purchases} purchases", c.Name, c.TotalPurchases);

        // ─────────────────────────────────────────────────────────────────
        // 2. IReadRepository<T>.FirstOrDefaultAsync — get first
        // ─────────────────────────────────────────────────────────────────
        var firstSpec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.CreditLimit)
            .Take(1);

        var firstCustomer = await repo.FirstOrDefaultAsync(firstSpec);
        _logger.LogInformation("[FirstOrDefaultAsync] Top credit customer: {Name} ({CL})",
            firstCustomer?.Name ?? "N/A",
            firstCustomer?.CreditLimit);

        // ─────────────────────────────────────────────────────────────────
        // 3. IReadRepository<T>.SingleOrDefaultAsync — get single or default
        // ─────────────────────────────────────────────────────────────────
        var aliceSpec = QuerySpec<Customer>.Empty
            .Where(c => c.Name == "Alice");

        var alice = await repo.SingleOrDefaultAsync(aliceSpec);
        _logger.LogInformation("[SingleOrDefaultAsync] Alice found: {Found}", alice is not null);

        // ─────────────────────────────────────────────────────────────────
        // 4. IReadRepository<T>.GetByIdAsync — get by identifier
        // ─────────────────────────────────────────────────────────────────
        var targetId = alice?.Id ?? Guid.Empty;
        var customerById = await repo.GetByIdAsync(targetId);
        _logger.LogInformation("[GetByIdAsync] Customer by Id={Id}: {Name}", targetId, customerById?.Name ?? "N/A");

        // ─────────────────────────────────────────────────────────────────
        // 5. IReadRepository<T>.CountAsync — count records
        // ─────────────────────────────────────────────────────────────────
        var countSpec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        int count = await repo.CountAsync(countSpec);
        _logger.LogInformation("[CountAsync] Active customers: {N}", count);

        // ─────────────────────────────────────────────────────────────────
        // 6. IReadRepository<T>.AnyAsync — verify existence
        // ─────────────────────────────────────────────────────────────────
        var inactiveSpec = QuerySpec<Customer>.Empty.Where(c => !c.IsActive);
        bool hasInactive = await repo.AnyAsync(inactiveSpec);
        _logger.LogInformation("[AnyAsync] Inactive customers exist? {R}", hasInactive);

        // ─────────────────────────────────────────────────────────────────
        // 7. IReadRepository<T>.ListAsync<TResult> — typed projection
        // ─────────────────────────────────────────────────────────────────
        var projectedSpec = new QuerySpec<Customer, CustomerSummary>()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.TotalPurchases)
            .Select(c => new CustomerSummary(c.Id, c.Name, c.TotalPurchases));

        IReadOnlyList<CustomerSummary> summaries = await repo.ListAsync(projectedSpec);
        _logger.LogInformation("[ListAsync<TResult>] Projections ({N}):", summaries.Count);
        foreach (var s in summaries)
            _logger.LogInformation("  - {Name}: {Purchases} purchases", s.Name, s.TotalPurchases);

        // ─────────────────────────────────────────────────────────────────
        // 8. Clean Architecture Pattern
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("""

    ┌─── CLEAN ARCHITECTURE ────────────────────────────────────────────────┐
    │                                                                        │
    │  Domain Layer (Core)                                                   │
    │    • Customer, Order (entities)                                        │
    │    • ActiveCustomerSpecification (domain rule)                         │
    │    • VipCustomerSpecification (domain rule)                            │
    │    → Zero infrastructure dependencies. Unit testable in isolation.    │
    │                                                                        │
    │  Application Layer                                                     │
    │    • GetTopCustomersQuery : IRequest<List<CustomerSummary>>            │
    │    • GetTopCustomersHandler(IReadRepository<Customer>)                 │
    │    → Instantiates domain specs, calls repository.                      │
    │    → Only depends on IReadRepository<T> abstraction.                   │
    │                                                                        │
    │  Infrastructure Layer                                                  │
    │    • EfReadRepository<TDbContext, TEntity> : IReadRepository<TEntity>  │
    │    • MongoReadRepository<TDocument> : IReadRepository<TDocument>       │
    │    • Dapper / DapperExtensions                                         │
    │    → Translates QuerySpec<T> to SQL/LINQ/Mongo.                        │
    │                                                                        │
    │  Presentation Layer                                                    │
    │    • CustomerController / gRPC / Worker                                │
    │    → Calls Application Handler via Mediator or direct dispatch.        │
    │                                                                        │
    └────────────────────────────────────────────────────────────────────────┘
    """);

        // ─────────────────────────────────────────────────────────────────
        // 9. DI Registration
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[DI] Specification<T>: register as Singleton (immutable, thread-safe).");
        _logger.LogInformation("[DI] AddSpecificationEntityFramework: registers EF Core repositories automatically.");
        _logger.LogInformation("[DI] IReadRepository<T>: register as Scoped (matches DbContext lifetime).");

        // ─────────────────────────────────────────────────────────────────
        // 10. ISpecificationEvaluator — EF Core pipeline abstraction
        // ─────────────────────────────────────────────────────────────────
        ISpecificationEvaluator evaluator = EfSpecificationEvaluator.Default;
        _logger.LogInformation("[ISpecificationEvaluator] Type: {T}", evaluator.GetType().Name);

        // ─────────────────────────────────────────────────────────────────
        // 11. QuerySpecEfCoreExtensions.Apply(asSplitQuery, ignoreAutoIncludes)
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[QuerySpecEfCoreExtensions.Apply] asSplitQuery, ignoreAutoIncludes available.");
        _logger.LogInformation("[QuerySpecEfCoreExtensions.Apply<T,TResult>] Projected version available.");

        // ─────────────────────────────────────────────────────────────────
        // 12. EfReadRepository variants
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[EfReadRepository<T>] Single DbContext repository.");
        _logger.LogInformation("[EfReadRepository<TDbContext,TEntity>] Two-parameter repository for multiple contexts.");
    }
}




