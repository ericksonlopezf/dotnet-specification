// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.Sql.Tests;

[CollectionDefinition("QueryPlanCacheCollection", DisableParallelization = true)]
public sealed class QueryPlanCacheTestCollection { }

internal sealed class CacheTestCustomer
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

[Collection("QueryPlanCacheCollection")]
public sealed class QueryPlanCacheTests : IDisposable
{
    public QueryPlanCacheTests()
    {
        QueryPlanCache.Clear();
        QueryPlanCache.Capacity = 512;
    }

    public void Dispose()
    {
        QueryPlanCache.Clear();
        QueryPlanCache.Capacity = 512;
    }

    [Fact]
    public void Cache_Clear_RemovesAllItems()
    {
        Expression<Func<CacheTestCustomer, bool>> expr = c => c.Id == 1;
        var model = new QueryModel { TableName = "t" };

        QueryPlanCache.SetPlan(expr, "t", model);
        QueryPlanCache.TryGetPlan(expr, "t", out _).Should().BeTrue();
        QueryPlanCache.Count.Should().Be(1);

        QueryPlanCache.Clear();
        QueryPlanCache.TryGetPlan(expr, "t", out _).Should().BeFalse();
        QueryPlanCache.Count.Should().Be(0);
    }

    [Fact]
    public void Cache_TryGetPlan_NullArguments_Throws()
    {
        Expression<Func<CacheTestCustomer, bool>> expr = c => c.Id == 1;

        var act1 = () => QueryPlanCache.TryGetPlan(null!, "t", out _);
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => QueryPlanCache.TryGetPlan(expr, null!, out _);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Cache_SetPlan_NullArguments_Throws()
    {
        Expression<Func<CacheTestCustomer, bool>> expr = c => c.Id == 1;
        var model = new QueryModel { TableName = "t" };

        var act1 = () => QueryPlanCache.SetPlan(null!, "t", model);
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => QueryPlanCache.SetPlan(expr, null!, model);
        act2.Should().Throw<ArgumentNullException>();

        var act3 = () => QueryPlanCache.SetPlan(expr, "t", null!);
        act3.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Cache_EvictsOldestItem_WhenCapacityExceeded()
    {
        QueryPlanCache.Clear();
        QueryPlanCache.Capacity = 3;

        Expression<Func<CacheTestCustomer, bool>> expr1 = c => c.Id == 1;
        Expression<Func<CacheTestCustomer, bool>> expr2 = c => c.Id == 2;
        Expression<Func<CacheTestCustomer, bool>> expr3 = c => c.Id == 3;
        Expression<Func<CacheTestCustomer, bool>> expr4 = c => c.Id == 4;

        QueryPlanCache.SetPlan(expr1, "t", new QueryModel { TableName = "t1" });
        QueryPlanCache.SetPlan(expr2, "t", new QueryModel { TableName = "t2" });
        QueryPlanCache.SetPlan(expr3, "t", new QueryModel { TableName = "t3" });

        QueryPlanCache.Count.Should().Be(3);

        // Adding 4th item should evict expr1 (the least recently used)
        QueryPlanCache.SetPlan(expr4, "t", new QueryModel { TableName = "t4" });

        QueryPlanCache.Count.Should().Be(3);
        QueryPlanCache.TryGetPlan(expr1, "t", out _).Should().BeFalse("expr1 was oldest and should be evicted");
        QueryPlanCache.TryGetPlan(expr2, "t", out _).Should().BeTrue();
        QueryPlanCache.TryGetPlan(expr3, "t", out _).Should().BeTrue();
        QueryPlanCache.TryGetPlan(expr4, "t", out _).Should().BeTrue();
    }

    [Fact]
    public void Cache_TryGetPlan_PromotesItemToMRU()
    {
        QueryPlanCache.Clear();
        QueryPlanCache.Capacity = 3;

        Expression<Func<CacheTestCustomer, bool>> expr1 = c => c.Id == 1;
        Expression<Func<CacheTestCustomer, bool>> expr2 = c => c.Id == 2;
        Expression<Func<CacheTestCustomer, bool>> expr3 = c => c.Id == 3;
        Expression<Func<CacheTestCustomer, bool>> expr4 = c => c.Id == 4;

        QueryPlanCache.SetPlan(expr1, "t", new QueryModel { TableName = "t1" });
        QueryPlanCache.SetPlan(expr2, "t", new QueryModel { TableName = "t2" });
        QueryPlanCache.SetPlan(expr3, "t", new QueryModel { TableName = "t3" });

        // Access expr1 -> promotes it to most recently used
        QueryPlanCache.TryGetPlan(expr1, "t", out _).Should().BeTrue();

        // Adding 4th item should now evict expr2 (since expr1 was refreshed)
        QueryPlanCache.SetPlan(expr4, "t", new QueryModel { TableName = "t4" });

        QueryPlanCache.TryGetPlan(expr1, "t", out _).Should().BeTrue("expr1 was accessed and promoted");
        QueryPlanCache.TryGetPlan(expr2, "t", out _).Should().BeFalse("expr2 became the oldest and should be evicted");
        QueryPlanCache.TryGetPlan(expr3, "t", out _).Should().BeTrue();
        QueryPlanCache.TryGetPlan(expr4, "t", out _).Should().BeTrue();
    }

    [Fact]
    public void Cache_ShrinkingCapacity_EvictsExcessEntries()
    {
        QueryPlanCache.Clear();
        Expression<Func<CacheTestCustomer, bool>> expr1 = c => c.Id == 1;
        Expression<Func<CacheTestCustomer, bool>> expr2 = c => c.Id == 2;
        Expression<Func<CacheTestCustomer, bool>> expr3 = c => c.Id == 3;

        QueryPlanCache.SetPlan(expr1, "t", new QueryModel { TableName = "t1" });
        QueryPlanCache.SetPlan(expr2, "t", new QueryModel { TableName = "t2" });
        QueryPlanCache.SetPlan(expr3, "t", new QueryModel { TableName = "t3" });

        QueryPlanCache.Count.Should().Be(3);

        QueryPlanCache.Capacity = 1;
        QueryPlanCache.Count.Should().Be(1);
        QueryPlanCache.TryGetPlan(expr3, "t", out _).Should().BeTrue("Latest item remains");

        // Verify that subsequent insertions correctly evict from the reduced LRU list without orphans
        Expression<Func<CacheTestCustomer, bool>> expr4 = c => c.Id == 4;
        QueryPlanCache.SetPlan(expr4, "t", new QueryModel { TableName = "t4" });
        QueryPlanCache.Count.Should().Be(1);
        QueryPlanCache.TryGetPlan(expr3, "t", out _).Should().BeFalse("expr3 was evicted by expr4");
        QueryPlanCache.TryGetPlan(expr4, "t", out _).Should().BeTrue("expr4 is cached");
    }

    [Fact]
    public void Cache_InvalidCapacity_Throws()
    {
        var act = () => QueryPlanCache.Capacity = 0;
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Cache_ConcurrentAccess_IsThreadSafe()
    {
        QueryPlanCache.Clear();
        Parallel.For(0, 100, i =>
        {
            var param = Expression.Parameter(typeof(CacheTestCustomer), "c");
            var body = Expression.Equal(Expression.Property(param, nameof(CacheTestCustomer.Id)), Expression.Constant(i));
            var lambda = Expression.Lambda<Func<CacheTestCustomer, bool>>(body, param);

            QueryPlanCache.SetPlan(lambda, "customers", new QueryModel { TableName = "customers" });
            QueryPlanCache.TryGetPlan(lambda, "customers", out _);
        });

        QueryPlanCache.Count.Should().BeGreaterThan(0);
        QueryPlanCache.Count.Should().BeLessThanOrEqualTo(QueryPlanCache.Capacity);
    }

    [Fact]
    public void Cache_SetPlan_UpdatesExistingItem_AndPromotesToMRU()
    {
        QueryPlanCache.Clear();
        QueryPlanCache.Capacity = 2;

        Expression<Func<CacheTestCustomer, bool>> expr1 = c => c.Id == 1;
        Expression<Func<CacheTestCustomer, bool>> expr2 = c => c.Id == 2;
        Expression<Func<CacheTestCustomer, bool>> expr3 = c => c.Id == 3;

        var plan1 = new QueryModel { TableName = "t1" };
        var plan1Updated = new QueryModel { TableName = "t1_updated" };
        var plan2 = new QueryModel { TableName = "t2" };
        var plan3 = new QueryModel { TableName = "t3" };

        QueryPlanCache.SetPlan(expr1, "t", plan1);
        QueryPlanCache.SetPlan(expr2, "t", plan2);

        // expr1 is now second (not first). Updating it should update value and promote to MRU.
        QueryPlanCache.SetPlan(expr1, "t", plan1Updated);

        // Adding expr3 should evict expr2 (LRU), keeping expr1
        QueryPlanCache.SetPlan(expr3, "t", plan3);

        QueryPlanCache.TryGetPlan(expr1, "t", out var result1).Should().BeTrue();
        result1!.TableName.Should().Be("t1_updated");

        QueryPlanCache.TryGetPlan(expr2, "t", out _).Should().BeFalse("expr2 was evicted");
        QueryPlanCache.TryGetPlan(expr3, "t", out _).Should().BeTrue();
    }

    [Fact]
    public void CacheKey_EqualityAndHashing_CoversAllCombinations()
    {
        Expression<Func<CacheTestCustomer, bool>> expr1 = c => c.Id == 1;
        Expression<Func<CacheTestCustomer, bool>> expr2 = c => c.Id == 2;

        var model1 = new QueryModel { TableName = "customers" };

        QueryPlanCache.SetPlan(expr1, "customers", model1);

        QueryPlanCache.TryGetPlan(expr1, "customers", out var res1).Should().BeTrue();
        res1.Should().BeSameAs(model1);

        // Same criteria, different table -> must return false
        QueryPlanCache.TryGetPlan(expr1, "orders", out _).Should().BeFalse();

        // Different criteria, same table -> must return false
        QueryPlanCache.TryGetPlan(expr2, "customers", out _).Should().BeFalse();
    }

    [Fact]
    public void SetPlan_UpdatingExistingEntry_DoesNotIncreaseCountOrDuplicateNodes()
    {
        QueryPlanCache.Clear();
        QueryPlanCache.Capacity = 3;

        Expression<Func<CacheTestCustomer, bool>> expr1 = c => c.Id == 1;
        var plan1 = new QueryModel { TableName = "t1" };
        var plan2 = new QueryModel { TableName = "t2" };

        QueryPlanCache.SetPlan(expr1, "t", plan1);
        QueryPlanCache.Count.Should().Be(1);

        // Update existing plan
        QueryPlanCache.SetPlan(expr1, "t", plan2);
        QueryPlanCache.Count.Should().Be(1, "Updating an existing plan must not increase cache count");

        // Add 2 more items up to capacity 3
        Expression<Func<CacheTestCustomer, bool>> expr2 = c => c.Id == 2;
        Expression<Func<CacheTestCustomer, bool>> expr3 = c => c.Id == 3;
        QueryPlanCache.SetPlan(expr2, "t", plan1);
        QueryPlanCache.SetPlan(expr3, "t", plan1);
        QueryPlanCache.Count.Should().Be(3);

        // Adding 4th item should evict oldest
        Expression<Func<CacheTestCustomer, bool>> expr4 = c => c.Id == 4;
        QueryPlanCache.SetPlan(expr4, "t", plan1);
        QueryPlanCache.Count.Should().Be(3);
    }

    [Fact]
    public void Cache_Eviction_ProperlyRemovesFromLruList()
    {
        QueryPlanCache.Clear();
        QueryPlanCache.Capacity = 2;

        Expression<Func<CacheTestCustomer, bool>> expr1 = c => c.Id == 1;
        Expression<Func<CacheTestCustomer, bool>> expr2 = c => c.Id == 2;
        Expression<Func<CacheTestCustomer, bool>> expr3 = c => c.Id == 3;
        Expression<Func<CacheTestCustomer, bool>> expr4 = c => c.Id == 4;

        QueryPlanCache.SetPlan(expr1, "t", new QueryModel { TableName = "t1" });
        QueryPlanCache.SetPlan(expr2, "t", new QueryModel { TableName = "t2" });
        QueryPlanCache.SetPlan(expr3, "t", new QueryModel { TableName = "t3" });
        QueryPlanCache.SetPlan(expr4, "t", new QueryModel { TableName = "t4" });

        QueryPlanCache.Count.Should().Be(2);
        QueryPlanCache.TryGetPlan(expr1, "t", out _).Should().BeFalse();
        QueryPlanCache.TryGetPlan(expr2, "t", out _).Should().BeFalse();
        QueryPlanCache.TryGetPlan(expr3, "t", out _).Should().BeTrue();
        QueryPlanCache.TryGetPlan(expr4, "t", out _).Should().BeTrue();
    }

    [Fact]
    public void SetPlan_UpdatingExistingEntry_WhenCacheAtCapacity_DoesNotEvictOtherEntries()
    {
        QueryPlanCache.Clear();
        QueryPlanCache.Capacity = 2;

        Expression<Func<CacheTestCustomer, bool>> expr1 = c => c.Id == 1;
        Expression<Func<CacheTestCustomer, bool>> expr2 = c => c.Id == 2;

        var plan1 = new QueryModel { TableName = "t1" };
        var plan2 = new QueryModel { TableName = "t2" };
        var plan1Updated = new QueryModel { TableName = "t1_updated" };

        QueryPlanCache.SetPlan(expr1, "t", plan1);
        QueryPlanCache.SetPlan(expr2, "t", plan2);

        // Cache is at capacity (2). Updating expr1 should NOT evict expr2!
        QueryPlanCache.SetPlan(expr1, "t", plan1Updated);

        QueryPlanCache.Count.Should().Be(2);
        QueryPlanCache.TryGetPlan(expr1, "t", out var res1).Should().BeTrue();
        res1!.TableName.Should().Be("t1_updated");

        QueryPlanCache.TryGetPlan(expr2, "t", out _).Should().BeTrue("expr2 must NOT be evicted when updating existing entry expr1");
    }

    [Fact]
    public void CacheKey_ObjectEquals_Works()
    {
        System.Linq.Expressions.Expression<Func<CacheTestCustomer, bool>> expr = c => c.Id == 1;
        var key1 = new QueryPlanCache.CacheKey(expr, "t");
        var key2 = new QueryPlanCache.CacheKey(expr, "t");
        var keyDifferent = new QueryPlanCache.CacheKey(expr, "other");

        key1.Equals(key2).Should().BeTrue();
        key1.Equals((object)key2).Should().BeTrue();
        key1.Equals(keyDifferent).Should().BeFalse();
        key1.Equals((object)keyDifferent).Should().BeFalse();
        key1.Equals((object)"string").Should().BeFalse();
        key1.Equals(null).Should().BeFalse();
        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }

    [Fact]
    public void Capacity_Shrinking_EvictsLruItems()
    {
        QueryPlanCache.Clear();
        QueryPlanCache.Capacity = 4;

        System.Linq.Expressions.Expression<Func<CacheTestCustomer, bool>> expr1 = c => c.Id == 1;
        System.Linq.Expressions.Expression<Func<CacheTestCustomer, bool>> expr2 = c => c.Id == 2;
        System.Linq.Expressions.Expression<Func<CacheTestCustomer, bool>> expr3 = c => c.Id == 3;
        System.Linq.Expressions.Expression<Func<CacheTestCustomer, bool>> expr4 = c => c.Id == 4;

        QueryPlanCache.SetPlan(expr1, "t", new QueryModel { TableName = "t1" });
        QueryPlanCache.SetPlan(expr2, "t", new QueryModel { TableName = "t2" });
        QueryPlanCache.SetPlan(expr3, "t", new QueryModel { TableName = "t3" });
        QueryPlanCache.SetPlan(expr4, "t", new QueryModel { TableName = "t4" });

        QueryPlanCache.Count.Should().Be(4);

        // Shrink capacity to 2
        QueryPlanCache.Capacity = 2;

        QueryPlanCache.Count.Should().Be(2);
        QueryPlanCache.TryGetPlan(expr1, "t", out _).Should().BeFalse();
        QueryPlanCache.TryGetPlan(expr2, "t", out _).Should().BeFalse();
        QueryPlanCache.TryGetPlan(expr3, "t", out _).Should().BeTrue();
        QueryPlanCache.TryGetPlan(expr4, "t", out _).Should().BeTrue();
    }

    [Fact]
    public void Translate_ZeroParameters_CacheHit_ReturnsCachedPlan()
    {
        QueryPlanCache.Clear();
        var translator = new QuerySpecTranslator<CacheTestCustomer>("Customers");
        var spec = QuerySpec<CacheTestCustomer>.Empty.Where(c => c.Name == null);

        var plan1 = translator.Translate(spec);
        plan1.Parameters.Should().BeEmpty();

        var plan2 = translator.Translate(spec);
        plan2.Should().BeSameAs(plan1);
    }

    [Fact]
    public void Translate_SameParameters_CacheHit_ReturnsCachedPlan()
    {
        QueryPlanCache.Clear();
        var translator = new QuerySpecTranslator<CacheTestCustomer>("Customers");
        var id = 42;
        var spec1 = QuerySpec<CacheTestCustomer>.Empty.Where(c => c.Id == id);

        var plan1 = translator.Translate(spec1);
        plan1.Parameters.Should().HaveCount(1);
        plan1.Parameters[0].Value.Should().Be(42);

        var spec2 = QuerySpec<CacheTestCustomer>.Empty.Where(c => c.Id == id);
        var plan2 = translator.Translate(spec2);
        plan2.Should().BeSameAs(plan1);
    }

    [Fact]
    public void Translate_DifferentParameterValue_CacheHit_ReparameterizesPlan()
    {
        QueryPlanCache.Clear();
        var translator = new QuerySpecTranslator<CacheTestCustomer>("Customers");
        var id1 = 42;
        var spec1 = QuerySpec<CacheTestCustomer>.Empty.Where(c => c.Id == id1);

        var plan1 = translator.Translate(spec1);
        plan1.Parameters[0].Value.Should().Be(42);

        var id2 = 99;
        var spec2 = QuerySpec<CacheTestCustomer>.Empty.Where(c => c.Id == id2);
        var plan2 = translator.Translate(spec2);

        plan2.Parameters[0].Value.Should().Be(99);
        plan2.Should().NotBeSameAs(plan1);
    }

    [Fact]
    public void Translate_DifferentParameterName_CacheHit_ReparameterizesPlan()
    {
        QueryPlanCache.Clear();
        var translator = new QuerySpecTranslator<CacheTestCustomer>("Customers");
        var id1 = 42;
        var spec1 = QuerySpec<CacheTestCustomer>.Empty.Where(c => c.Id == id1);

        var plan1 = translator.Translate(spec1);

        var cacheKey = spec1.Criteria[0];
        QueryPlanCache.TryGetPlan(cacheKey, "Customers", out var cached);
        var corrupted = cached! with { Parameters = [new SqlParameter("corrupted", 42)] };
        QueryPlanCache.SetPlan(cacheKey, "Customers", corrupted);

        var plan2 = translator.Translate(spec1);
        plan2.Parameters[0].Name.Should().Be("p1");
        plan2.Parameters[0].Value.Should().Be(42);
    }
}






