// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification.Linq;
using EricksonLopez.Specification.Showcase.Domain;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase.Levels;

/// <summary>
/// Level 3 — Real-World Use Cases.
/// Demonstrates LINQ integration (<see cref="QuerySpecLinqExtensions"/>)
/// using in-memory collections simulating an EF Core DbSet.
/// Covers: <c>Apply&lt;T&gt;</c>, <c>Apply&lt;T,TResult&gt;</c>, <c>Any&lt;T&gt;</c>, <c>Count&lt;T&gt;</c>.
/// </summary>
public sealed class Level3_RealUseCases : ILevel
{
    private readonly ILogger<Level3_RealUseCases> _logger;

    /// <inheritdoc/>
    public string Name => "Level 3 — Real-World Use Cases";

    /// <inheritdoc/>
    public string Description => "Application of QuerySpec<T> and QuerySpec<T,TResult> against IQueryable using QuerySpecLinqExtensions.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level3_RealUseCases(ILogger<Level3_RealUseCases> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        // ─────────────────────────────────────────────────────────────────
        // Test data — simulating an EF Core DbSet<Customer>.
        // ─────────────────────────────────────────────────────────────────
        var customers = new List<Customer>
        {
            new Customer { Name = "Alice",   IsActive = true,  TotalPurchases = 25, CreditLimit = 10_000m, CreatedAt = DateTime.UtcNow.AddDays(-60) },
            new Customer { Name = "Bob",     IsActive = false, TotalPurchases = 2,  CreditLimit = 500m,   CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new Customer { Name = "Charlie", IsActive = true,  TotalPurchases = 8,  CreditLimit = 3_000m, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new Customer { Name = "Diana",   IsActive = true,  TotalPurchases = 42, CreditLimit = 15_000m, CreatedAt = DateTime.UtcNow.AddDays(-90) },
            new Customer { Name = "Eve",     IsActive = true,  TotalPurchases = 11, CreditLimit = 7_500m, CreatedAt = DateTime.UtcNow.AddDays(-3) },
        }.AsQueryable();

        // ─────────────────────────────────────────────────────────────────
        // 1. Apply<T>(IQueryable<T>, QuerySpec<T>)
        //    Applies filters, ordering, and pagination onto an IQueryable<T>.
        //    EF Core translates this to server-side SQL.
        // ─────────────────────────────────────────────────────────────────
        var topVipQuery = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => c.TotalPurchases > 10)
            .OrderByDescending(c => c.TotalPurchases)
            .Take(3);

        var topVip = customers.Apply(topVipQuery).ToList();

        _logger.LogInformation("[Apply<T>] Active VIPs (Top 3 by purchases):");
        foreach (var c in topVip)
            _logger.LogInformation("  - {Name}: {Purchases} purchases", c.Name, c.TotalPurchases);

        // ─────────────────────────────────────────────────────────────────
        // 2. Apply<T, TResult>(IQueryable<T>, QuerySpec<T, TResult>)
        //    Applies a projected QuerySpec producing IQueryable<TResult>.
        //    Ideal for DTOs/projections without loading whole entities.
        // ─────────────────────────────────────────────────────────────────
        var summaryQuery = new QuerySpec<Customer, CustomerSummary>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.TotalPurchases)
            .Select(c => new CustomerSummary(c.Id, c.Name, c.TotalPurchases));

        var summaries = customers.Apply(summaryQuery).ToList();

        _logger.LogInformation("[Apply<T,TResult>] Projected summaries ({N}):", summaries.Count);
        foreach (var s in summaries)
            _logger.LogInformation("  - {Name}: {Purchases} purchases", s.Name, s.TotalPurchases);

        // ─────────────────────────────────────────────────────────────────
        // 3. Any<T>(IQueryable<T>, IExpressionSpecification<T>)
        //    Checks if any element satisfies the specification.
        //    Translates to: WHERE ... LIMIT 1 / EXISTS in SQL.
        // ─────────────────────────────────────────────────────────────────
        var activeSpec = new ActiveCustomerSpecification();
        bool hasAnyActive = customers.Any(activeSpec);

