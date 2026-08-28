// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;

namespace EricksonLopez.Specification;

/// <summary>
/// Defines a domain-level specification used to evaluate whether a candidate satisfies business criteria.
/// </summary>
/// <typeparam name="T">The type of candidate being evaluated.</typeparam>
/// <remarks>
/// This interface is minimal and AOT-safe, providing in-memory rule evaluation without external persistence dependencies.
/// </remarks>
public interface ISpecification<
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicFields)] T>
{
    /// <summary>
    /// Determines whether the specified <paramref name="candidate"/> satisfies this specification.
    /// </summary>
    /// <param name="candidate">The candidate to evaluate.</param>
    /// <returns><see langword="true"/> if the candidate satisfies the specification; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="candidate"/> is <see langword="null"/>.</exception>
    bool IsSatisfiedBy(T candidate);
}


