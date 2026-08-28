// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EricksonLopez.Specification.Linq;
using EricksonLopez.Specification.PostgreSql;
using EricksonLopez.Specification.Sql;

namespace EricksonLopez.Specification.Benchmarks;

// ─────────────────────────────────────────────────────────
// Test domain model
// ─────────────────────────────────────────────────────────

public sealed class Customer
{
    public int Id { get; init; }
    public bool IsActive { get; init; }
    public decimal CreditLimit { get; init; }
    public string CountryCode { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
}

// ─────────────────────────────────────────────────────────
// Test specifications
// ─────────────────────────────────────────────────────────

public sealed class ActiveCustomerSpec : Specification<Customer>
{
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => c => c.IsActive;
}

public sealed class NotDeletedSpec : Specification<Customer>
{
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => c => !c.IsDeleted;
}

// ─────────────────────────────────────────────────────────
// Benchmarks
// ─────────────────────────────────────────────────────────

/// <summary>
/// Benchmarks comparing specification creation, composition, and evaluation
/// against manual lambdas and Ardalis.Specification.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
public class SpecificationCreationBenchmarks
{
    [Benchmark(Baseline = true, Description = "Manual lambda")]
    public Expression<Func<Customer, bool>> ManualLambda()
        => c => c.IsActive && !c.IsDeleted;

    [Benchmark(Description = "EricksonLopez: Specification.ToExpression")]
    public Expression<Func<Customer, bool>> OurSpecification()
        => new ActiveCustomerSpec().And(new NotDeletedSpec()).ToExpression();

    [Benchmark(Description = "EricksonLopez: Spec.For factory")]
    public Expression<Func<Customer, bool>> OurFactory()
        => Spec.For<Customer>(c => c.IsActive)
            .And(Spec.For<Customer>(c => !c.IsDeleted))
            .ToExpression();
}

/// <summary>
/// Benchmarks for expression composition performance.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
public class ExpressionCompositionBenchmarks
{
    private readonly Expression<Func<Customer, bool>> _left = c => c.IsActive;
    private readonly Expression<Func<Customer, bool>> _right = c => !c.IsDeleted;

    [Benchmark(Baseline = true, Description = "Manual: x => left && right")]
    public Expression<Func<Customer, bool>> ManualCombination()
    {
        var param = Expression.Parameter(typeof(Customer), "c");
        return Expression.Lambda<Func<Customer, bool>>(
            Expression.AndAlso(
                Expression.Property(param, nameof(Customer.IsActive)),
                Expression.Not(Expression.Property(param, nameof(Customer.IsDeleted)))),
            param);
    }

    [Benchmark(Description = "EricksonLopez: ExpressionComposer.And")]
    public Expression<Func<Customer, bool>> OurComposer()
        => ExpressionComposer.And(_left, _right);

    [Benchmark(Description = "EricksonLopez: 5-way AND")]
    public Expression<Func<Customer, bool>> FiveWayAnd()
    {
        var result = _left;
        result = ExpressionComposer.And(result, _right);
        result = ExpressionComposer.And(result, _left);
        result = ExpressionComposer.And(result, _right);
        result = ExpressionComposer.And(result, _left);
        return result;
    }
}

/// <summary>
/// Benchmarks for in-memory specification evaluation.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
public class SpecificationEvaluationBenchmarks
{
    private readonly Customer _validCustomer = new() { IsActive = true, IsDeleted = false, CreditLimit = 5000m };
    private readonly Specification<Customer> _spec = new ActiveCustomerSpec().And(new NotDeletedSpec());
    private readonly Func<Customer, bool> _manualPredicate = c => c.IsActive && !c.IsDeleted;

    [Benchmark(Baseline = true, Description = "Manual delegate")]
    public bool ManualDelegate()
        => _manualPredicate(_validCustomer);

    [Benchmark(Description = "EricksonLopez: IsSatisfiedBy (interpreted)")]
    public bool OurInterpreted()
        => _spec.IsSatisfiedBy(_validCustomer);

