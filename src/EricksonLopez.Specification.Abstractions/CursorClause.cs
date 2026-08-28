// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Represents a keyset cursor clause for pagination.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public sealed class CursorClause<T>
{
    /// <summary>Gets the key selector expression used for the cursor.</summary>
    public Expression<Func<T, object?>> KeySelector { get; }

    /// <summary>Gets the cursor value to seek from.</summary>
    public object? Value { get; }

    /// <summary>Gets the seek direction.</summary>
    public CursorDirection Direction { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorClause{T}"/> class.
    /// </summary>
    /// <param name="keySelector">The key selector expression.</param>
    /// <param name="value">The cursor threshold value.</param>
    /// <param name="direction">The seek direction.</param>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/></exception>
    public CursorClause(Expression<Func<T, object?>> keySelector, object? value, CursorDirection direction)
    {
        KeySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        Value = value;
        Direction = direction;
    }
}
