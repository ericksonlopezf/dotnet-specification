// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification.Diagnostics;
using EricksonLopez.Specification.Showcase.Domain;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase.Levels;

/// <summary>
/// Level 7 — Scalability and Performance.
/// Demonstrates internal engine performance components:
/// <see cref="ExpressionHasher"/>, <see cref="ExpressionSimplifier"/>,
/// <see cref="ExpressionCompilationCache"/>, <see cref="ExpressionInterpreter"/>,
/// <see cref="ExpressionComposer"/> (AndAll/OrAny), and <see cref="SpecificationDiagnostics"/> (OTel).
/// Also includes <c>Specification&lt;T&gt;.ToCompiledPredicate()</c> for JIT evaluation.
/// </summary>
public sealed class Level7_Scalability : ILevel
{
    private readonly ILogger<Level7_Scalability> _logger;

    /// <inheritdoc/>
    public string Name => "Level 7 — Scalability and Performance";

    /// <inheritdoc/>
    public string Description => "ExpressionHasher, ExpressionSimplifier, ExpressionCompilationCache, ExpressionInterpreter, ExpressionComposer, OpenTelemetry.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level7_Scalability(ILogger<Level7_Scalability> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        // ─────────────────────────────────────────────────────────────────
        // 1. ExpressionHasher.ComputeHash
        //    Calculates a structural hash of an expression tree.
        //    Two structurally identical expressions produce the identical hash.
        // ─────────────────────────────────────────────────────────────────
        var spec = Spec.For<Customer>(c => c.IsActive && c.TotalPurchases > 10);
        var expr = spec.ToExpression();

        int hash1 = ExpressionHasher.ComputeHash(expr);
        int hash2 = ExpressionHasher.ComputeHash(expr); // identical

        _logger.LogInformation("[ExpressionHasher] Hash: {H1}  Stable across invocations: {Equal}", hash1, hash1 == hash2);

        // Different expressions → distinct hashes
        var expr2 = Spec.For<Customer>(c => c.TotalPurchases > 20).ToExpression();
        int hash3 = ExpressionHasher.ComputeHash(expr2);
        _logger.LogInformation("[ExpressionHasher] Different hash for distinct expr: {Diff}", hash1 != hash3);

        // ─────────────────────────────────────────────────────────────────
        // 2. ExpressionSimplifier
        //    Applies constant folding over the expression tree:
        //    TRUE AND x → x, FALSE OR x → x, NOT(NOT(x)) → x, etc.
        // ─────────────────────────────────────────────────────────────────
        var simplified1 = ExpressionSimplifier.Simplify(expr);
        _logger.LogInformation("[ExpressionSimplifier.Simplify] Simplified: {E}", simplified1);

        var simplifier = ExpressionSimplifier.Default;
        var simplified2 = (Expression<Func<Customer, bool>>)simplifier.Visit(expr);
        _logger.LogInformation("[ExpressionSimplifier.Default.Visit] Simplified: {E}", simplified2);

        // ─────────────────────────────────────────────────────────────────
        // 3. ExpressionCompilationCache.GetOrCompile
        //    Compiles expression to an IL delegate once per process.
        //    NOTE: RequiresDynamicCode — for JIT runtimes.
        // ─────────────────────────────────────────────────────────────────
        var compiled1 = ExpressionCompilationCache.GetOrCompile(expr);
        var compiled2 = ExpressionCompilationCache.GetOrCompile(expr); // cache hit

        _logger.LogInformation("[ExpressionCompilationCache] Cached compilations: {Count}", ExpressionCompilationCache.CachedCount);
        _logger.LogInformation("[ExpressionCompilationCache] Same delegate reused: {Same}", ReferenceEquals(compiled1, compiled2));

        var customer = new Customer { IsActive = true, TotalPurchases = 15 };
        bool compiledResult = compiled1(customer);
        _logger.LogInformation("[GetOrCompile] Compiled evaluation: {R}", compiledResult);

