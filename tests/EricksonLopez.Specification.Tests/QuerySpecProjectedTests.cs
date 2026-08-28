// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

public sealed class QuerySpecProjectedTests
{
    [Fact]
    public void Empty_HasNoConstraints()
    {
        var spec = QuerySpec<Customer, string>.Empty;
        spec.Criteria.Should().BeEmpty();
        spec.OrderClauses.Should().BeEmpty();
        spec.Selector.Should().BeNull();
        spec.SkipCount.Should().BeNull();
        spec.TakeCount.Should().BeNull();
        spec.IsDistinct.Should().BeFalse();
    }

    [Fact]
    public void Where_ReturnsNewInstance_WithPredicate()
    {
        var spec = QuerySpec<Customer, string>.Empty.Where(c => c.IsActive);
        spec.Criteria.Should().HaveCount(1);
    }

    [Fact]
    public void Where_WithNullPredicate_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Where(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Select_ReturnsNewInstance_WithSelector()
    {
        var spec = QuerySpec<Customer, string>.Empty.Select(c => c.Name);
        spec.Selector.Should().NotBeNull();
    }

    [Fact]
    public void Select_WithNullSelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Select(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OrderBy_SetsAscendingDirection()
    {
        var spec = QuerySpec<Customer, string>.Empty.OrderBy(c => c.Name);
        spec.OrderClauses.Should().HaveCount(1);
        spec.OrderClauses[0].Direction.Should().Be(OrderDirection.Ascending);
    }

    [Fact]
    public void OrderBy_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.OrderBy<string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OrderByDescending_SetsDescendingDirection()
    {
        var spec = QuerySpec<Customer, string>.Empty.OrderByDescending(c => c.Name);
        spec.OrderClauses.Should().HaveCount(1);
        spec.OrderClauses[0].Direction.Should().Be(OrderDirection.Descending);
    }

    [Fact]
    public void OrderByDescending_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.OrderByDescending<string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ThenBy_SetsAscendingDirection()
    {
        var spec = QuerySpec<Customer, string>.Empty.ThenBy(c => c.Name);
        spec.OrderClauses.Should().HaveCount(1);
        spec.OrderClauses[0].Direction.Should().Be(OrderDirection.Ascending);
    }

    [Fact]
    public void ThenBy_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.ThenBy<string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ThenByDescending_SetsDescendingDirection()
    {
        var spec = QuerySpec<Customer, string>.Empty.ThenByDescending(c => c.Name);
        spec.OrderClauses.Should().HaveCount(1);
        spec.OrderClauses[0].Direction.Should().Be(OrderDirection.Descending);
    }

    [Fact]
    public void ThenByDescending_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.ThenByDescending<string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(1, 10, 0, 10)]
    [InlineData(2, 10, 10, 10)]
    public void Page_CalculatesCorrectSkipAndTake(int page, int pageSize, int expectedSkip, int expectedTake)
    {
        var spec = QuerySpec<Customer, string>.Empty.Page(page, pageSize);
        spec.SkipCount.Should().Be(expectedSkip);
        spec.TakeCount.Should().Be(expectedTake);
    }

    [Fact]
    public void Page_WithPageLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Page(0, 10);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Page_WithPageSizeLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Page(1, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Skip_WithNegativeCount_ThrowsArgumentOutOfRangeException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Skip(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Skip_SetsSkipCount()
    {
        var spec = QuerySpec<Customer, string>.Empty.Skip(5);
        spec.SkipCount.Should().Be(5);
    }

    [Fact]
    public void Skip_WithZeroCount_Succeeds()
    {
        var spec = QuerySpec<Customer, string>.Empty.Skip(0);
        spec.SkipCount.Should().Be(0);
    }

    [Fact]
    public void Take_WithCountLessThanOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Take(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Take_WithBoundaryOne_Succeeds()
    {
        var spec = QuerySpec<Customer, string>.Empty.Take(1);
        spec.TakeCount.Should().Be(1);
    }

    [Fact]
    public void Take_SetsTakeCount()
    {
        var spec = QuerySpec<Customer, string>.Empty.Take(15);
        spec.TakeCount.Should().Be(15);
    }

    [Fact]
    public void Distinct_SetsIsDistinctTrue()
    {
        var spec = QuerySpec<Customer, string>.Empty.Distinct();
        spec.IsDistinct.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tagging
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TagWith_SetsTag()
    {
        var spec = QuerySpec<Customer, string>.Empty.TagWith("ProjectedTag");
        spec.Tag.Should().Be("ProjectedTag");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TagWith_InvalidTag_ThrowsArgumentException(string? invalidTag)
    {
        var act = () => QuerySpec<Customer, string>.Empty.TagWith(invalidTag!);
        act.Should().Throw<ArgumentException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Keyset pagination & Cursor
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SeekAfter_And_SeekBefore_SetsCursorProperly()
    {
        var afterSpec = QuerySpec<Customer, string>.Empty.SeekAfter(c => c.Id, 100, 10);
        afterSpec.Cursor.Should().NotBeNull();
        afterSpec.Cursor!.Value.Should().Be(100);
        afterSpec.Cursor!.Direction.Should().Be(CursorDirection.After);
        afterSpec.TakeCount.Should().Be(10);
        afterSpec.Cursor.KeySelector.Compile()(new Customer { Id = 100 }).Should().Be(100);

        var beforeSpec = QuerySpec<Customer, string>.Empty.SeekBefore(c => c.Id, 200, 5);
        beforeSpec.Cursor.Should().NotBeNull();
        beforeSpec.Cursor!.Value.Should().Be(200);
        beforeSpec.Cursor!.Direction.Should().Be(CursorDirection.Before);
        beforeSpec.TakeCount.Should().Be(5);
        beforeSpec.Cursor.KeySelector.Compile()(new Customer { Id = 200 }).Should().Be(200);
    }

    [Fact]
    public void WithCursor_WithNullKeySelector_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.WithCursor<int>(null!, 10, CursorDirection.After, 5);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void WithCursor_WithInvalidTake_ThrowsArgumentOutOfRangeException(int invalidTake)
    {
        var act = () => QuerySpec<Customer, string>.Empty.WithCursor(c => c.Id, 10, CursorDirection.After, invalidTake);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithCursor_WithBoundaryTake_Succeeds()
    {
        var spec = QuerySpec<Customer, string>.Empty.WithCursor(c => c.Id, 10, CursorDirection.After, 1);
        spec.TakeCount.Should().Be(1);
        spec.SkipCount.Should().BeNull();
    }

    [Fact]
    public void WithCursor_SetsPropertiesAndClearsSkipCount()
    {
        var spec = QuerySpec<Customer, string>.Empty
            .Skip(10)
            .WithCursor(c => c.Id, 42, CursorDirection.Before, 20);

        spec.Cursor.Should().NotBeNull();
        spec.Cursor!.Value.Should().Be(42);
        spec.Cursor.Direction.Should().Be(CursorDirection.Before);
        spec.TakeCount.Should().Be(20);
        spec.SkipCount.Should().BeNull();
    }

    [Fact]
    public void OrderClauses_KeySelectors_CompileAndEvaluateCorrectly()
    {
        var spec = QuerySpec<Customer, string>.Empty
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

    [Fact]
    public void Select_EvaluatesProjectionCorrectly()
    {
        var spec = QuerySpec<Customer, string>.Empty.Select(c => c.Name.ToUpperInvariant());
        spec.Selector.Should().NotBeNull();
        var fn = spec.Selector!.Compile();
        fn(new Customer { Name = "alice" }).Should().Be("ALICE");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Search
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Search_WithNullSearchPhrase_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Search(null!, c => c.Name);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Search_WithNullPropertySelectors_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Search("test", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Search_WithEmptyPropertySelectors_ThrowsArgumentException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Search("test");
        act.Should().Throw<ArgumentException>()
            .WithMessage("*At least one property selector must be provided.*");
    }

    [Fact]
    public void Search_WithNullFirstSelectorInArray_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Search("test", new System.Linq.Expressions.Expression<Func<Customer, string?>>[] { null! });
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Search_WithNullSelectorInArray_ThrowsArgumentNullException()
    {
        var act = () => QuerySpec<Customer, string>.Empty.Search("test", c => c.Name, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Search_WithSingleSelector_AddsCriteria()
    {
        var spec = QuerySpec<Customer, string>.Empty.Search("Alice", c => c.Name);
        spec.Criteria.Should().HaveCount(1);

        var compiled = spec.Criteria[0].Compile();
        compiled(new Customer { Name = "Alice Smith" }).Should().BeTrue();
        compiled(new Customer { Name = "Bob" }).Should().BeFalse();
    }

    [Fact]
    public void Search_WithMultipleSelectors_CombinesWithOr()
    {
        var spec = QuerySpec<Customer, string>.Empty.Search("test", c => c.Name, c => c.CountryCode);
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

        var spec = QuerySpec<Customer, string>.Empty.Search("vip", sel1, sel2, sel3);
        spec.Criteria.Should().HaveCount(1);

        var fn = spec.Criteria[0].Compile();
        fn(new Customer { Name = "vip customer", CountryCode = "US" }).Should().BeTrue();
        fn(new Customer { Name = "regular", CountryCode = "vip-country" }).Should().BeTrue();
        fn(new Customer { Name = "a vip user", CountryCode = "US" }).Should().BeTrue();
        fn(new Customer { Name = "regular", CountryCode = "US" }).Should().BeFalse();
    }
}



