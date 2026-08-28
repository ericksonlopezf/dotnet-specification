// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification.Showcase.Domain;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase.Levels;

/// <summary>
/// Level 2 — Complete Configuration.
/// Demonstrates the complete API of <see cref="QuerySpec{T}"/>, <see cref="QuerySpec{T,TResult}"/>,
/// and <see cref="QuerySpecExtensions"/>:
/// filters, ordering, pagination, distinct, keyset cursor pagination, and projections.
/// </summary>
public sealed class Level2_Configuration : ILevel
{
    private readonly ILogger<Level2_Configuration> _logger;

    /// <inheritdoc/>
    public string Name => "Level 2 — Complete Configuration";

    /// <inheritdoc/>
    public string Description => "Complete API of QuerySpec<T>: filters, ordering, pagination, distinct, and extensions.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level2_Configuration(ILogger<Level2_Configuration> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        // ─────────────────────────────────────────────────────────────────
        // 1. QuerySpec<T>.Empty — starting point for every query descriptor.
        //    An immutable QuerySpec with no criteria, ordering, or pagination.
        // ─────────────────────────────────────────────────────────────────
        var empty = QuerySpec<Customer>.Empty;
        _logger.LogInformation("[Empty] Criteria: {C}  Orders: {O}", empty.Criteria.Length, empty.OrderClauses.Length);

