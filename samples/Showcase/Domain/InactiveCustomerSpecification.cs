// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Specification that identifies inactive customers.
/// </summary>
public sealed class InactiveCustomerSpecification : Specification<Customer>
{
    /// <inheritdoc/>
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => customer => !customer.IsActive;
}
