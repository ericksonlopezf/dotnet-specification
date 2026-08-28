// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Tests for <see cref="QuerySpec{T}"/> — the immutable query descriptor.
/// </summary>
public sealed class QuerySpecTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // Immutability
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Where_ReturnsNewInstance()
    {
        var original = QuerySpec<Customer>.Empty;
        var modified = original.Where(c => c.IsActive);

        ReferenceEquals(original, modified).Should().BeFalse();
        original.Criteria.Should().BeEmpty("original should be unchanged");
        modified.Criteria.Should().HaveCount(1);
    }

    [Fact]
    public void OrderBy_ReturnsNewInstance()
    {
        var original = QuerySpec<Customer>.Empty;
        var modified = original.OrderBy(c => c.Name);

        ReferenceEquals(original, modified).Should().BeFalse();
        original.OrderClauses.Should().BeEmpty();
        modified.OrderClauses.Should().HaveCount(1);
    }

    [Fact]
    public void Take_ReturnsNewInstance()
    {
        var original = QuerySpec<Customer>.Empty;
        var modified = original.Take(10);

        ReferenceEquals(original, modified).Should().BeFalse();
        original.TakeCount.Should().BeNull();
        modified.TakeCount.Should().Be(10);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Chaining
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ChainedOperations_AllAreAccumulated()
    {
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .OrderByDescending(c => c.CreditLimit)
            .Skip(10)
            .Take(20);

        spec.Criteria.Should().HaveCount(2);
        spec.OrderClauses.Should().HaveCount(2);
        spec.SkipCount.Should().Be(10);
        spec.TakeCount.Should().Be(20);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Pagination
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 10, 0, 10)]
    [InlineData(2, 10, 10, 10)]
    [InlineData(3, 25, 50, 25)]
    [InlineData(1, 100, 0, 100)]
    public void Page_CalculatesCorrectSkipAndTake(int page, int pageSize, int expectedSkip, int expectedTake)
    {
        var spec = QuerySpec<Customer>.Empty.Page(page, pageSize);

        spec.SkipCount.Should().Be(expectedSkip);
        spec.TakeCount.Should().Be(expectedTake);
    }

    [Fact]
    public void Page_WithPageLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => QuerySpec<Customer>.Empty.Page(0, 10);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Page_WithPageSizeLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => QuerySpec<Customer>.Empty.Page(1, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Skip_WithNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        var act = () => QuerySpec<Customer>.Empty.Skip(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Take_WithCountLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => QuerySpec<Customer>.Empty.Take(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Ordering direction
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OrderBy_SetsAscendingDirection()
    {
        var spec = QuerySpec<Customer>.Empty.OrderBy(c => c.Name);
        spec.OrderClauses[0].Direction.Should().Be(OrderDirection.Ascending);
    }

    [Fact]
    public void OrderByDescending_SetsDescendingDirection()
    {
        var spec = QuerySpec<Customer>.Empty.OrderByDescending(c => c.Name);
        spec.OrderClauses[0].Direction.Should().Be(OrderDirection.Descending);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Flags
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Distinct_SetsIsDistinctTrue()
    {
        var spec = QuerySpec<Customer>.Empty.Distinct();
        spec.IsDistinct.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Null guards
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Where_WithNullPredicate_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.Where(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OrderBy_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.OrderBy<string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Extensions
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void HasCriteria_EmptySpec_ReturnsFalse()
    {
        QuerySpec<Customer>.Empty.HasCriteria().Should().BeFalse();
    }

    [Fact]
    public void HasCriteria_WithPredicate_ReturnsTrue()
    {
        QuerySpec<Customer>.Empty.Where(c => c.IsActive).HasCriteria().Should().BeTrue();
    }

    [Fact]
    public void HasOrdering_EmptySpec_ReturnsFalse()
    {
        QuerySpec<Customer>.Empty.HasOrdering().Should().BeFalse();
    }

    [Fact]
    public void HasOrdering_WithOrder_ReturnsTrue()
    {
        QuerySpec<Customer>.Empty.OrderBy(c => c.Name).HasOrdering().Should().BeTrue();
    }

    [Fact]
    public void HasPagination_EmptySpec_ReturnsFalse()
    {
        QuerySpec<Customer>.Empty.HasPagination().Should().BeFalse();
    }

    [Fact]
    public void HasPagination_WithTake_ReturnsTrue()
    {
        QuerySpec<Customer>.Empty.Take(10).HasPagination().Should().BeTrue();
    }

    [Fact]
    public void BuildCombinedPredicate_EmptySpec_ReturnsNull()
    {
        QuerySpec<Customer>.Empty.BuildCombinedPredicate().Should().BeNull();
    }

    [Fact]
    public void BuildCombinedPredicate_SingleCriteria_ReturnsIt()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        spec.BuildCombinedPredicate().Should().NotBeNull();
    }

    [Fact]
    public void BuildCombinedPredicate_MultipleCriteria_CombinesWithAnd()
    {
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => !c.IsDeleted);

        var combined = spec.BuildCombinedPredicate()!;

        // Should match only active and not-deleted
        combined.Compile()(new Customer { IsActive = true, IsDeleted = false }).Should().BeTrue();
        combined.Compile()(new Customer { IsActive = true, IsDeleted = true }).Should().BeFalse();
        combined.Compile()(new Customer { IsActive = false, IsDeleted = false }).Should().BeFalse();
    }

    [Fact]
    public void ThenBy_SetsAscendingDirection()
    {
        var spec = QuerySpec<Customer>.Empty.ThenBy(c => c.Name);
        spec.OrderClauses[0].Direction.Should().Be(OrderDirection.Ascending);
    }

    [Fact]
    public void ThenByDescending_SetsDescendingDirection()
    {
        var spec = QuerySpec<Customer>.Empty.ThenByDescending(c => c.Name);
        spec.OrderClauses[0].Direction.Should().Be(OrderDirection.Descending);
    }



    [Fact]
    public void OrderByDescending_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.OrderByDescending<string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ThenBy_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.ThenBy<string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ThenByDescending_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.ThenByDescending<string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tagging
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TagWith_SetsTag()
    {
        var spec = QuerySpec<Customer>.Empty.TagWith("UserQuery");
        spec.Tag.Should().Be("UserQuery");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TagWith_InvalidTag_ThrowsArgumentException(string? invalidTag)
    {
        var act = () => QuerySpec<Customer>.Empty.TagWith(invalidTag!);
        act.Should().Throw<ArgumentException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Keyset pagination & Cursor
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SeekAfter_SetsCursorClauseCorrectly()
    {
        var spec = QuerySpec<Customer>.Empty.SeekAfter(c => c.Id, 100, 25);
        spec.Cursor.Should().NotBeNull();
        spec.Cursor!.Direction.Should().Be(CursorDirection.After);
        spec.Cursor.Value.Should().Be(100);
        spec.TakeCount.Should().Be(25);
        spec.SkipCount.Should().BeNull();
        spec.Cursor.KeySelector.Compile()(new Customer { Id = 100 }).Should().Be(100);
    }

    [Fact]
    public void SeekBefore_SetsCursorClauseCorrectly()
    {
        var spec = QuerySpec<Customer>.Empty.SeekBefore(c => c.Id, 50, 10);
        spec.Cursor.Should().NotBeNull();
        spec.Cursor!.Direction.Should().Be(CursorDirection.Before);
        spec.Cursor.Value.Should().Be(50);
        spec.TakeCount.Should().Be(10);
        spec.SkipCount.Should().BeNull();
        spec.Cursor.KeySelector.Compile()(new Customer { Id = 50 }).Should().Be(50);
    }

    [Fact]
    public void WithCursor_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.WithCursor<int>(null!, 10, CursorDirection.After, 5);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void WithCursor_WithInvalidTake_ThrowsArgumentOutOfRangeException(int invalidTake)
    {
        var act = () => QuerySpec<Customer>.Empty.WithCursor(c => c.Id, 10, CursorDirection.After, invalidTake);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithCursor_WithBoundaryTake_Succeeds()
    {
        var spec = QuerySpec<Customer>.Empty.WithCursor(c => c.Id, 10, CursorDirection.After, 1);
        spec.TakeCount.Should().Be(1);
        spec.SkipCount.Should().BeNull();
    }

    [Fact]
    public void Skip_WithZeroCount_Succeeds()
    {
        var spec = QuerySpec<Customer>.Empty.Skip(0);
        spec.SkipCount.Should().Be(0);
    }

    [Fact]
    public void Take_WithBoundaryOne_Succeeds()
    {
        var spec = QuerySpec<Customer>.Empty.Take(1);
        spec.TakeCount.Should().Be(1);
    }

    [Fact]
    public void OrderClauses_KeySelectors_CompileAndEvaluateCorrectly()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.Name)
            .ThenBy(c => c.CreditLimit)
            .ThenByDescending(c => c.Id);

        spec.OrderClauses.Should().HaveCount(3);
        spec.OrderClauses[0].Direction.Should().Be(OrderDirection.Ascending);
        spec.OrderClauses[1].Direction.Should().Be(OrderDirection.Ascending);
        spec.OrderClauses[2].Direction.Should().Be(OrderDirection.Descending);

        var cust = new Customer { Name = "Alice", CreditLimit = 500m, Id = 42 };
        spec.OrderClauses[0].KeySelector.Compile()(cust).Should().Be("Alice");
        spec.OrderClauses[1].KeySelector.Compile()(cust).Should().Be(500m);
        spec.OrderClauses[2].KeySelector.Compile()(cust).Should().Be(42);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Search
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Search_WithNullSearchPhrase_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.Search(null!, c => c.Name);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Search_WithNullPropertySelectors_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.Search("test", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Search_WithEmptyPropertySelectors_ThrowsArgumentException()
    {
        var act = () => QuerySpec<Customer>.Empty.Search("test");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one property selector must be provided.*");
    }

    [Fact]
    public void Search_WithNullFirstSelectorInArray_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.Search("test", new System.Linq.Expressions.Expression<Func<Customer, string?>>[] { null! });
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Search_WithNullSelectorInArray_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer>.Empty.Search("test", c => c.Name, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Search_WithSingleSelector_AddsCriteria()
    {
        var spec = QuerySpec<Customer>.Empty.Search("Alice", c => c.Name);
        spec.Criteria.Should().HaveCount(1);

        var compiled = spec.Criteria[0].Compile();
        compiled(new Customer { Name = "Alice Smith" }).Should().BeTrue();
        compiled(new Customer { Name = "Bob" }).Should().BeFalse();
    }

    [Fact]
    public void Search_WithMultipleSelectors_CombinesWithOr()
    {
        var spec = QuerySpec<Customer>.Empty.Search("test", c => c.Name, c => c.CountryCode);
        spec.Criteria.Should().HaveCount(1);

        var compiled = spec.Criteria[0].Compile();
        compiled(new Customer { Name = "test user", CountryCode = "other" }).Should().BeTrue();
        compiled(new Customer { Name = "someone", CountryCode = "test" }).Should().BeTrue();
        compiled(new Customer { Name = "someone", CountryCode = "other" }).Should().BeFalse();
    }

    [Fact]
    public void Search_WithDistinctParametersAndNestedExpressions_RewritesCorrectly()
    {
        System.Linq.Expressions.Expression<Func<Customer, string?>> sel1 = a => a.Name;
        System.Linq.Expressions.Expression<Func<Customer, string?>> sel2 = b => b.CountryCode;
        System.Linq.Expressions.Expression<Func<Customer, string?>> sel3 = d => d.Name.Count(c => c != ' ') > 0 ? d.Name : null;

        var spec = QuerySpec<Customer>.Empty.Search("vip", sel1, sel2, sel3);
        spec.Criteria.Should().HaveCount(1);

        var fn = spec.Criteria[0].Compile();
        fn(new Customer { Name = "vip customer", CountryCode = "US" }).Should().BeTrue();
        fn(new Customer { Name = "regular", CountryCode = "vip-country" }).Should().BeTrue();
        fn(new Customer { Name = "a vip user", CountryCode = "US" }).Should().BeTrue();
        fn(new Customer { Name = "regular", CountryCode = "US" }).Should().BeFalse();
    }
}

public sealed class CursorClauseTests
{
    [Fact]
    public void Constructor_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => new CursorClause<Customer>(null!, 10, CursorDirection.After);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var clause = new CursorClause<Customer>(c => c.Id, 42, CursorDirection.Before);
        clause.KeySelector.Should().NotBeNull();
        clause.Value.Should().Be(42);
        clause.Direction.Should().Be(CursorDirection.Before);

        var nullValClause = new CursorClause<Customer>(c => c.Name, null, CursorDirection.After);
        nullValClause.Value.Should().BeNull();
        nullValClause.Direction.Should().Be(CursorDirection.After);
    }
}

public sealed class OrderClauseTests
{
    [Fact]
    public void Constructor_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => new OrderClause<Customer>(null!, OrderDirection.Ascending);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var clause = new OrderClause<Customer>(c => c.Name, OrderDirection.Descending);
        clause.KeySelector.Should().NotBeNull();
        clause.Direction.Should().Be(OrderDirection.Descending);
    }
}



