// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Specification that checks whether a <see cref="Customer"/> account is active.
/// </summary>
public sealed class ActiveCustomerSpecification : Specification<Customer>
{
    /// <inheritdoc/>
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => customer => customer.IsActive;
}
