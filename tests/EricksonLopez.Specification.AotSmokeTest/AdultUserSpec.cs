// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;
using EricksonLopez.Specification;

namespace EricksonLopez.Specification.AotSmokeTest;

/// <summary>
/// Specification verifying that a user meets or exceeds a minimum age.
/// </summary>
public sealed class AdultUserSpec : Specification<UserDto>
{
    private readonly int _minAge;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdultUserSpec"/> class.
    /// </summary>
    /// <param name="minAge">The minimum age threshold.</param>
    public AdultUserSpec(int minAge) => _minAge = minAge;

    protected override Expression<Func<UserDto, bool>> BuildExpression() => u => u.Age >= _minAge;
}
