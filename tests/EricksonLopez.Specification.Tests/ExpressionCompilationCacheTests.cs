// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

[CollectionDefinition("ExpressionCompilationCacheCollection", DisableParallelization = true)]
public sealed class ExpressionCompilationCacheTestCollection { }

[Collection("ExpressionCompilationCacheCollection")]
public sealed class ExpressionCompilationCacheTests : IDisposable
{
    public ExpressionCompilationCacheTests()
    {
        ExpressionCompilationCache.Clear();
    }

    public void Dispose()
    {
        ExpressionCompilationCache.Clear();
    }

    [Fact]
    public void GetOrCompile_WithNullExpression_ThrowsArgumentNullException()
    {
        var act = () => ExpressionCompilationCache.GetOrCompile<Customer>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("expression");
    }

    [Fact]
    public void GetOrCompile_WithNewExpression_CompilesAndCaches()
    {
        Expression<Func<Customer, bool>> expr = c => c.IsActive && c.Id > 5000;

        var initialCount = ExpressionCompilationCache.CachedCount;
        var compiled = ExpressionCompilationCache.GetOrCompile(expr);
        compiled.Should().NotBeNull();

        ExpressionCompilationCache.CachedCount.Should().Be(1);

        var result = compiled(new Customer { IsActive = true, Id = 5001 });
        result.Should().BeTrue();

        var falseResult = compiled(new Customer { IsActive = false, Id = 5001 });
        falseResult.Should().BeFalse();
    }

    [Fact]
    public void GetOrCompile_WithSameExpression_ReturnsCachedDelegate()
    {
        Expression<Func<Customer, bool>> expr1 = c => c.IsActive && c.Id > 6000;
        Expression<Func<Customer, bool>> expr2 = c => c.IsActive && c.Id > 6000;

        var compiled1 = ExpressionCompilationCache.GetOrCompile(expr1);
        var compiled2 = ExpressionCompilationCache.GetOrCompile(expr2);

        ReferenceEquals(compiled1, compiled2).Should().BeTrue("Should reuse the cached compiled delegate for the same structural expression");
    }

    [Fact]
    public void GetOrCompile_WithDifferentExpressions_ReturnsDifferentDelegates()
    {
        Expression<Func<Customer, bool>> expr1 = c => c.IsActive && c.CreditLimit > 100m;
        Expression<Func<Customer, bool>> expr2 = c => c.IsActive && c.CreditLimit < 50m;

        var compiled1 = ExpressionCompilationCache.GetOrCompile(expr1);
        var compiled2 = ExpressionCompilationCache.GetOrCompile(expr2);

        ReferenceEquals(compiled1, compiled2).Should().BeFalse("Different expressions must produce different delegates");

        // Verify behavioral separation
        var customer = new Customer { IsActive = true, CreditLimit = 200m };
        compiled1(customer).Should().BeTrue();
        compiled2(customer).Should().BeFalse();
    }

    [Fact]
    public void GetOrCompile_WithDifferentGenericEntityTypes_PreservesTypeSafety()
    {
        Expression<Func<Customer, bool>> customerExpr = c => c.IsActive;
        Expression<Func<Order, bool>> orderExpr = o => o.Id > 0;

        var customerDelegate = ExpressionCompilationCache.GetOrCompile(customerExpr);
        var orderDelegate = ExpressionCompilationCache.GetOrCompile(orderExpr);

        customerDelegate.Should().NotBeNull();
        orderDelegate.Should().NotBeNull();

        customerDelegate(new Customer { IsActive = true }).Should().BeTrue();
        orderDelegate(new Order { Id = 42 }).Should().BeTrue();
    }

    [Fact]
    public void GetOrCompile_UnderConcurrentAccess_IsThreadSafeAndConsistent()
    {
        Expression<Func<Customer, bool>> expr = c => c.IsActive && c.CreditLimit >= 1000m;

        var results = new Func<Customer, bool>[50];
        Parallel.For(0, 50, i =>
        {
            results[i] = ExpressionCompilationCache.GetOrCompile(expr);
        });

        var first = results[0];
        first.Should().NotBeNull();
        for (var i = 1; i < 50; i++)
        {
            ReferenceEquals(results[i], first).Should().BeTrue("All concurrent requests for the same expression must receive the identical cached delegate");
        }
    }