        // ─────────────────────────────────────────────────────────────────
        // 2. Where — adds AND-combined predicates.
        //    Each call to Where adds an additional criterion.
        //    All criteria are combined with AND in SQL translation.
        // ─────────────────────────────────────────────────────────────────
        var withFilter = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => c.TotalPurchases > 5);

        _logger.LogInformation("[Where] Applied criteria: {N}", withFilter.Criteria.Length);

        // ─────────────────────────────────────────────────────────────────
        // 3. QuerySpecExtensions.And — applies an existing Specification<T>
        //    as an additional filter onto a QuerySpec<T>.
        // ─────────────────────────────────────────────────────────────────
        var activeSpec = new ActiveCustomerSpecification();
        var withSpecFilter = QuerySpec<Customer>.Empty.And(activeSpec);
        _logger.LogInformation("[And ext] Criteria with spec: {N}", withSpecFilter.Criteria.Length);

        // ─────────────────────────────────────────────────────────────────
        // 4. OrderBy / OrderByDescending / ThenBy / ThenByDescending
        //    Multi-level ordering. Call sequence defines ORDER BY hierarchy.
        // ─────────────────────────────────────────────────────────────────
        var withOrdering = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.TotalPurchases)   // ORDER BY total_purchases DESC
            .ThenBy(c => c.Name)                        // , name ASC
            .ThenByDescending(c => c.CreatedAt);        // , created_at DESC

        _logger.LogInformation("[Order] Ordering clauses: {N}", withOrdering.OrderClauses.Length);

        // ─────────────────────────────────────────────────────────────────
        // 5. Skip / Take — low-level offset/limit pagination
        // ─────────────────────────────────────────────────────────────────
        var withSkipTake = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderBy(c => c.CreatedAt)
            .Skip(20)   // OFFSET 20
            .Take(10);  // LIMIT 10

        _logger.LogInformation("[Skip/Take] Skip: {S}  Take: {T}", withSkipTake.SkipCount, withSkipTake.TakeCount);

        // ─────────────────────────────────────────────────────────────────
        // 6. Page(page, pageSize) — page-number-based pagination.
        //    Page(2, 10) == Skip(10).Take(10)
        // ─────────────────────────────────────────────────────────────────
        var withPage = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Page(page: 3, pageSize: 25);  // Skip=50, Take=25

        _logger.LogInformation("[Page] Calculated Skip: {S}  Take: {T}", withPage.SkipCount, withPage.TakeCount);

        // ─────────────────────────────────────────────────────────────────
        // 7. Distinct() — eliminates duplicate results (SELECT DISTINCT)
        // ─────────────────────────────────────────────────────────────────
        var withDistinct = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Distinct();

        _logger.LogInformation("[Distinct] IsDistinct: {D}", withDistinct.IsDistinct);

        // ─────────────────────────────────────────────────────────────────
        // 8. Search(...) — multi-property search (OR-combined)
        // ─────────────────────────────────────────────────────────────────
        var withSearch = QuerySpec<Customer>.Empty
            .Search("alice", c => c.Name, c => c.Email);

        _logger.LogInformation("[Search] Added criteria for search: {N}", withSearch.Criteria.Length);

        // ─────────────────────────────────────────────────────────────────
        // 9. TagWith(string) — diagnostic query tag / SQL comment
        // ─────────────────────────────────────────────────────────────────
        var withTag = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .TagWith("GetActiveCustomersReport");

        _logger.LogInformation("[TagWith] Diagnostic tag: {Tag}", withTag.Tag);

        // ─────────────────────────────────────────────────────────────────
        // 10. Keyset / Cursor Pagination (SeekAfter, SeekBefore, WithCursor)
        // ─────────────────────────────────────────────────────────────────
        var cursorSpec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.CreatedAt)
            .SeekAfter(c => c.CreatedAt, DateTime.UtcNow.AddDays(-7), take: 10);

        var cursorBeforeSpec = QuerySpec<Customer>.Empty
            .OrderByDescending(c => c.CreatedAt)
            .SeekBefore(c => c.CreatedAt, DateTime.UtcNow.AddDays(-1), take: 10);

        _logger.LogInformation("[Keyset Cursor] SeekAfter Direction: {Dir}, Take: {Take}",
            cursorSpec.Cursor?.Direction, cursorSpec.TakeCount);
        _logger.LogInformation("[Keyset Cursor] SeekBefore Direction: {Dir}, Take: {Take}",
            cursorBeforeSpec.Cursor?.Direction, cursorBeforeSpec.TakeCount);

        // ─────────────────────────────────────────────────────────────────
        // 11. QuerySpecExtensions — inspection utilities
        //
        //     HasCriteria   → has filters?
        //     HasOrdering   → has ORDER BY clauses?
        //     HasPagination → has Skip or Take?
        // ─────────────────────────────────────────────────────────────────
        var richSpec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Take(50);

        _logger.LogInformation("[HasCriteria]   {V}", richSpec.HasCriteria());
        _logger.LogInformation("[HasOrdering]   {V}", richSpec.HasOrdering());
        _logger.LogInformation("[HasPagination] {V}", richSpec.HasPagination());

        // ─────────────────────────────────────────────────────────────────
        // 12. BuildCombinedPredicate() — combines all criteria into a
        //     single AND-composed expression tree.
        // ─────────────────────────────────────────────────────────────────
        var multiSpec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => c.TotalPurchases > 10);

        Expression<Func<Customer, bool>>? combinedPredicate = multiSpec.BuildCombinedPredicate();
        _logger.LogInformation("[BuildCombinedPredicate] Combined predicate: {P}", combinedPredicate);

        // ─────────────────────────────────────────────────────────────────
        // 13. ExpressionSimplifier.Simplify<T>() — predicate simplification.
        //     Applies constant folding: TRUE AND x → x, NOT(NOT(x)) → x, etc.
        // ─────────────────────────────────────────────────────────────────
        bool filterByActive = false; // dynamic business condition
        var conditionalSpec = filterByActive
            ? Spec.For<Customer>(c => c.IsActive)
            : Spec.True<Customer>();  // neutral identity — does not filter

        Expression<Func<Customer, bool>> composed = conditionalSpec
            .And(Spec.For<Customer>(c => c.TotalPurchases > 0))
            .ToExpression();

        var simplified = ExpressionSimplifier.Simplify(composed);
        _logger.LogInformation("[ExpressionSimplifier] Simplified expression generated.");
        _logger.LogInformation("[ExpressionSimplifier] Expr: {E}", simplified);

        // ─────────────────────────────────────────────────────────────────
        // 14. QuerySpec<T, TResult> — typed projection.
        //     Projected version adds a selector mapping T → TResult.
        // ─────────────────────────────────────────────────────────────────
        var projectedSpec = new QuerySpec<Customer, CustomerSummary>()
            .Where(c => c.IsActive)
            .Where(c => c.TotalPurchases > 5)
            .OrderByDescending(c => c.TotalPurchases)
            .Page(page: 1, pageSize: 10)
            .Select(c => new CustomerSummary(c.Id, c.Name, c.TotalPurchases));

        _logger.LogInformation("[QuerySpec<T,TResult>] Criteria: {C}  |  Order: {O}  |  Selector: {S}",
            projectedSpec.Criteria.Length,
            projectedSpec.OrderClauses.Length,
            projectedSpec.Selector is not null ? "configured" : "null");

        // QuerySpec<T,TResult>.Empty — equivalent to QuerySpec<T>.Empty
        var emptyProjected = QuerySpec<Customer, CustomerSummary>.Empty;
        _logger.LogInformation("[QuerySpec<T,TResult>.Empty] Criteria: {C}  Selector: {S}",
            emptyProjected.Criteria.Length,
            emptyProjected.Selector is null ? "null (requires .Select)" : "configured");

        // ─────────────────────────────────────────────────────────────────
        // 15. OrderClause<T> and OrderDirection
        //     Each .OrderBy/.OrderByDescending/.ThenBy/.ThenByDescending call
        //     adds an OrderClause<T> to QuerySpec.OrderClauses.
        // ─────────────────────────────────────────────────────────────────
        var orderedSpec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.Name)
            .OrderByDescending(c => c.TotalPurchases);

        foreach (var clause in orderedSpec.OrderClauses)
        {
            _logger.LogInformation("[OrderClause] Selector: {S}  Direction: {D}",
                clause.KeySelector, clause.Direction);
        }

        // OrderDirection enum: Ascending | Descending
        OrderDirection asc = OrderDirection.Ascending;
        OrderDirection desc = OrderDirection.Descending;
        _logger.LogInformation("[OrderDirection] Ascending={A}  Descending={D}", asc, desc);

        // ─────────────────────────────────────────────────────────────────
        // 16. CursorClause<T> and CursorDirection
        //     .SeekAfter(keySelector, cursorValue, take) — forward keyset pagination
        //     .SeekBefore(keySelector, cursorValue, take) — backward keyset pagination
        //     .WithCursor(cursor) — assigns a prebuilt CursorClause<T>
        // ─────────────────────────────────────────────────────────────────
        var seekSpec = QuerySpec<Customer>.Empty
            .SeekAfter(c => c.TotalPurchases, 100, take: 10);

        CursorClause<Customer>? cursor = seekSpec.Cursor;
        _logger.LogInformation("[CursorClause] Cursor configured: {C}", cursor is not null);

        if (cursor is not null)
        {
            _logger.LogInformation("[CursorClause] Direction: {D}  Value: {V}",
                cursor.Direction, cursor.Value);
        }

        // CursorDirection enum: After | Before
        CursorDirection after = CursorDirection.After;
        CursorDirection before = CursorDirection.Before;
        _logger.LogInformation("[CursorDirection] After={A}  Before={B}", after, before);

        return Task.CompletedTask;
    }
}




