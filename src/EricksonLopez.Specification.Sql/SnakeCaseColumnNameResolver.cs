// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Converts PascalCase or camelCase property names to snake_case SQL column names.
/// </summary>
/// <example>
/// <c>IsActive</c> → <c>is_active</c><br/>
/// <c>CreditLimit</c> → <c>credit_limit</c><br/>
/// <c>CustomerId</c> → <c>customer_id</c>
/// </example>
public sealed class SnakeCaseColumnNameResolver : IColumnNameResolver
{
    /// <summary>Gets the shared singleton instance.</summary>
    public static readonly SnakeCaseColumnNameResolver Default = new();

    private SnakeCaseColumnNameResolver() { }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException"><paramref name="propertyName"/> is <see langword="null"/> or whitespace</exception>
    public string Resolve(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return ToSnakeCase(propertyName);
    }

    private static string ToSnakeCase(string name)
    {
        var result = new System.Text.StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
                result.Append('_');
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }
}
