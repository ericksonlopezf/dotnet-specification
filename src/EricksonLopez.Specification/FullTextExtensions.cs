// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Specification;


/// <summary>
/// Provides extension methods for full-text search expressions.
/// </summary>
public static class FullTextExtensions
{
    /// <summary>
    /// Evaluates whether the string contains the specified full-text search query.
    /// </summary>
    /// <param name="value">The string value to evaluate.</param>
    /// <param name="query">The search terms to match.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> matches the search query; otherwise, <see langword="false"/>.</returns>
    public static bool MatchesFullText(this string? value, string query)
    {
        if (value is null || query is null) return false;
        return value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}