        _logger.LogInformation("[Any] Any active customer exists? {R}", hasAnyActive);

        // 3b. Any with composed specification
        var vipSpec = new VipCustomerSpecification(minimumPurchases: 20);
        bool hasVipActive = customers.Any(activeSpec.And(vipSpec));

        _logger.LogInformation("[Any + And] Any active VIP (>20) exists? {R}", hasVipActive);

        // ─────────────────────────────────────────────────────────────────
        // 4. Count<T>(IQueryable<T>, IExpressionSpecification<T>)
        //    Counts elements matching the specification.
        //    Translates to: SELECT COUNT(*) WHERE ... in SQL.
        // ─────────────────────────────────────────────────────────────────
        int activeCount = customers.Count(activeSpec);
        _logger.LogInformation("[Count] Active customers: {N}", activeCount);

        // ─────────────────────────────────────────────────────────────────
        // 5. DDD Specification composition with fluent QuerySpec
        //    Typical pattern for CQRS Query Handlers.
        // ─────────────────────────────────────────────────────────────────
        var recentSpec = new RecentCustomerSpecification(daysBack: 7);
        var highCreditSpec = new HighCreditCustomerSpecification(minimumCreditLimit: 5_000m);

        // Recent high-credit customers, sorted by credit limit descending
        var recentHighCredit = QuerySpec<Customer>.Empty
            .And(recentSpec)
            .And(highCreditSpec)
            .OrderByDescending(c => c.CreditLimit)
            .Page(page: 1, pageSize: 10);

        var results = customers.Apply(recentHighCredit).ToList();
        _logger.LogInformation("[DDD Composition] Recent high-credit customers: {N}", results.Count);
        foreach (var c in results)
            _logger.LogInformation("  - {Name}: CreditLimit={CL}", c.Name, c.CreditLimit);

        // ─────────────────────────────────────────────────────────────────
        // 6. Keyset Pagination (Cursor) with Apply
        // ─────────────────────────────────────────────────────────────────
        var cursorSpec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.TotalPurchases)
            .SeekAfter(c => c.TotalPurchases, 10, take: 2);

        var cursorResults = customers.Apply(cursorSpec).ToList();
        _logger.LogInformation("[Keyset LINQ] Records with TotalPurchases > 10 (Take 2): {N}", cursorResults.Count);
        foreach (var c in cursorResults)
            _logger.LogInformation("  - {Name}: {Purchases} purchases", c.Name, c.TotalPurchases);

        // ─────────────────────────────────────────────────────────────────
        // 7. IQueryable<T> Direct Specification Extensions (Where, All, FirstOrDefault)
        // ─────────────────────────────────────────────────────────────────
        var queryableWhere = customers.Where(activeSpec).ToList();
        bool queryableAll = customers.All(activeSpec);
        var queryableFirst = customers.FirstOrDefault(vipSpec);

        _logger.LogInformation("[IQueryable Extensions] Where: {W} items | All active: {A} | FirstOrDefault VIP: {F}",
            queryableWhere.Count, queryableAll, queryableFirst?.Name ?? "None");

        // ─────────────────────────────────────────────────────────────────
        // 8. IEnumerable<T> In-Memory Specification Extensions (Where, Any, All, Count, FirstOrDefault)
        // ─────────────────────────────────────────────────────────────────
        var inMemoryList = customers.ToList();
        var memWhere = inMemoryList.Where(activeSpec).ToList();
        bool memAny = inMemoryList.Any(vipSpec);
        bool memAll = inMemoryList.All(activeSpec);
        int memCount = inMemoryList.Count(activeSpec);
        var memFirst = inMemoryList.FirstOrDefault(vipSpec);

        _logger.LogInformation("[IEnumerable Extensions] Where: {W} | Any: {A} | All: {All} | Count: {C} | First: {F}",
            memWhere.Count, memAny, memAll, memCount, memFirst?.Name ?? "None");

        return Task.CompletedTask;
    }
}



