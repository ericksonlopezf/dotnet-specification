// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Specification;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.Tests;

public sealed class ConcurrencyCustomer
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

[CollectionDefinition("ConcurrencyTestCollection", DisableParallelization = true)]
public sealed class ConcurrencyTestCollection { }

[Collection("ConcurrencyTestCollection")]
public sealed class ConcurrencyAuditTests : IDisposable
{
    public ConcurrencyAuditTests()
    {
        QueryPlanCache.Clear();
        ExpressionCompilationCache.Clear();
    }

    public void Dispose()
    {
        QueryPlanCache.Clear();
        ExpressionCompilationCache.Clear();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1_000)]
    [InlineData(10_000)]
    public void ConcurrentEvaluations_ProducesConsistentResults(int iterations)
    {
        var spec = Spec.For<ConcurrencyCustomer>(c => c.IsActive && c.Id > 5);
        var valid = new ConcurrencyCustomer { Id = 10, IsActive = true };
        var invalid = new ConcurrencyCustomer { Id = 2, IsActive = true };

        Parallel.For(0, iterations, _ =>
        {
            spec.IsSatisfiedBy(valid).Should().BeTrue();
            spec.IsSatisfiedBy(invalid).Should().BeFalse();
        });
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1_000)]
    public void ConcurrentQueryPlanCache_AccessDoesNotCorruptState(int operations)
    {
        var translator = new QuerySpecTranslator<ConcurrencyCustomer>("customers");

        Parallel.For(0, operations, i =>
        {
            var spec = Spec.For<ConcurrencyCustomer>(c => c.IsActive).ToQuerySpec();
            var plan = translator.Translate(spec);
            plan.Should().NotBeNull();
            plan.TableName.Should().Be("customers");
        });

        QueryPlanCache.Count.Should().BeLessThanOrEqualTo(QueryPlanCache.Capacity);
    }
}
