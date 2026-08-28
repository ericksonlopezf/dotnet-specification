// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Returns property names unchanged (verbatim mapping). Use when columns match property names exactly.
/// </summary>
public sealed class VerbatimColumnNameResolver : IColumnNameResolver
{
    /// <summary>Gets the shared singleton instance.</summary>
    public static readonly VerbatimColumnNameResolver Default = new();

    private VerbatimColumnNameResolver() { }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is <see langword="null"/> or whitespace</exception>
    public string Resolve(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return propertyName;
    }
}