    [Benchmark(Description = "EricksonLopez: IsSatisfiedBy via compiled cache")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("AOT", "IL3050")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Trimming", "IL2026")]
    public bool OurCompiled()
        => _spec.ToCompiledPredicate()(_validCustomer);
}

/// <summary>
/// Benchmarks for SQL translation performance.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
public class SqlTranslationBenchmarks
{
    private readonly QuerySpecTranslator<Customer> _translator = new("customers");
    private readonly PostgreSqlDialect _dialect = PostgreSqlDialect.Default;
    private readonly QuerySpec<Customer> _simpleSpec;
    private readonly QuerySpec<Customer> _complexSpec;

    public SqlTranslationBenchmarks()
    {
        _simpleSpec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        _complexSpec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => !c.IsDeleted)
            .Where(c => c.CreditLimit > 1000m)
            .OrderByDescending(c => c.CreditLimit)
            .Page(1, 50);
    }

    [Benchmark(Description = "EricksonLopez: Simple spec → SQL")]
    public string SimpleSpecToSql()
    {
        var model = _translator.Translate(_simpleSpec);
        return _dialect.Render(model).Sql;
    }

    [Benchmark(Description = "EricksonLopez: Complex spec → SQL")]
    public string ComplexSpecToSql()
    {
        var model = _translator.Translate(_complexSpec);
        return _dialect.Render(model).Sql;
    }
}

/// <summary>
/// Benchmarks for applying QuerySpec to IQueryable.
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
public class QuerySpecLinqBenchmarks
{
    private readonly IQueryable<Customer> _source;
    private readonly QuerySpec<Customer> _spec;

    public QuerySpecLinqBenchmarks()
    {
        _source = Enumerable.Range(1, 1000)
            .Select(i => new Customer
            {
                Id = i,
                IsActive = i % 2 == 0,
                IsDeleted = i % 7 == 0,
                CreditLimit = i * 100m
            })
            .AsQueryable();

        _spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreditLimit)
            .Take(20);
    }

    [Benchmark(Baseline = true, Description = "Manual LINQ")]
    public int ManualLinq()
        => _source
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderByDescending(c => c.CreditLimit)
            .Take(20)
            .Count();

    [Benchmark(Description = "EricksonLopez: QuerySpec.Apply")]
    public int OurQuerySpec()
        => _source.Apply(_spec).Count();
}

/// <summary>
/// Scenario E: <see cref="ExpressionComposer.AndAll{T}(ReadOnlySpan{Expression{Func{T, bool}}})"/>
/// vs chained <c>.And()</c> calls — allocation comparison.
/// </summary>
/// <remarks>
/// Tests whether the span-based bulk composition path avoids intermediate allocations
/// compared to chaining individual .And() calls when combining multiple predicates.
/// </remarks>
[ShortRunJob]
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
public class SpanCompositionBenchmarks
{
    private readonly Expression<Func<Customer, bool>> _p1 = c => c.IsActive;
    private readonly Expression<Func<Customer, bool>> _p2 = c => !c.IsDeleted;
    private readonly Expression<Func<Customer, bool>> _p3 = c => c.CreditLimit > 100m;
    private readonly Expression<Func<Customer, bool>> _p4 = c => c.CreditLimit < 10_000m;
    private readonly Expression<Func<Customer, bool>> _p5 = c => c.CountryCode != "XX";

    [Benchmark(Baseline = true, Description = "Chained .And() x4")]
    public Expression<Func<Customer, bool>> ChainedAnd()
    {
        var result = ExpressionComposer.And(_p1, _p2);
        result = ExpressionComposer.And(result, _p3);
        result = ExpressionComposer.And(result, _p4);
        result = ExpressionComposer.And(result, _p5);
        return result;
    }

    [Benchmark(Description = "AndAll(ReadOnlySpan) x5")]
    public Expression<Func<Customer, bool>> AndAllSpan()
    {
        ReadOnlySpan<Expression<Func<Customer, bool>>> predicates =
        [
            _p1, _p2, _p3, _p4, _p5
        ];
        return ExpressionComposer.AndAll<Customer>(predicates);
    }

    [Benchmark(Description = "Chained Spec.And() x4")]
    public Expression<Func<Customer, bool>> ChainedSpecAnd()
        => new ActiveCustomerSpec()
            .And(new NotDeletedSpec())
            .And(Spec.For<Customer>(c => c.CreditLimit > 100m))
            .And(Spec.For<Customer>(c => c.CreditLimit < 10_000m))
            .ToExpression();
}




