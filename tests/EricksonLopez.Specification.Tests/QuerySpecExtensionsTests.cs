// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Tests for <see cref="QuerySpecExtensions"/> and related specification/QuerySpec bridging methods.
/// </summary>
public sealed class QuerySpecExtensionsTests
{
    [Fact]
    public void BuildCombinedPredicate_NullQuerySpec_ThrowsArgumentNullException()
    {
        var act = () => QuerySpecExtensions.BuildCombinedPredicate<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BuildCombinedPredicate_NoCriteria_ReturnsNull()
    {
        var result = QuerySpec<Customer>.Empty.BuildCombinedPredicate();
        result.Should().BeNull();
    }

    [Fact]
    public void BuildCombinedPredicate_WithCriteria_ReturnsCombinedExpression()
    {
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => c.CreditLimit > 100m);

        var combined = spec.BuildCombinedPredicate();
        combined.Should().NotBeNull();
        combined!.Compile()(new Customer { IsActive = true, CreditLimit = 200m }).Should().BeTrue();
        combined.Compile()(new Customer { IsActive = false, CreditLimit = 200m }).Should().BeFalse();
    }

    [Fact]
    public void HasOrdering_NullQuerySpec_ThrowsArgumentNullException()
    {
        var act = () => QuerySpecExtensions.HasOrdering<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasOrdering_IdentifiesOrderingCorrectly()
    {
        var empty = QuerySpec<Customer>.Empty;
        empty.HasOrdering().Should().BeFalse();

        var withOrdering = empty.OrderBy(c => c.Name);
        withOrdering.HasOrdering().Should().BeTrue();
    }

    [Fact]
    public void HasPagination_NullQuerySpec_ThrowsArgumentNullException()
    {
        var act = () => QuerySpecExtensions.HasPagination<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasPagination_SkipAndTakeCombinations()
    {
        var empty = QuerySpec<Customer>.Empty;
        empty.HasPagination().Should().BeFalse();

        var withSkip = empty.Skip(10);
        withSkip.HasPagination().Should().BeTrue();

        var withTake = empty.Take(20);
        withTake.HasPagination().Should().BeTrue();

        var withBoth = empty.Skip(10).Take(20);
        withBoth.HasPagination().Should().BeTrue();
    }

    [Fact]
    public void HasCriteria_NullQuerySpec_ThrowsArgumentNullException()
    {
        var act = () => QuerySpecExtensions.HasCriteria<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasCriteria_IdentifiesCriteriaCorrectly()
    {
        var empty = QuerySpec<Customer>.Empty;
        empty.HasCriteria().Should().BeFalse();

        var withCriteria = empty.Where(c => c.IsActive);
        withCriteria.HasCriteria().Should().BeTrue();
    }

    [Fact]
    public void And_NullQuerySpec_ThrowsArgumentNullException()
    {
        var act = () => QuerySpecExtensions.And(null!, new ActiveCustomerSpecification());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void And_NullSpecification_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.And(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void And_ValidSpecification_AddsPredicate()
    {
        var querySpec = QuerySpec<Customer>.Empty;
        var spec = new ActiveCustomerSpecification();

        var result = querySpec.And(spec);
        result.Criteria.Length.Should().Be(1);
        result.HasCriteria().Should().BeTrue();
    }

    [Fact]
    public void ToQuerySpec_FromSpecification_CreatesValidQuerySpec()
    {
        var spec = new ActiveCustomerSpecification();
        var querySpec = spec.ToQuerySpec();

        querySpec.Should().NotBeNull();
        querySpec.Criteria.Length.Should().Be(1);
        querySpec.HasCriteria().Should().BeTrue();
    }

    [Fact]
    public void ToQuerySpec_WithBaseQuerySpec_CombinesCriteria()
    {
        var qs = new QuerySpec<Customer>().OrderBy(c => c.Name);
        var spec = new ActiveCustomerSpecification();
        var combined = spec.ToQuerySpec(qs);

        combined.Criteria.Should().HaveCount(1);
        combined.HasOrdering().Should().BeTrue();
    }
}



