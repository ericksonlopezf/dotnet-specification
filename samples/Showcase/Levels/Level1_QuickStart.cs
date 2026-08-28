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
/// Level 1 — Quick Start.
/// Demonstrates the creation, evaluation, and basic usage of specifications,
/// covering: <see cref="Specification{T}"/>, <see cref="Spec"/>,
/// <see cref="ISpecification{T}"/>, and <see cref="IExpressionSpecification{T}"/>.
/// </summary>
public sealed class Level1_QuickStart : ILevel
{
    private readonly ILogger<Level1_QuickStart> _logger;

    /// <inheritdoc/>
    public string Name => "Level 1 — Quick Start";

    /// <inheritdoc/>
    public string Description => "Creation, evaluation, and basic in-memory composition of specifications.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level1_QuickStart(ILogger<Level1_QuickStart> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        // ─────────────────────────────────────────────────────────────────
        // 1. Subclassing Specification<T>
        //    Canonical DDD approach: one class per business rule.
        //    ActiveCustomerSpecification is defined in Domain/Specifications.cs
        // ─────────────────────────────────────────────────────────────────
        var activeSpec = new ActiveCustomerSpecification();

        var alice = new Customer { Name = "Alice", IsActive = true, TotalPurchases = 15 };
        var bob = new Customer { Name = "Bob", IsActive = false, TotalPurchases = 2 };

        // ISpecification<T>.IsSatisfiedBy — in-memory evaluation (AOT-safe).
        bool aliceIsActive = activeSpec.IsSatisfiedBy(alice); // true
        bool bobIsActive = activeSpec.IsSatisfiedBy(bob);   // false

        _logger.LogInformation("[IsSatisfiedBy] Alice active: {A}  |  Bob active: {B}", aliceIsActive, bobIsActive);

        // ─────────────────────────────────────────────────────────────────
        // 2. Spec.For<T> — inline specification created from a lambda.
        //    Useful for one-off rules; prefer subclassing for reusable rules.
        // ─────────────────────────────────────────────────────────────────
        var vipSpec = Spec.For<Customer>(c => c.TotalPurchases > 10);

        _logger.LogInformation("[Spec.For] Alice is VIP: {A}  |  Bob is VIP: {B}",
            vipSpec.IsSatisfiedBy(alice),
            vipSpec.IsSatisfiedBy(bob));

        // ─────────────────────────────────────────────────────────────────
        // 3. Spec.True<T>() / Spec.False<T>()
        //    Neutral identity elements for conditional composition.
        //    • True is the identity for And: spec.And(Spec.True<T>()) == spec
        //    • False is the identity for Or: spec.Or(Spec.False<T>()) == spec
        // ─────────────────────────────────────────────────────────────────
        var alwaysTrue = Spec.True<Customer>();
        var alwaysFalse = Spec.False<Customer>();

        _logger.LogInformation("[Spec.True ] Alice satisfies True : {R}", alwaysTrue.IsSatisfiedBy(alice));
        _logger.LogInformation("[Spec.False] Alice satisfies False: {R}", alwaysFalse.IsSatisfiedBy(alice));

        // ─────────────────────────────────────────────────────────────────
        // 4. IExpressionSpecification<T>.ToExpression() and ToDebugString()
        //    Retrieves the underlying LINQ expression tree and readable string format.
        // ─────────────────────────────────────────────────────────────────
        Expression<Func<Customer, bool>> expr = activeSpec.ToExpression();
        string debugStr = activeSpec.ToDebugString();
        _logger.LogInformation("[ToExpression]  Expression: {Expr}", expr);
        _logger.LogInformation("[ToDebugString] Readable format: {Debug}", debugStr);

        // ─────────────────────────────────────────────────────────────────
        // 5. Boolean composition: And / Or / Not
        //    Specification<T> provides these combinators directly.
        // ─────────────────────────────────────────────────────────────────
        var activeAndVip = activeSpec.And(vipSpec);         // AND
        var activeOrVip = activeSpec.Or(vipSpec);          // OR
        var notActive = activeSpec.Not();                // NOT (NegatedSpecification)