    [Fact]
    public async Task GetOrCompile_HighContentionConcurrentAccess_HitsDoubleCheckedLocking()
    {
        ThreadPool.GetMinThreads(out var origWorker, out var origIocp);
        try
        {
            ThreadPool.SetMinThreads(32, origIocp);
            for (var run = 0; run < 5; run++)
            {
                var id = 99000 + run;
                Expression<Func<Customer, bool>> expr = c => c.Id == id;
                using var startSignal = new ManualResetEventSlim(false);
                var tasks = Enumerable.Range(0, 12).Select(_ => Task.Run(() =>
                {
                    startSignal.Wait();
                    return ExpressionCompilationCache.GetOrCompile(expr);
                })).ToArray();

                startSignal.Set();
                var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                var expected = results[0];
                foreach (var res in results)
                {
                    ReferenceEquals(res, expected).Should().BeTrue();
                }
            }
        }
        finally
        {
            ThreadPool.SetMinThreads(origWorker, origIocp);
        }
    }

    [Fact]
    public void GetOrCompile_WithDifferentConstants_NeverMixesUpDelegates()
    {
        // 100 expressions with distinct constants
        for (var i = 0; i < 50; i++)
        {
            var threshold = i;
            var param = Expression.Parameter(typeof(Customer), "c");
            var body = Expression.GreaterThan(
                Expression.Property(param, nameof(Customer.Id)),
                Expression.Constant(threshold));
            var lambda = Expression.Lambda<Func<Customer, bool>>(body, param);

            var del = ExpressionCompilationCache.GetOrCompile(lambda);
            del(new Customer { Id = threshold + 1 }).Should().BeTrue();
            del(new Customer { Id = threshold - 1 }).Should().BeFalse();
        }
    }

    [Fact]
    public void Clear_EmptiesCache()
    {
        Expression<Func<Customer, bool>> expr = c => c.IsActive;
        ExpressionCompilationCache.GetOrCompile(expr);

        ExpressionCompilationCache.CachedCount.Should().BeGreaterThan(0);
        ExpressionCompilationCache.Clear();
        ExpressionCompilationCache.CachedCount.Should().Be(0);
    }

    [Fact]
    public void Capacity_DefaultsTo512()
    {
        ExpressionCompilationCache.Capacity.Should().Be(512);
    }

    [Fact]
    public void Capacity_WhenSetToLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => ExpressionCompilationCache.Capacity = 0;
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Capacity_WhenReduced_EvictsOldestEntries()
    {
        for (var i = 0; i < 10; i++)
        {
            var id = i;
            Expression<Func<Customer, bool>> expr = c => c.Id == id;
            ExpressionCompilationCache.GetOrCompile(expr);
        }

        ExpressionCompilationCache.CachedCount.Should().Be(10);
        ExpressionCompilationCache.Capacity = 5;
        ExpressionCompilationCache.CachedCount.Should().Be(5);

        // Verify that subsequent insertions correctly evict from the reduced LRU list without orphans
        Expression<Func<Customer, bool>> exprNew = c => c.Id == 999;
        ExpressionCompilationCache.GetOrCompile(exprNew);
        ExpressionCompilationCache.CachedCount.Should().Be(5);

        // Reset
        ExpressionCompilationCache.Capacity = 512;
    }

    [Fact]
    public void GetOrCompile_WhenCapacityReached_EvictsLruEntry()
    {
        ExpressionCompilationCache.Capacity = 2;

        Expression<Func<Customer, bool>> expr1 = c => c.Id == 1;
        Expression<Func<Customer, bool>> expr2 = c => c.Id == 2;
        Expression<Func<Customer, bool>> expr3 = c => c.Id == 3;

        var del1 = ExpressionCompilationCache.GetOrCompile(expr1);
        var del2 = ExpressionCompilationCache.GetOrCompile(expr2);
        ExpressionCompilationCache.CachedCount.Should().Be(2);

        // Access expr1 to make expr2 the LRU
        ExpressionCompilationCache.GetOrCompile(expr1);

        // Add expr3, should evict expr2
        var del3 = ExpressionCompilationCache.GetOrCompile(expr3);
        ExpressionCompilationCache.CachedCount.Should().Be(2);

        // del1 is still cached
        var del1Again = ExpressionCompilationCache.GetOrCompile(expr1);
        ReferenceEquals(del1, del1Again).Should().BeTrue();

        // Add 10 more distinct expressions and verify CachedCount stays strictly bounded at capacity 2
        for (var i = 10; i < 20; i++)
        {
            var id = i;
            Expression<Func<Customer, bool>> expr = c => c.Id == id;
            ExpressionCompilationCache.GetOrCompile(expr);
            ExpressionCompilationCache.CachedCount.Should().Be(2);
        }

        // Reset capacity
        ExpressionCompilationCache.Capacity = 512;
    }
}





