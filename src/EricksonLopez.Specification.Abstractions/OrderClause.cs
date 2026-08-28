// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Represents a single ordering clause containing a key selector and direction.
/// </summary>
/// <typeparam name="T">The entity type being ordered.</typeparam>
public sealed class OrderClause<T>
{
    /// <summary>Gets the key selector expression used for ordering.</summary>
    public Expression<Func<T, object?>> KeySelector { get; }

    /// <summary>Gets the ordering direction.</summary>
    public OrderDirection Direction { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrderClause{T}"/> class.
    /// </summary>
    /// <param name="keySelector">The key selector expression.</param>
    /// <param name="direction">The ordering direction.</param>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/></exception>
    public OrderClause(Expression<Func<T, object?>> keySelector, OrderDirection direction)
    {
        KeySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        Direction = direction;
    }
}
