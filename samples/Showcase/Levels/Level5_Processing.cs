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
/// Level 5 — Processing.
/// Demonstrates using specifications in processing scenarios:
/// batch processing, pagination, conditional filtering with
/// <see cref="Spec.True{T}()"/>/<see cref="Spec.False{T}()"/>, concurrency, and cancellation.
/// </summary>
public sealed class Level5_Processing : ILevel
{
    private readonly ILogger<Level5_Processing> _logger;

    /// <inheritdoc/>
    public string Name => "Level 5 — Processing";

    /// <inheritdoc/>
    public string Description => "Batch processing with Page(), conditional filtering with Spec.True/False, concurrency, and CancellationToken.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level5_Processing(ILogger<Level5_Processing> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        // ─────────────────────────────────────────────────────────────────
        // Simulated test data — large dataset for batch processing.
        // ─────────────────────────────────────────────────────────────────
        var allCustomers = Enumerable.Range(1, 50).Select(i => new Customer
        {
            Name = $"Customer-{i:D3}",
            IsActive = i % 5 != 0,  // 80% active
            TotalPurchases = i * 2,
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        }).AsQueryable();

        // ─────────────────────────────────────────────────────────────────
        // 1. Batch Processing with Page(page, pageSize)
        //    Paging through large datasets prevents high memory allocation.
        // ─────────────────────────────────────────────────────────────────
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var cancellationToken = cts.Token;

        const int pageSize = 10;
        int page = 1;
        int processed = 0;

        var baseSpec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name);

        _logger.LogInformation("[Batch] Starting batch processing in pages of {PageSize}...", pageSize);

        while (!cancellationToken.IsCancellationRequested)
        {
            var batchSpec = baseSpec.Page(page, pageSize);
            var batch = allCustomers.Apply(batchSpec).ToList();

            if (batch.Count == 0) break;

            processed += batch.Count;
            _logger.LogInformation("[Batch] Page {P}: {N} records processed (total: {T})", page, batch.Count, processed);

            await Task.Delay(50, cancellationToken);  // simulate I/O workload
            page++;
        }

        _logger.LogInformation("[Batch] Processing completed. Total: {Total}", processed);

        // ─────────────────────────────────────────────────────────────────
        // 2. Conditional filtering with Spec.True<T>()
        //    Spec.True<T>() is the neutral identity element for AND:
        //      any.And(Spec.True<T>()) == any (no filtering effect)
        // ─────────────────────────────────────────────────────────────────
        bool filterByActive = true;   // from request parameters
        bool filterByVip = false;

        Specification<Customer> activeFilter = filterByActive
            ? new ActiveCustomerSpecification()
            : Spec.True<Customer>();  // does not filter

        Specification<Customer> vipFilter = filterByVip
            ? new VipCustomerSpecification(minimumPurchases: 10)
            : Spec.True<Customer>();  // does not filter

        var dynamicQuery = QuerySpec<Customer>.Empty
            .And(activeFilter)
            .And(vipFilter)
            .OrderBy(c => c.Name)
            .Take(5);

        var dynamicResult = allCustomers.Apply(dynamicQuery).ToList();
        _logger.LogInformation("[Conditional] Active filters: isActive={A} isVip={V}  Results: {N}",
            filterByActive, filterByVip, dynamicResult.Count);

        // ─────────────────────────────────────────────────────────────────
        // 3. Spec.False<T>() — neutral identity element for OR.
        //    Useful for building dynamic OR chains.
        //    any.Or(Spec.False<T>()) == any (no effect on the result)
        // ─────────────────────────────────────────────────────────────────
        var segments = new List<string> { "Alice", "Bob" };

        Specification<Customer> nameFilter = Spec.False<Customer>(); // neutral base

        foreach (var segment in segments)
        {
            var segmentName = segment;
            nameFilter = nameFilter.Or(Spec.For<Customer>(c => c.Name.Contains(segmentName)));
        }

        var segmentQuery = QuerySpec<Customer>.Empty.And(nameFilter);
        _logger.LogInformation("[Or-chain] Spec.False + dynamic OR: criteria={N}", segmentQuery.Criteria.Length);

        // ─────────────────────────────────────────────────────────────────
        // 4. Concurrent processing — specifications are immutable and thread-safe.
        // ─────────────────────────────────────────────────────────────────
        var sharedSpec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => c.TotalPurchases > 0)
            .OrderByDescending(c => c.TotalPurchases)
            .Take(5);

        using var cts2 = new CancellationTokenSource();

        var tasks = Enumerable.Range(1, 3).Select(workerIndex =>
            Task.Run(async () =>
            {
                var results = allCustomers.Apply(sharedSpec).ToList();
                await Task.Delay(10);
                _logger.LogInformation("[Concurrency] Worker {W}: {N} customers processed", workerIndex, results.Count);
            }, cts2.Token));

        await Task.WhenAll(tasks);
        _logger.LogInformation("[Concurrency] All tasks completed. QuerySpec is thread-safe.");
    }
}



