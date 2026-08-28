// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;

namespace EricksonLopez.Specification.Tests;

// ─────────────────────────────────────────────────────────
// Test domain model
// ─────────────────────────────────────────────────────────

/// <summary>Test entity representing a customer.</summary>
public sealed class Customer
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal CreditLimit { get; init; }
    public string CountryCode { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
}

/// <summary>Test entity representing an order.</summary>
public sealed class Order
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public decimal Total { get; init; }
    public DateTime CreatedAt { get; init; }
}

// ─────────────────────────────────────────────────────────
// Concrete specifications for testing
// ─────────────────────────────────────────────────────────

/// <summary>Specification: customer must be active.</summary>
public sealed class ActiveCustomerSpecification : Specification<Customer>
{
    protected override System.Linq.Expressions.Expression<Func<Customer, bool>> BuildExpression()
        => customer => customer.IsActive;
}

/// <summary>Specification: customer must not be deleted.</summary>
public sealed class NotDeletedCustomerSpecification : Specification<Customer>
{
    protected override System.Linq.Expressions.Expression<Func<Customer, bool>> BuildExpression()
        => customer => !customer.IsDeleted;
}

/// <summary>Specification: customer must have a specific country code.</summary>
public sealed class CustomerFromCountrySpecification : Specification<Customer>
{
    private readonly string _countryCode;

    public CustomerFromCountrySpecification(string countryCode) => _countryCode = countryCode;

    protected override System.Linq.Expressions.Expression<Func<Customer, bool>> BuildExpression()
        => customer => customer.CountryCode == _countryCode;
}

/// <summary>Specification: customer credit limit must exceed threshold.</summary>
public sealed class CreditLimitExceedsSpecification : Specification<Customer>
{
    private readonly decimal _threshold;

    public CreditLimitExceedsSpecification(decimal threshold) => _threshold = threshold;

    protected override System.Linq.Expressions.Expression<Func<Customer, bool>> BuildExpression()
        => customer => customer.CreditLimit > _threshold;
}

/// <summary>Specification that tracks how many times BuildExpression() was called.</summary>
public sealed class CountingSpecification : Specification<Customer>
{
    private readonly Action _onBuild;

    public CountingSpecification(Action onBuild) => _onBuild = onBuild;

    protected override System.Linq.Expressions.Expression<Func<Customer, bool>> BuildExpression()
    {
        _onBuild();
        return customer => customer.IsActive;
    }
}



