// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.Specification;

using System;

/// <summary>
/// Provides extension methods for range operations.
/// </summary>
public static class RangeExtensions
{
    /// <summary>
    /// Determines whether the value is inclusively between the specified range bounds.
    /// </summary>
    /// <typeparam name="T">The value type implementing <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to test.</param>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is contained within the range; otherwise, <see langword="false"/>.</returns>
    public static bool InRange<T>(this T value, T lower, T upper) where T : IComparable<T>
    {
        return value.CompareTo(lower) >= 0 && value.CompareTo(upper) <= 0;
    }

    /// <summary>
    /// Determines whether the value is contained within the specified range bounds.
    /// </summary>
    /// <typeparam name="T">The value type implementing <see cref="IComparable{T}"/>.</typeparam>
    /// <param name="value">The value to test.</param>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> is contained within the range; otherwise, <see langword="false"/>.</returns>
    public static bool ContainedByRange<T>(this T value, T lower, T upper) where T : IComparable<T>
    {
        return InRange(value, lower, upper);
    }
}

