// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Specification that identifies high-value customers by credit limit.
/// </summary>
public sealed class HighCreditCustomerSpecification : Specification<Customer>
{
    private readonly decimal _minimumCreditLimit;

    /// <summary>
    /// Initializes a new <see cref="HighCreditCustomerSpecification"/>.
    /// </summary>
    /// <param name="minimumCreditLimit">The minimum credit limit for a high-credit customer.</param>
    public HighCreditCustomerSpecification(decimal minimumCreditLimit = 5000m)
    {
        _minimumCreditLimit = minimumCreditLimit;
    }

    /// <inheritdoc/>
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => customer => customer.CreditLimit >= _minimumCreditLimit;
}
