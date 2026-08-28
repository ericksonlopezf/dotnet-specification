// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.Specification;

using System;

/// <summary>
/// Provides extension methods for range comparisons in specifications and LINQ expressions.
/// </summary>
public static class BetweenExtensions
{
    /// <summary>
    /// Determines whether the value is inclusively between the lower and upper bounds.
    /// </summary>
    /// <typeparam name="T">The value type implementing <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to test.</param>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is between <paramref name="lower"/> and <paramref name="upper"/>; otherwise, <see langword="false"/>.</returns>
    public static bool Between<T>(this T value, T lower, T upper) where T : IComparable<T>
    {
        return value.CompareTo(lower) >= 0 && value.CompareTo(upper) <= 0;
    }
}

