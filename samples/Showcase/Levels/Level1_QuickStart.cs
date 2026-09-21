// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
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
        string formattedViaRegistry = ExpressionDebugFormatterRegistry.Format(expr);
        _logger.LogInformation("[ToExpression]  Expression: {Expr}", expr);
        _logger.LogInformation("[ToDebugString] Readable format: {Debug}", debugStr);
        _logger.LogInformation("[RegistryFormat] Formatted via registry: {Formatted}", formattedViaRegistry);

        // ─────────────────────────────────────────────────────────────────
        // 5. Boolean composition: And / Or / Not & C# Language Operators
        //    Specification<T> provides combinator methods and operator overloads:
        //    &, |, !, and short-circuiting && and || via operator true/false.
        // ─────────────────────────────────────────────────────────────────
        var activeAndVip = activeSpec.And(vipSpec);         // AND method
        var activeOrVip = activeSpec.Or(vipSpec);          // OR method
        var notActive = activeSpec.Not();                // NOT (NegatedSpecification)

        // C# Language Operators & Named Alternates
        var opAnd = activeSpec & vipSpec;                // operator &
        var opOr = activeSpec | vipSpec;                 // operator |
        var opNot = !activeSpec;                         // operator !
        var opShortAnd = activeSpec && vipSpec;          // short-circuit && (via operator false)
        var opShortOr = activeSpec || vipSpec;           // short-circuit || (via operator true)

        var bitwiseAndSpec = Specification<Customer>.BitwiseAnd(activeSpec, vipSpec);
        var bitwiseOrSpec = Specification<Customer>.BitwiseOr(activeSpec, vipSpec);
        var logicalNotSpec = Specification<Customer>.LogicalNot(activeSpec);

        _logger.LogInformation("[Operators] op&: {A}, op|: {O}, op!: {N}, op&&: {SA}, op||: {SO}",
            opAnd.IsSatisfiedBy(alice), opOr.IsSatisfiedBy(alice), opNot.IsSatisfiedBy(alice),
            opShortAnd.IsSatisfiedBy(alice), opShortOr.IsSatisfiedBy(alice));
        _logger.LogInformation("[And] Alice (active && vip): {R} | BitwiseAnd: {B}", activeAndVip.IsSatisfiedBy(alice), bitwiseAndSpec.IsSatisfiedBy(alice));
        _logger.LogInformation("[Or ] Bob  (active || vip) : {R} | BitwiseOr: {B}", activeOrVip.IsSatisfiedBy(bob), bitwiseOrSpec.IsSatisfiedBy(bob));
        _logger.LogInformation("[Not] Alice NOT active     : {R} | LogicalNot: {B}", notActive.IsSatisfiedBy(alice), logicalNotSpec.IsSatisfiedBy(alice));

        // ─────────────────────────────────────────────────────────────────
        // 6. Spec.All<T> and Spec.Any<T> — Multi-specification composition
        //    Supports both params arrays and IEnumerable<Specification<T>> collections.
        // ─────────────────────────────────────────────────────────────────
        var highCreditSpec = new HighCreditCustomerSpecification(5_000m);
        var allSpec = Spec.All(activeSpec, vipSpec, highCreditSpec);
        var anySpec = Spec.Any(vipSpec, highCreditSpec);

        // IEnumerable overloads
        var specCollection = new List<Specification<Customer>> { activeSpec, vipSpec, highCreditSpec };
        var allFromEnumerable = Spec.All(specCollection);
        var anyFromEnumerable = Spec.Any(specCollection);

        _logger.LogInformation("[Spec.All] Satisfies all (params): {R} | (IEnumerable): {E}", allSpec.IsSatisfiedBy(alice), allFromEnumerable.IsSatisfiedBy(alice));
        _logger.LogInformation("[Spec.Any] Satisfies any (params): {R} | (IEnumerable): {E}", anySpec.IsSatisfiedBy(alice), anyFromEnumerable.IsSatisfiedBy(alice));

        // ─────────────────────────────────────────────────────────────────
        // 7. Spec.Between, Spec.InRange, Spec.Search, Spec.FullText, Spec.MatchesFullText
        //    Between supports both non-nullable and nullable struct properties.
        //    Spec.InRange<T,TProperty> is a direct alias for Spec.Between — same semantics.
        //    Spec.MatchesFullText<T> is a direct alias for Spec.FullText — same semantics.
        // ─────────────────────────────────────────────────────────────────
        var betweenSpec = Spec.Between<Customer, int>(c => c.TotalPurchases, 10, 50);
        alice.DiscountRate = 0.15m;
        var nullableBetweenSpec = Spec.Between<Customer, decimal>(c => c.DiscountRate, 0.05m, 0.20m);

        // Spec.InRange<T,TProperty> — alias for Between, prefer when semantics are "inclusive range containment".
        var inRangeSpec = Spec.InRange<Customer, int>(c => c.TotalPurchases, 10, 50);
        _logger.LogInformation("[Spec.InRange] Same as Between — Alice in [10,50]: {R}", inRangeSpec.IsSatisfiedBy(alice));

        var searchSpec = Spec.Search<Customer>("Ali", c => c.Name, c => c.Email);
        var fullTextSpec = Spec.FullText<Customer>(c => c.Name, "Alice");

        // Spec.MatchesFullText<T> — alias for FullText (case-sensitive Contains).
        var matchesFullTextSpec = Spec.MatchesFullText<Customer>(c => c.Name, "Ali");
        _logger.LogInformation("[Spec.MatchesFullText] Alias for FullText — Alice matches 'Ali': {R}", matchesFullTextSpec.IsSatisfiedBy(alice));

        _logger.LogInformation("[Spec.Between] Alice purchases in [10,50]: {R}", betweenSpec.IsSatisfiedBy(alice));
        _logger.LogInformation("[Spec.Between (Nullable)] Alice discount in [0.05, 0.20]: {R}", nullableBetweenSpec.IsSatisfiedBy(alice));
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




