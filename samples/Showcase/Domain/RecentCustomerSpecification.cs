// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Specification that identifies customers created within a recent time window.
/// </summary>
public sealed class RecentCustomerSpecification : Specification<Customer>
{
    private readonly DateTime _cutoffDate;

    /// <summary>
    /// Initializes a new <see cref="RecentCustomerSpecification"/>.
    /// </summary>
    /// <param name="daysBack">How many days back to consider a customer "recent". Defaults to 30.</param>
    public RecentCustomerSpecification(int daysBack = 30)
    {
        _cutoffDate = DateTime.UtcNow.AddDays(-daysBack);
    }

    /// <inheritdoc/>
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => customer => customer.CreatedAt >= _cutoffDate;
}
