// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Specification.Linq;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Tests for <see cref="QuerySpecLinqExtensions"/> — applying QuerySpec to IQueryable.
/// Uses in-memory LINQ as a test provider (validates expression correctness without a database).
/// </summary>
public sealed class QuerySpecLinqExtensionsTests
{
    private readonly IQueryable<Customer> _customers = new[]
    {
        new Customer { Id = 1, Name = "Alice", IsActive = true, IsDeleted = false, CreditLimit = 5000m, CountryCode = "US" },
        new Customer { Id = 2, Name = "Bob", IsActive = false, IsDeleted = false, CreditLimit = 1000m, CountryCode = "UK" },
        new Customer { Id = 3, Name = "Charlie", IsActive = true, IsDeleted = true, CreditLimit = 2000m, CountryCode = "US" },
        new Customer { Id = 4, Name = "Diana", IsActive = true, IsDeleted = false, CreditLimit = 10000m, CountryCode = "CA" },
        new Customer { Id = 5, Name = "Eve", IsActive = true, IsDeleted = false, CreditLimit = 500m, CountryCode = "US" }
    }.AsQueryable();

    // ──────────────────────────────────────────────────────────────────────────
    // Filtering
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_WithSingleWhere_FiltersCorrectly()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(4); // Alice, Charlie, Diana, Eve
        result.All(c => c.IsActive).Should().BeTrue();
    }

    [Fact]
    public void Apply_WithMultipleWhere_CombinesAsAnd()
    {
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => !c.IsDeleted);

        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(3); // Alice, Diana, Eve (Charlie is deleted)
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Ordering
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_WithOrderBy_OrdersAscending()
    {
        var spec = QuerySpec<Customer>.Empty.OrderBy(c => c.CreditLimit);
        var result = _customers.Apply(spec).ToList();

        result[0].CreditLimit.Should().Be(500m); // Eve
        result[^1].CreditLimit.Should().Be(10000m); // Diana
    }

    [Fact]
    public void Apply_WithOrderByDescending_OrdersDescending()
    {
        var spec = QuerySpec<Customer>.Empty.OrderByDescending(c => c.CreditLimit);
        var result = _customers.Apply(spec).ToList();

        result[0].CreditLimit.Should().Be(10000m); // Diana first
    }

    [Fact]
    public void Apply_WithMultipleOrderings_AppliesOrderByAndThenBy()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.Name);

        var result = _customers.Apply(spec).ToList();

        // Active customers first, then ordered by name
        result[0].Name.Should().Be("Alice");
        result[1].Name.Should().Be("Charlie");
    }

    [Fact]
    public void Apply_WithMultipleOrderings_AppliesOrderByAndThenByDescending()
    {
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive) // Alice, Charlie, Diana, Eve
            .OrderBy(c => c.IsDeleted) // False (Alice, Diana, Eve), True (Charlie)
            .ThenByDescending(c => c.CreditLimit); // For False: Diana (10000), Alice (5000), Eve (500)

        var result = _customers.Apply(spec).ToList();

        result[0].Name.Should().Be("Diana");
        result[1].Name.Should().Be("Alice");
        result[2].Name.Should().Be("Eve");
        result[3].Name.Should().Be("Charlie");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Pagination
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_WithTake_LimitsResults()
    {
        var spec = QuerySpec<Customer>.Empty.Take(2);
        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public void Apply_WithSkip_SkipsResults()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.Id)
            .Skip(2);

        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(3);
        result[0].Id.Should().Be(3);
    }

    [Fact]
    public void Apply_WithPage_PaginatesCorrectly()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.Id)
            .Page(2, 2); // Page 2 of 2-per-page

        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(4);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Combined
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_ComplexSpec_FiltersOrdersAndPaginatesCorrectly()
    {
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreditLimit)
            .Take(2);

        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(2);
        result[0].CreditLimit.Should().Be(10000m); // Diana
        result[1].CreditLimit.Should().Be(5000m);  // Alice
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Distinct
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_WithDistinct_ReturnsDistinctResults()
    {
        var alice = new Customer { Id = 1, Name = "Alice" };
        var duplicateCustomers = new[] { alice, alice }.AsQueryable();

        // Test non-projected Distinct
        var spec = QuerySpec<Customer>.Empty.Distinct();
        var result = duplicateCustomers.Apply(spec).ToList();

        result.Should().HaveCount(1);
    }

    [Fact]
    public void ApplyProjected_WithDistinct_ReturnsDistinctResults()
    {
        var alice = new Customer { Id = 1, Name = "Alice" };
        var duplicateCustomers = new[] { alice, alice }.AsQueryable();

        var projectedSpec = QuerySpec<Customer, string>.Empty.Distinct().Select(c => c.Name);
        var projectedResult = duplicateCustomers.Apply(projectedSpec).ToList();

        projectedResult.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Domain spec integration
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_DomainSpecConvertedToQuerySpec_WorksCorrectly()
    {
        var domainSpec = new ActiveCustomerSpecification();
        QuerySpec<Customer> querySpec = domainSpec; // implicit conversion

        var result = _customers.Apply(querySpec).ToList();

        result.Should().HaveCount(4);
        result.All(c => c.IsActive).Should().BeTrue();
    }

    [Fact]
    public void Apply_EmptySpec_ReturnsAllResults()
    {
        var result = _customers.Apply(QuerySpec<Customer>.Empty).ToList();
        result.Should().HaveCount(5);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Projected QuerySpec<T, TResult>
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ApplyProjected_WithAllClauses_TranslatesCorrectly()
    {
        var spec = QuerySpec<Customer, string>.Empty
            .Where(c => c.IsActive) // Alice (US, 5000), Charlie (US, 2000), Diana (CA, 10000), Eve (US, 500)
            .OrderBy(c => c.CountryCode) // CA (Diana), US (Alice, Charlie, Eve)
            .ThenByDescending(c => c.CreditLimit) // US (Alice 5000, Charlie 2000, Eve 500)
                                                  // Order: Diana, Alice, Charlie, Eve
            .Skip(1) // Skips Diana
            .Take(2) // Takes Alice, Charlie
            .Distinct()
            .Select(c => c.Name);

        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(2);
        result[0].Should().Be("Alice");
        result[1].Should().Be("Charlie");
    }

    [Fact]
    public void ApplyProjected_WithMultipleOrderings_OrderByDescendingAndThenBy()
    {
        var spec = QuerySpec<Customer, string>.Empty
            .Where(c => c.IsActive) // Alice, Charlie, Diana, Eve
            .OrderByDescending(c => c.CountryCode) // US (Alice, Charlie, Eve), CA (Diana)
            .ThenBy(c => c.CreditLimit) // US: Eve (500), Charlie (2000), Alice (5000)
            .Select(c => c.Name);

        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(4);
        result[0].Should().Be("Eve");
        result[1].Should().Be("Charlie");
        result[2].Should().Be("Alice");
        result[3].Should().Be("Diana");
    }

    [Fact]
    public void ApplyProjected_WithoutProjection_ThrowsInvalidOperationException()
    {
        var spec = QuerySpec<Customer, string>.Empty
            .Where(c => c.IsActive);

        var act = () => _customers.Apply(spec);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApplyProjected_MultipleWhere_CombinesAsAnd()
    {
        var spec = QuerySpec<Customer, string>.Empty
            .Where(c => c.IsActive)
            .Where(c => c.CountryCode == "US")
            .Select(c => c.Name);

        var result = _customers.Apply(spec).ToList();
        result.Should().HaveCount(3); // Alice, Charlie, Eve
    }

    [Fact]
    public void ApplyProjected_SingleWhere_WorksCorrectly()
    {
        var spec = QuerySpec<Customer, string>.Empty
            .Where(c => c.CountryCode == "UK")
            .Select(c => c.Name);

        var result = _customers.Apply(spec).ToList();
        result.Should().HaveCount(1);
        result[0].Should().Be("Bob");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Argument Null Exceptions
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Apply_NullSource_ThrowsArgumentNullException()
    {
        IQueryable<Customer> source = null!;
        var act = () => source.Apply(QuerySpec<Customer>.Empty);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("source");
    }

    [Fact]
    public void Apply_NullSpec_ThrowsArgumentNullException()
    {
        var act = () => _customers.Apply((QuerySpec<Customer>)null!);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("spec");
    }

    [Fact]
    public void ApplyProjected_NullSource_ThrowsArgumentNullException()
    {
        IQueryable<Customer> source = null!;
        var act = () => source.Apply(QuerySpec<Customer, string>.Empty);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("source");
    }

    [Fact]
    public void ApplyProjected_NullSpec_ThrowsArgumentNullException()
    {
        var act = () => _customers.Apply((QuerySpec<Customer, string>)null!);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("spec");
    }

    [Fact]
    public void Any_NullSource_ThrowsArgumentNullException()
    {
        IQueryable<Customer> source = null!;
        var act = () => source.Any(new ThrowingSpecification());
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("source");
    }

    [Fact]
    public void Any_NullSpec_ThrowsArgumentNullException()
    {
        var act = () => _customers.Any((IExpressionSpecification<Customer>)null!);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("specification");
    }

    [Fact]
    public void Count_NullSource_ThrowsArgumentNullException()
    {
        IQueryable<Customer> source = null!;
        var act = () => source.Count(new ThrowingSpecification());
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("source");
    }

    [Fact]
    public void Count_NullSpec_ThrowsArgumentNullException()
    {
        var act = () => _customers.Count((IExpressionSpecification<Customer>)null!);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("specification");
    }

    [Fact]
    public void Any_WithMatchingElement_ReturnsTrue()
    {
        var result = QuerySpecLinqExtensions.Any(_customers, new ActiveCustomerSpecification());
        result.Should().BeTrue();
    }

    [Fact]
    public void Any_WithoutMatchingElement_ReturnsFalse()
    {
        var result = QuerySpecLinqExtensions.Any(_customers, new NoMatchCustomerSpecification());
        result.Should().BeFalse();
    }

    [Fact]
    public void Count_WithMatchingElements_ReturnsCorrectCount()
    {
        var result = QuerySpecLinqExtensions.Count(_customers, new ActiveCustomerSpecification());
        result.Should().Be(4);
    }

    [Fact]
    public void Apply_WithSeekAfter_FiltersAndLimitsCorrectly()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.Id)
            .SeekAfter(c => c.Id, 2, 2);

        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(4);
    }

    [Fact]
    public void Apply_WithSeekBefore_FiltersAndLimitsCorrectly()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderByDescending(c => c.Id)
            .SeekBefore(c => c.Id, 4, 2);

        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public void Apply_ProjectedWithCursor_FiltersAndLimitsCorrectly()
    {
        var spec = QuerySpec<Customer, string>.Empty
            .OrderBy(c => c.Id)
            .SeekAfter(c => c.Id, 1, 2)
            .Select(c => c.Name);

        var result = _customers.Apply(spec).ToList();

        result.Should().HaveCount(2);
        result[0].Should().Be("Bob");
        result[1].Should().Be("Charlie");
    }

    [Fact]
    public void Apply_WithNullableConvertedCursorKeySelector_UnwrapsConvertAndFilters()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.Id)
            .SeekAfter(c => (int?)c.Id, 2, 2);

        var result = _customers.Apply(spec).ToList();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(4);
    }

    [Fact]
    public void Apply_WithUnaryNonConvertCursorKeySelector_PreservesUnaryAndFilters()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => -c.Id)
            .SeekAfter(c => -c.Id, -4, 2);

        var result = _customers.Apply(spec).ToList();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public void Apply_WithNullableConvertedCursorKeySelector_SeekBefore_UnwrapsConvertAndFilters()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderByDescending(c => c.Id)
            .SeekBefore(c => (int?)c.Id, 4, 2);

        var result = _customers.Apply(spec).ToList();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public void Apply_WithNonConvertUnaryCursorKeySelector_FiltersCorrectly()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => -c.Id)
            .SeekAfter(c => -c.Id, -4, 2);

        var result = _customers.Apply(spec).ToList();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(3);
        result[1].Id.Should().Be(2);
    }

    [Fact]
    public void Any_WithSpecification_EvaluatesCorrectly()
    {
        var spec = new ActiveCustomerSpecification();
        _customers.Any(spec).Should().BeTrue();

        var noMatchSpec = new NoMatchCustomerSpecification();
        _customers.Any(noMatchSpec).Should().BeFalse();
    }

    [Fact]
    public void Any_NullArguments_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var act1 = () => ((IQueryable<Customer>)null!).Any(spec);
        act1.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("source");

        var throwingSpec = new ThrowingSpecification();
        var actThrow = () => ((IQueryable<Customer>)null!).Any(throwingSpec);
        actThrow.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("source");

        var act2 = () => _customers.Any((IExpressionSpecification<Customer>)null!);
        act2.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("specification");
    }

    [Fact]
    public void Count_WithSpecification_EvaluatesCorrectly()
    {
        var spec = new ActiveCustomerSpecification();
        _customers.Count(spec).Should().Be(4);

        var noMatchSpec = new NoMatchCustomerSpecification();
        _customers.Count(noMatchSpec).Should().Be(0);
    }

    [Fact]
    public void Count_NullArguments_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var act1 = () => ((IQueryable<Customer>)null!).Count(spec);
        act1.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("source");

        var throwingSpec = new ThrowingSpecification();
        var actThrow = () => ((IQueryable<Customer>)null!).Count(throwingSpec);
        actThrow.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("source");

        var act2 = () => _customers.Count((IExpressionSpecification<Customer>)null!);
        act2.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("specification");
    }

    private sealed class NoMatchCustomerSpecification : EricksonLopez.Specification.Specification<Customer>
    {
        protected override System.Linq.Expressions.Expression<Func<Customer, bool>> BuildExpression()
            => c => c.CreditLimit > 100000m;
    }

    private sealed class ThrowingSpecification : IExpressionSpecification<Customer>
    {
        public System.Linq.Expressions.Expression<Func<Customer, bool>> ToExpression()
            => throw new InvalidOperationException("ToExpression should not be evaluated when source is null.");

        public bool IsSatisfiedBy(Customer entity)
            => throw new InvalidOperationException("IsSatisfiedBy should not be evaluated.");
    }
}