        _logger.LogInformation("[And] Alice (active && vip): {R}", activeAndVip.IsSatisfiedBy(alice));
        _logger.LogInformation("[Or ] Bob  (active || vip) : {R}", activeOrVip.IsSatisfiedBy(bob));
        _logger.LogInformation("[Not] Alice NOT active     : {R}", notActive.IsSatisfiedBy(alice));

        // ─────────────────────────────────────────────────────────────────
        // 6. Spec.All<T> and Spec.Any<T> — Multi-specification composition
        // ─────────────────────────────────────────────────────────────────
        var highCreditSpec = new HighCreditCustomerSpecification(5_000m);
        var allSpec = Spec.All(activeSpec, vipSpec, highCreditSpec);
        var anySpec = Spec.Any(vipSpec, highCreditSpec);

        _logger.LogInformation("[Spec.All] Satisfies all: {R}", allSpec.IsSatisfiedBy(alice));
        _logger.LogInformation("[Spec.Any] Satisfies any: {R}", anySpec.IsSatisfiedBy(alice));

        // ─────────────────────────────────────────────────────────────────
        // 7. Spec.Between, Spec.Search, and Spec.FullText
        // ─────────────────────────────────────────────────────────────────
        var betweenSpec = Spec.Between<Customer, int>(c => c.TotalPurchases, 10, 50);
        var searchSpec = Spec.Search<Customer>("Ali", c => c.Name, c => c.Email);
        var fullTextSpec = Spec.FullText<Customer>(c => c.Name, "Alice");

        _logger.LogInformation("[Spec.Between] Alice purchases in [10,50]: {R}", betweenSpec.IsSatisfiedBy(alice));
        _logger.LogInformation("[Spec.Search]  Alice matches 'Ali': {R}", searchSpec.IsSatisfiedBy(alice));
        _logger.LogInformation("[Spec.FullText] Alice matches 'Alice': {R}", fullTextSpec.IsSatisfiedBy(alice));

        // ─────────────────────────────────────────────────────────────────
        // 8. Extension Methods: Between, InRange, ContainedByRange, MatchesFullText
        // ─────────────────────────────────────────────────────────────────
        bool isBetween = alice.TotalPurchases.Between(10, 20);         // BetweenExtensions
        bool isInRange = alice.TotalPurchases.InRange(10, 20);         // RangeExtensions
        bool isContained = alice.TotalPurchases.ContainedByRange(10, 20); // RangeExtensions
        bool matchesFt = alice.Name.MatchesFullText("Ali");            // FullTextExtensions

        _logger.LogInformation("[Extensions] Between: {B}, InRange: {IR}, Contained: {C}, FullText: {FT}",
            isBetween, isInRange, isContained, matchesFt);

        // ─────────────────────────────────────────────────────────────────
        // 9. Implicit and explicit conversion to QuerySpec<T>
        //    ToQuerySpec() — creates a QuerySpec with only the specification predicate.
        //    ToQuerySpec(QuerySpec<T>) — combines the specification with
        //      additional query modifiers (sorting, pagination, etc.).
        // ─────────────────────────────────────────────────────────────────
        QuerySpec<Customer> querySpec = activeSpec; // implicit operator: Specification<T> → QuerySpec<T>
        _logger.LogInformation("[ImplicitCast] QuerySpec created from ActiveCustomerSpec. Criteria: {N}", querySpec.Criteria.Length);

        // ToQuerySpec() without parameters — equivalent to QuerySpec<T>.Empty.Where(spec.ToExpression())
        QuerySpec<Customer> querySpec2 = activeSpec.ToQuerySpec();

        // ToQuerySpec(QuerySpec<T>) — combines the spec with an existing QuerySpec base (ordering, paging, etc.)
        QuerySpec<Customer> querySpec3 = activeSpec.ToQuerySpec(
            QuerySpec<Customer>.Empty.OrderBy(c => c.Name).Page(1, 10));

        _logger.LogInformation("[ToQuerySpec ]         Criteria in spec2: {N2}", querySpec2.Criteria.Length);
        _logger.LogInformation("[ToQuerySpec overload] spec3 has order: {O}  |  pagination: {P}",
            !querySpec3.OrderClauses.IsEmpty, querySpec3.TakeCount.HasValue);

        return Task.CompletedTask;
    }
}




