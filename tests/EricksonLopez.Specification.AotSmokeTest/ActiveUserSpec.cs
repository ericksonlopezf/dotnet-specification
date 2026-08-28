// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;
using EricksonLopez.Specification;

namespace EricksonLopez.Specification.AotSmokeTest;

/// <summary>
/// Specification verifying that a user is marked active.
/// </summary>
public sealed class ActiveUserSpec : Specification<UserDto>
{
    protected override Expression<Func<UserDto, bool>> BuildExpression() => u => u.IsActive;
}
