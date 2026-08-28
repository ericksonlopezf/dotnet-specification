// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Specification that identifies VIP customers (more than 10 total purchases).
/// </summary>
public sealed class VipCustomerSpecification : Specification<Customer>
{
    private readonly int _minimumPurchases;

    /// <summary>
    /// Initializes a new <see cref="VipCustomerSpecification"/>.
    /// </summary>
    /// <param name="minimumPurchases">The minimum number of purchases required to be considered VIP.</param>
    public VipCustomerSpecification(int minimumPurchases = 10)
    {
        _minimumPurchases = minimumPurchases;
    }

    /// <inheritdoc/>
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => customer => customer.TotalPurchases > _minimumPurchases;
}
