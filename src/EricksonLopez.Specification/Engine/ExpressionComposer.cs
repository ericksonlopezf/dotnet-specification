// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace EricksonLopez.Specification;

/// <summary>
/// Provides efficient, provider-safe expression composition without <c>Expression.Invoke</c>.
/// All compositions inline the right-hand expression body using parameter rebinding.
/// </summary>
/// <remarks>
/// <para>
/// The fundamental constraint this engine satisfies: no composed expression tree
/// should contain an <c>InvocationExpression</c>. EF Core's query translator and
/// most SQL translators do not support <c>Expression.Invoke</c>, and its presence
/// causes client-side evaluation or translation failures.
/// </para>
/// <para>
/// All methods are allocation-minimal: they create exactly the new expression nodes
/// required (a parameter expression reuse, a binary node, and the lambda wrapper).
/// </para>
/// </remarks>
public static class ExpressionComposer
{
    /// <summary>
    /// Combines two predicate expressions with a logical AND.
    /// Equivalent to: <c>x => left(x) &amp;&amp; right(x)</c>
    /// </summary>
    /// <typeparam name="T">The input type of both predicates.</typeparam>
    /// <param name="left">The left-hand predicate.</param>
    /// <param name="right">The right-hand predicate.</param>
    /// <returns>A new expression that is the logical AND of both predicates.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression<Func<T, bool>> And<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var parameter = left.Parameters[0];
        var rightBody = ParameterReplacer.Replace(right.Body, right.Parameters[0], parameter);
        var body = Expression.AndAlso(left.Body, rightBody);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    /// <summary>
    /// Combines two predicate expressions using logical OR with parameter rebinding.
    /// </summary>
    /// <typeparam name="T">The predicate parameter type.</typeparam>
    /// <param name="left">The left operand expression.</param>
    /// <param name="right">The right operand expression.</param>
    /// <returns>A new expression representing <c>left OR right</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/></exception>
    public static Expression<Func<T, bool>> Or<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var param = left.Parameters[0];
        var rightBody = ParameterReplacer.Replace(right.Body, right.Parameters[0], param);
        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(left.Body, rightBody), param);
    }

    /// <summary>
    /// Negates a predicate expression.
    /// Equivalent to: <c>x => !predicate(x)</c>
    /// </summary>
    /// <typeparam name="T">The input type of the predicate.</typeparam>
    /// <param name="predicate">The predicate to negate.</param>
    /// <returns>A new expression that is the logical NOT of the predicate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Expression<Func<T, bool>> Not<T>(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Expression.Lambda<Func<T, bool>>(
            Expression.Not(predicate.Body), predicate.Parameters[0]);
    }

    /// <summary>
    /// Combines an array of predicate expressions with logical AND.
    /// All predicates share the same parameter after composition.
    /// </summary>
    /// <typeparam name="T">The input type of all predicates.</typeparam>
    /// <param name="predicates">The predicates to combine. Must not be empty.</param>
    /// <returns>A single expression representing the logical AND of all predicates.</returns>
    /// <exception cref="ArgumentException"><paramref name="predicates"/> is empty</exception>
    public static Expression<Func<T, bool>> AndAll<T>(
        ReadOnlySpan<Expression<Func<T, bool>>> predicates)
    {
        if (predicates.IsEmpty)
            throw new ArgumentException("At least one predicate is required.", nameof(predicates));

        var result = predicates[0];
        for (var i = 1; i < predicates.Length; i++)
            result = And(result, predicates[i]);
        return result;
    }

    /// <summary>
    /// Combines an array of predicate expressions with logical OR.
    /// All predicates share the same parameter after composition.
    /// </summary>
    /// <typeparam name="T">The input type of all predicates.</typeparam>
    /// <param name="predicates">The predicates to combine. Must not be empty.</param>
    /// <returns>A single expression representing the logical OR of all predicates.</returns>
    /// <exception cref="ArgumentException"><paramref name="predicates"/> is empty</exception>
    public static Expression<Func<T, bool>> OrAny<T>(
        ReadOnlySpan<Expression<Func<T, bool>>> predicates)
    {
        if (predicates.IsEmpty)
            throw new ArgumentException("At least one predicate is required.", nameof(predicates));

        var result = predicates[0];
        for (var i = 1; i < predicates.Length; i++)
            result = Or(result, predicates[i]);
        return result;
    }
}



