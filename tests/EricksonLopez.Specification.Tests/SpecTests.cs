// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

public sealed class SpecTests
{
    [Fact]
    public void For_WithNullPredicate_ThrowsArgumentNullException()
    {
        var act = () => Spec.For<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void For_WithValidPredicate_CreatesSpecification()
    {
        var spec = Spec.For<Customer>(c => c.Name == "Alice");
        spec.IsSatisfiedBy(new Customer { Name = "Alice" }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { Name = "Bob" }).Should().BeFalse();
    }

    [Fact]
    public void True_ReturnsSpecificationThatIsAlwaysSatisfied()
    {
        var spec = Spec.True<Customer>();
        spec.IsSatisfiedBy(new Customer()).Should().BeTrue();

        var compiled = spec.ToExpression().Compile();
        compiled(new Customer()).Should().BeTrue();
    }

    [Fact]
    public void False_ReturnsSpecificationThatIsNeverSatisfied()
    {
        var spec = Spec.False<Customer>();
        spec.IsSatisfiedBy(new Customer()).Should().BeFalse();

        var compiled = spec.ToExpression().Compile();
        compiled(new Customer()).Should().BeFalse();
    }

    [Fact]
    public void Between_CreatesRangeSpecification()
    {
        var spec = Spec.Between<Customer, decimal>(c => c.CreditLimit, 100m, 500m);
        spec.IsSatisfiedBy(new Customer { CreditLimit = 50m }).Should().BeFalse();
        spec.IsSatisfiedBy(new Customer { CreditLimit = 100m }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { CreditLimit = 300m }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { CreditLimit = 500m }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { CreditLimit = 600m }).Should().BeFalse();
    }

    [Fact]
    public void Between_WithNullSelector_ThrowsArgumentNullException()
    {
        var act = () => Spec.Between<Customer, int>(null!, 1, 10);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void BetweenExtensions_EvaluatesCorrectly()
    {
        25.Between(18, 65).Should().BeTrue();
        18.Between(18, 65).Should().BeTrue();
        65.Between(18, 65).Should().BeTrue();
        10.Between(18, 65).Should().BeFalse();
        70.Between(18, 65).Should().BeFalse();
    }

    [Fact]
    public void FullText_CreatesFullTextSpecification()
    {
        var spec = Spec.FullText<Customer>(c => c.Name, "alice");
        spec.IsSatisfiedBy(new Customer { Name = "Alice in Wonderland" }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { Name = "Bob" }).Should().BeFalse();
    }

    [Fact]
    public void FullText_WithNullArguments_ThrowsArgumentNullException()
    {
        var act1 = () => Spec.FullText<Customer>(null!, "query");
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => Spec.FullText<Customer>(c => c.Name, null!);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FullTextExtensions_EvaluatesCorrectly()
    {
        "Hello World".MatchesFullText("world").Should().BeTrue();
        "Hello World".MatchesFullText("xyz").Should().BeFalse();
        ((string?)null).MatchesFullText("test").Should().BeFalse();
    }

    [Fact]
    public void InRange_CreatesRangeSpecification()
    {
        var spec = Spec.InRange<Customer, decimal>(c => c.CreditLimit, 100m, 500m);
        spec.IsSatisfiedBy(new Customer { CreditLimit = 250m }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { CreditLimit = 100m }).Should().BeTrue(); // Lower bound exact
        spec.IsSatisfiedBy(new Customer { CreditLimit = 500m }).Should().BeTrue(); // Upper bound exact
        spec.IsSatisfiedBy(new Customer { CreditLimit = 99.99m }).Should().BeFalse(); // Below lower bound
        spec.IsSatisfiedBy(new Customer { CreditLimit = 500.01m }).Should().BeFalse(); // Above upper bound
    }

    [Fact]
    public void RangeExtensions_EvaluatesCorrectly()
    {
        50.InRange(10, 100).Should().BeTrue();
        10.InRange(10, 100).Should().BeTrue(); // Lower bound exact
        100.InRange(10, 100).Should().BeTrue(); // Upper bound exact
        9.InRange(10, 100).Should().BeFalse(); // Below lower bound
        101.InRange(10, 100).Should().BeFalse(); // Above upper bound

        50.ContainedByRange(10, 100).Should().BeTrue();
        10.ContainedByRange(10, 100).Should().BeTrue(); // Lower bound exact
        100.ContainedByRange(10, 100).Should().BeTrue(); // Upper bound exact
        9.ContainedByRange(10, 100).Should().BeFalse(); // Below lower bound
        101.ContainedByRange(10, 100).Should().BeFalse(); // Above upper bound
    }

    [Fact]
    public void Search_MatchesAcrossMultipleProperties()
    {
        var spec = Spec.Search<Customer>("Acme", c => c.Name, c => c.CountryCode);

        spec.ToExpression().Parameters[0].Name.Should().Be("x");
        spec.IsSatisfiedBy(new Customer { Name = "Acme Corp", CountryCode = "US" }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { Name = "Beta LLC", CountryCode = "Acme South" }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { Name = "Beta LLC", CountryCode = "CA" }).Should().BeFalse();
    }

    [Fact]
    public void Search_WithNullOrEmpty_ThrowsArgumentException()
    {
        var act1 = () => Spec.Search<Customer>(null!, c => c.Name);
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => Spec.Search<Customer>("test", null!);
        act2.Should().Throw<ArgumentNullException>();

        var act3 = () => Spec.Search<Customer>("test");
        act3.Should().Throw<ArgumentException>()
            .WithParameterName("propertySelectors")
            .WithMessage("At least one property selector must be provided.*");

        var act4 = () => Spec.Search<Customer>("test", new Expression<Func<Customer, string?>>[] { null! });
        act4.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void QuerySpec_Search_AppliesLogicalOrFilter()
    {
        var spec = new QuerySpec<Customer>().Search("Tech", c => c.Name, c => c.CountryCode);

        var expr = spec.Criteria[0].Compile();
        expr(new Customer { Name = "Tech Giant", CountryCode = "US" }).Should().BeTrue();
        expr(new Customer { Name = "Startup", CountryCode = "Tech Valley" }).Should().BeTrue();
        expr(new Customer { Name = "Startup", CountryCode = "EU" }).Should().BeFalse();
    }

    [Fact]
    public void QuerySpec_TagWith_SetsTagProperty()
    {
        var spec = new QuerySpec<Customer>().TagWith("GetActiveCustomersQuery");
        spec.Tag.Should().Be("GetActiveCustomersQuery");

        var actNull = () => new QuerySpec<Customer>().TagWith(null!);
        actNull.Should().Throw<ArgumentException>();

        var actEmpty = () => new QuerySpec<Customer>().TagWith("   ");
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void QuerySpecProjected_SearchAndTagWith_BehavesCorrectly()
    {
        var spec = new QuerySpec<Customer, string>()
            .Search("VIP", c => c.Name, c => c.CountryCode)
            .TagWith("ProjectedTag")
            .Select(c => c.Name);

        spec.Tag.Should().Be("ProjectedTag");
        spec.Criteria.Should().HaveCount(1);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Spec.All and Spec.Any Combinators
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void All_WithNullSpecifications_ThrowsArgumentNullException()
    {
        var act = () => Spec.All<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void All_WithEmptySpecifications_ReturnsAlwaysTrueSpecification()
    {
        var spec = Spec.All<Customer>();
        spec.IsSatisfiedBy(new Customer()).Should().BeTrue("empty All specification must be neutral element (true)");
        spec.ToExpression().Compile()(new Customer()).Should().BeTrue();
    }

    [Fact]
    public void All_WithSingleSpecification_ReturnsOriginalSpecification()
    {
        var single = Spec.For<Customer>(c => c.IsActive);
        var spec = Spec.All(single);

        spec.Should().BeSameAs(single);
        spec.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { IsActive = false }).Should().BeFalse();
    }

    [Fact]
    public void All_WithMultipleSpecifications_WhenAllSatisfied_ReturnsTrue()
    {
        var spec1 = Spec.For<Customer>(c => c.IsActive);
        var spec2 = Spec.For<Customer>(c => c.CreditLimit > 100m);
        var spec3 = Spec.For<Customer>(c => c.CountryCode == "US");

        var allSpec = Spec.All(spec1, spec2, spec3);

        allSpec.IsSatisfiedBy(new Customer { IsActive = true, CreditLimit = 500m, CountryCode = "US" }).Should().BeTrue();
    }

    [Fact]
    public void All_WithMultipleSpecifications_WhenOneFails_ReturnsFalse()
    {
        var spec1 = Spec.For<Customer>(c => c.IsActive);
        var spec2 = Spec.For<Customer>(c => c.CreditLimit > 100m);
        var spec3 = Spec.For<Customer>(c => c.CountryCode == "US");

        var allSpec = Spec.All(spec1, spec2, spec3);

        allSpec.IsSatisfiedBy(new Customer { IsActive = false, CreditLimit = 500m, CountryCode = "US" }).Should().BeFalse();
        allSpec.IsSatisfiedBy(new Customer { IsActive = true, CreditLimit = 50m, CountryCode = "US" }).Should().BeFalse();
        allSpec.IsSatisfiedBy(new Customer { IsActive = true, CreditLimit = 500m, CountryCode = "CA" }).Should().BeFalse();
    }

    [Fact]
    public void Any_WithNullSpecifications_ThrowsArgumentNullException()
    {
        var act = () => Spec.Any<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Any_WithEmptySpecifications_ReturnsAlwaysFalseSpecification()
    {
        var spec = Spec.Any<Customer>();
        spec.IsSatisfiedBy(new Customer()).Should().BeFalse("empty Any specification must be neutral element (false)");
        spec.ToExpression().Compile()(new Customer()).Should().BeFalse();
    }

    [Fact]
    public void Any_WithSingleSpecification_ReturnsOriginalSpecification()
    {
        var single = Spec.For<Customer>(c => c.IsActive);
        var spec = Spec.Any(single);

        spec.Should().BeSameAs(single);
        spec.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { IsActive = false }).Should().BeFalse();
    }

    [Fact]
    public void Any_WithMultipleSpecifications_WhenOneSatisfied_ReturnsTrue()
    {
        var spec1 = Spec.For<Customer>(c => c.IsActive);
        var spec2 = Spec.For<Customer>(c => c.CreditLimit > 1000m);
        var spec3 = Spec.For<Customer>(c => c.CountryCode == "US");

        var anySpec = Spec.Any(spec1, spec2, spec3);

        anySpec.IsSatisfiedBy(new Customer { IsActive = true, CreditLimit = 0m, CountryCode = "CA" }).Should().BeTrue();
        anySpec.IsSatisfiedBy(new Customer { IsActive = false, CreditLimit = 2000m, CountryCode = "CA" }).Should().BeTrue();
        anySpec.IsSatisfiedBy(new Customer { IsActive = false, CreditLimit = 0m, CountryCode = "US" }).Should().BeTrue();
    }

    [Fact]
    public void Any_WithMultipleSpecifications_WhenNoneSatisfied_ReturnsFalse()
    {
        var spec1 = Spec.For<Customer>(c => c.IsActive);
        var spec2 = Spec.For<Customer>(c => c.CreditLimit > 1000m);
        var spec3 = Spec.For<Customer>(c => c.CountryCode == "US");

        var anySpec = Spec.Any(spec1, spec2, spec3);

        anySpec.IsSatisfiedBy(new Customer { IsActive = false, CreditLimit = 100m, CountryCode = "CA" }).Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Neutral Element Simplification with True / False
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void True_And_Specification_SimplifiesToOriginalExpression()
    {
        var activeSpec = Spec.For<Customer>(c => c.IsActive);
        var composed = Spec.True<Customer>().And(activeSpec);

        composed.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeTrue();
        composed.IsSatisfiedBy(new Customer { IsActive = false }).Should().BeFalse();
    }

    [Fact]
    public void False_Or_Specification_SimplifiesToOriginalExpression()
    {
        var activeSpec = Spec.For<Customer>(c => c.IsActive);
        var composed = Spec.False<Customer>().Or(activeSpec);

        composed.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeTrue();
        composed.IsSatisfiedBy(new Customer { IsActive = false }).Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IEnumerable<Specification<T>> Overloads
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void All_WithNullEnumerable_ThrowsArgumentNullException()
    {
        IEnumerable<Specification<Customer>> specs = null!;
        var act = () => Spec.All(specs);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void All_WithEmptyList_ReturnsAlwaysTrueSpecification()
    {
        var list = new System.Collections.Generic.List<Specification<Customer>>();
        var spec = Spec.All(list);
        spec.IsSatisfiedBy(new Customer()).Should().BeTrue();
    }

    [Fact]
    public void All_WithSingleItemList_ReturnsOriginalSpecification()
    {
        var single = Spec.For<Customer>(c => c.IsActive);
        var list = new System.Collections.Generic.List<Specification<Customer>> { single };
        var spec = Spec.All(list);
        spec.Should().BeSameAs(single);
    }

    [Fact]
    public void All_WithMultipleItemList_ComposesWithAnd()
    {
        var spec1 = Spec.For<Customer>(c => c.IsActive);
        var spec2 = Spec.For<Customer>(c => c.CreditLimit > 100m);
        var list = new System.Collections.Generic.List<Specification<Customer>> { spec1, spec2 };

        var composed = Spec.All(list);
        composed.IsSatisfiedBy(new Customer { IsActive = true, CreditLimit = 200m }).Should().BeTrue();
        composed.IsSatisfiedBy(new Customer { IsActive = true, CreditLimit = 50m }).Should().BeFalse();
        composed.IsSatisfiedBy(new Customer { IsActive = false, CreditLimit = 200m }).Should().BeFalse();
    }

    [Fact]
    public void Any_WithNullEnumerable_ThrowsArgumentNullException()
    {
        IEnumerable<Specification<Customer>> specs = null!;
        var act = () => Spec.Any(specs);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Any_WithEmptyList_ReturnsAlwaysFalseSpecification()
    {
        var list = new System.Collections.Generic.List<Specification<Customer>>();
        var spec = Spec.Any(list);
        spec.IsSatisfiedBy(new Customer()).Should().BeFalse();
    }

    [Fact]
    public void Any_WithSingleItemList_ReturnsOriginalSpecification()
    {
        var single = Spec.For<Customer>(c => c.IsActive);
        var list = new System.Collections.Generic.List<Specification<Customer>> { single };
        var spec = Spec.Any(list);
        spec.Should().BeSameAs(single);
    }

    [Fact]
    public void Any_WithMultipleItemList_ComposesWithOr()
    {
        var spec1 = Spec.For<Customer>(c => c.IsActive);
        var spec2 = Spec.For<Customer>(c => c.CreditLimit > 1000m);
        var list = new System.Collections.Generic.List<Specification<Customer>> { spec1, spec2 };

        var composed = Spec.Any(list);
        composed.IsSatisfiedBy(new Customer { IsActive = true, CreditLimit = 50m }).Should().BeTrue();
        composed.IsSatisfiedBy(new Customer { IsActive = false, CreditLimit = 2000m }).Should().BeTrue();
        composed.IsSatisfiedBy(new Customer { IsActive = false, CreditLimit = 50m }).Should().BeFalse();
    }
}