        // ─────────────────────────────────────────────────────────────────
        // 4. ExpressionInterpreter.Evaluate
        //    Interpreted evaluation without IL generation — 100% Native AOT safe.
        // ─────────────────────────────────────────────────────────────────
        bool interpretedResult = ExpressionInterpreter.Evaluate(expr, customer);
        _logger.LogInformation("[ExpressionInterpreter] Interpreted evaluation: {R}", interpretedResult);

        // ─────────────────────────────────────────────────────────────────
        // 5. ExpressionComposer — composition without Expression.Invoke
        // ─────────────────────────────────────────────────────────────────
        Expression<Func<Customer, bool>> activeExpr = c => c.IsActive;
        Expression<Func<Customer, bool>> vipExpr = c => c.TotalPurchases > 10;
        Expression<Func<Customer, bool>> recentExpr = c => c.CreatedAt > DateTime.UtcNow.AddDays(-30);

        var andComposed = ExpressionComposer.And(activeExpr, vipExpr);
        var orComposed = ExpressionComposer.Or(activeExpr, vipExpr);
        var notComposed = ExpressionComposer.Not(activeExpr);

        _logger.LogInformation("[ExpressionComposer.And] {E}", andComposed);
        _logger.LogInformation("[ExpressionComposer.Or ] {E}", orComposed);
        _logger.LogInformation("[ExpressionComposer.Not] {E}", notComposed);

        // AndAll — combines array of predicates with AND
        Expression<Func<Customer, bool>>[] predicates = [activeExpr, vipExpr, recentExpr];
        var andAll = ExpressionComposer.AndAll<Customer>(predicates.AsSpan());
        _logger.LogInformation("[ExpressionComposer.AndAll] {E}", andAll);

        // OrAny — combines array of predicates with OR
        var orAny = ExpressionComposer.OrAny<Customer>(predicates.AsSpan());
        _logger.LogInformation("[ExpressionComposer.OrAny] {E}", orAny);

        // ─────────────────────────────────────────────────────────────────
        // 6. SpecificationDiagnostics — Native OpenTelemetry
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[OTel] ActivitySource: {A}", SpecificationDiagnostics.ActivitySourceName);
        _logger.LogInformation("[OTel] MeterName: {M}", SpecificationDiagnostics.MeterName);

        using var activity = SpecificationDiagnostics.ActivitySource.StartActivity("Level7.Demo");
        activity?.SetTag("showcase.level", 7);
        activity?.SetTag("specification.hash", hash1);

        _logger.LogInformation("[OTel] Active Meter counters: created, composed, cache.hits, cache.misses, sql.translations");

        // ─────────────────────────────────────────────────────────────────
        // 7. ExpressionEqualityComparer and ExpressionDebugFormatter
        // ─────────────────────────────────────────────────────────────────
        bool areStructurallyEqual = ExpressionEqualityComparer.Default.Equals(expr, expr);
        string formattedExpr = ExpressionDebugFormatter.Format(expr);
        _logger.LogInformation("[ExpressionEqualityComparer] Structural equality: {Eq}", areStructurallyEqual);
        _logger.LogInformation("[ExpressionDebugFormatter] Formatted: {Fmt}", formattedExpr);

        // ─────────────────────────────────────────────────────────────────
        // 8. Specification<T>.ToCompiledPredicate()
        // ─────────────────────────────────────────────────────────────────
        var activeSpec = new ActiveCustomerSpecification();
        var testCustomer = new Customer { IsActive = true, TotalPurchases = 15 };

        bool interpretedEval = activeSpec.IsSatisfiedBy(testCustomer);
        _logger.LogInformation("[IsSatisfiedBy AOT-safe] Result: {R}", interpretedEval);

        var compiledPredicate = activeSpec.ToCompiledPredicate();
        bool compiledEval = compiledPredicate(testCustomer);
        _logger.LogInformation("[ToCompiledPredicate JIT] Result: {R} | same result: {Same}",
            compiledEval, interpretedEval == compiledEval);

        // ─────────────────────────────────────────────────────────────────
        // 9. Horizontal Scalability & Thread-Safety
        // ─────────────────────────────────────────────────────────────────
        _logger.LogInformation("[Thread-safety] QuerySpec and Specification instances are immutable and thread-safe.");

        return Task.CompletedTask;
    }
}




