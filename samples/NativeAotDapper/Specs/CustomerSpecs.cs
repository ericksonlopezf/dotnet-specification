// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using EricksonLopez.Specification;

namespace NativeAotDapper.Specs;

internal sealed class ActiveCustomerSpec : Specification<Customer>
{
    protected override Expression<Func<Customer, bool>> BuildExpression()
    {
        return c => c.IsActive;
    }
}

internal sealed class PremiumCustomerSpec : Specification<Customer>
{
    private readonly string _region;

    public PremiumCustomerSpec(string region)
    {
        _region = region;
    }

    protected override Expression<Func<Customer, bool>> BuildExpression()
    {
        return c => c.Tier == "Premium" && c.Region == _region;
    }
}

