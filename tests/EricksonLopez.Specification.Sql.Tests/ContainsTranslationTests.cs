// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Specification.MsSql;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.Sql.Tests;

/// <summary>
/// Tests for the Enumerable.Contains / ICollection.Contains translation to SQL IN predicates.
/// </summary>
[Collection("QueryPlanCacheCollection")]
public sealed class ContainsTranslationTests : IDisposable
{
    private readonly QuerySpecTranslator<Customer> _translator;
    private readonly MsSqlDialect _dialect = MsSqlDialect.Default;

    public ContainsTranslationTests()
    {
        QueryPlanCache.Clear();
        _translator = new QuerySpecTranslator<Customer>("customers");
    }

    public void Dispose() => QueryPlanCache.Clear();

    [Fact]
    public void ListContains_TranslatesToInPredicateNode()
    {
        var ids = new List<int> { 1, 2, 3 };
        var spec = QuerySpec<Customer>.Empty.Where(c => ids.Contains(c.Id));
        var model = _translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        var inNode = model.Filters[0].Should().BeOfType<InPredicateNode>().Subject;
        inNode.ColumnName.Should().Be("id");
        inNode.Negated.Should().BeFalse();
    }

    [Fact]
    public void ArrayContains_TranslatesToInPredicateNode()
    {
        var ids = new[] { 10, 20, 30 };
        var spec = QuerySpec<Customer>.Empty.Where(c => ids.Contains(c.Id));
        var model = _translator.Translate(spec);

        model.Filters[0].Should().BeOfType<InPredicateNode>();
    }

    [Fact]
    public void HashSetContains_TranslatesToInPredicateNode()
    {
        var codes = new HashSet<string> { "US", "CA", "MX" };
        var spec = QuerySpec<Customer>.Empty.Where(c => codes.Contains(c.CountryCode));
        var model = _translator.Translate(spec);

        model.Filters[0].Should().BeOfType<InPredicateNode>();
    }

    [Fact]
    public void EnumerableContains_StaticCall_TranslatesToInPredicateNode()
    {
        var ids = new List<int> { 5, 6, 7 };
        var spec = QuerySpec<Customer>.Empty.Where(c => System.Linq.Enumerable.Contains(ids, c.Id));
        var model = _translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        var inNode = model.Filters[0].Should().BeOfType<InPredicateNode>().Subject;
        inNode.ColumnName.Should().Be("id");
    }

    [Fact]
    public void Contains_NonMemberItem_ThrowsNotSupportedException()
    {
        var ids = new List<int> { 1, 2, 3 };
        var spec = QuerySpec<Customer>.Empty.Where(c => ids.Contains(42));
        var act = () => _translator.Translate(spec);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Contains() translation requires a direct member access as the item argument*");
    }

    [Fact]
    public void Contains_NonParameterMemberItem_ThrowsNotSupportedException()
    {
        var ids = new List<int> { 1, 2, 3 };
        var external = new { Id = 10 };
        var spec = QuerySpec<Customer>.Empty.Where(c => ids.Contains(external.Id));
        var act = () => _translator.Translate(spec);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Cannot determine column name from Contains() item argument*");
    }
}
