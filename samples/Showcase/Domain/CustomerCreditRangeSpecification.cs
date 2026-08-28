// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Specification that filters customers whose credit limit is within a specified range.
/// </summary>
public sealed class CustomerCreditRangeSpecification : Specification<Customer>
{
    private readonly decimal _minCredit;
    private readonly decimal _maxCredit;

    /// <summary>
    /// Initializes a new instance of <see cref="CustomerCreditRangeSpecification"/>.
    /// </summary>
    public CustomerCreditRangeSpecification(decimal minCredit, decimal maxCredit)
    {
        _minCredit = minCredit;
        _maxCredit = maxCredit;
    }

    /// <inheritdoc/>
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => customer => customer.CreditLimit >= _minCredit && customer.CreditLimit <= _maxCredit;
}
