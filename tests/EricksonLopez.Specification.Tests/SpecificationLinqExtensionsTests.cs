// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Specification.Linq;
using Xunit;

namespace EricksonLopez.Specification.Tests;

public sealed class SpecificationLinqExtensionsTests
{
    private sealed class ActiveCustomerSpec : Specification<Customer>
    {
        protected override System.Linq.Expressions.Expression<Func<Customer, bool>> BuildExpression()
            => c => c.IsActive;
    }

    private sealed class HighCreditCustomerSpec : Specification<Customer>
    {
        private readonly decimal _threshold;
        public HighCreditCustomerSpec(decimal threshold) => _threshold = threshold;

        protected override System.Linq.Expressions.Expression<Func<Customer, bool>> BuildExpression()
            => c => c.CreditLimit >= _threshold;
    }

    private static List<Customer> CreateSampleCustomers() =>
    [
        new Customer { Id = 1, Name = "Alice", IsActive = true, CreditLimit = 500m },
        new Customer { Id = 2, Name = "Bob", IsActive = false, CreditLimit = 200m },
        new Customer { Id = 3, Name = "Charlie", IsActive = true, CreditLimit = 1500m },
        new Customer { Id = 4, Name = "Diana", IsActive = false, CreditLimit = 3000m }
    ];

    // ──────────────────────────────────────────────────────────────────────────
    // IEnumerable<T> Extension Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Where_OnEnumerable_FiltersMatchingEntities()
    {
        var customers = CreateSampleCustomers();
        var spec = new ActiveCustomerSpec();

        var result = customers.Where(spec).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.IsActive);
    }

    [Fact]
    public void Where_OnEnumerable_WithNullArguments_ThrowsArgumentNullException()
    {
        IEnumerable<Customer> customers = CreateSampleCustomers();
        ISpecification<Customer> spec = new ActiveCustomerSpec();

        Action act1 = () => ((IEnumerable<Customer>)null!).Where(spec);
        Action act2 = () => customers.Where((ISpecification<Customer>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Any_OnEnumerable_ReturnsCorrectBoolean()
    {
        var customers = CreateSampleCustomers();
        var specHigh = new HighCreditCustomerSpec(2000m);
        var specImpossible = new HighCreditCustomerSpec(10000m);

        customers.Any(specHigh).Should().BeTrue();
        customers.Any(specImpossible).Should().BeFalse();
    }

    [Fact]
    public void All_OnEnumerable_ReturnsCorrectBoolean()
    {
        var customers = CreateSampleCustomers();
        var activeSpec = new ActiveCustomerSpec();
        var positiveCreditSpec = new HighCreditCustomerSpec(0m);

        customers.All(activeSpec).Should().BeFalse();
        customers.All(positiveCreditSpec).Should().BeTrue();
    }

    [Fact]
    public void Count_OnEnumerable_ReturnsAccurateCount()
    {
        var customers = CreateSampleCustomers();
        var activeSpec = new ActiveCustomerSpec();

        customers.Count(activeSpec).Should().Be(2);
    }

    [Fact]
    public void FirstOrDefault_OnEnumerable_ReturnsFirstOrNull()
    {
        var customers = CreateSampleCustomers();
        var spec = new HighCreditCustomerSpec(1000m);
        var impossible = new HighCreditCustomerSpec(50000m);

        customers.FirstOrDefault(spec)?.Name.Should().Be("Charlie");
        customers.FirstOrDefault(impossible).Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IQueryable<T> Extension Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Where_OnQueryable_FiltersMatchingEntities()
    {
        var queryable = CreateSampleCustomers().AsQueryable();
        var spec = new ActiveCustomerSpec();

        var result = queryable.Where(spec).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.IsActive);
    }

    [Fact]
    public void Where_OnQueryable_WithNullArguments_ThrowsArgumentNullException()
    {
        IQueryable<Customer> queryable = CreateSampleCustomers().AsQueryable();
        IExpressionSpecification<Customer> spec = new ActiveCustomerSpec();

        Action act1 = () => ((IQueryable<Customer>)null!).Where(spec);
        Action act2 = () => queryable.Where((IExpressionSpecification<Customer>)null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Any_OnQueryable_ReturnsCorrectBoolean()
    {
        var queryable = CreateSampleCustomers().AsQueryable();
        var specHigh = new HighCreditCustomerSpec(2000m);
        var specImpossible = new HighCreditCustomerSpec(10000m);

        queryable.Any(specHigh).Should().BeTrue();
        queryable.Any(specImpossible).Should().BeFalse();
    }

    [Fact]
    public void All_OnQueryable_ReturnsCorrectBoolean()
    {
        var queryable = CreateSampleCustomers().AsQueryable();
        var activeSpec = new ActiveCustomerSpec();
        var positiveCreditSpec = new HighCreditCustomerSpec(0m);

        queryable.All(activeSpec).Should().BeFalse();
        queryable.All(positiveCreditSpec).Should().BeTrue();
    }

    [Fact]
    public void Count_OnQueryable_ReturnsAccurateCount()
    {
        var queryable = CreateSampleCustomers().AsQueryable();
        var activeSpec = new ActiveCustomerSpec();

        queryable.Count(activeSpec).Should().Be(2);
    }

    [Fact]
    public void FirstOrDefault_OnQueryable_ReturnsFirstOrNull()
    {
        var queryable = CreateSampleCustomers().AsQueryable();
        var spec = new HighCreditCustomerSpec(1000m);
        var impossible = new HighCreditCustomerSpec(50000m);

        queryable.FirstOrDefault(spec)?.Name.Should().Be("Charlie");
        queryable.FirstOrDefault(impossible).Should().BeNull();
    }
}
