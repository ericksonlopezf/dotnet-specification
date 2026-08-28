// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.Specification.Linq;

/// <summary>
/// Provides extension methods for applying <see cref="QuerySpec{T}"/> to <see cref="IQueryable{T}"/> sources.
/// </summary>
/// <remarks>
/// <para>
/// These extensions apply the specification's criteria, ordering, pagination, and other
/// modifiers to an <see cref="IQueryable{T}"/> source. They work with any IQueryable provider
/// including EF Core, LINQ to Objects, and in-memory collections.
/// </para>
/// <para>
/// The extension methods do NOT call <c>ToList()</c>, <c>ToArray()</c>, or any materializing
/// method — the returned value is always a composed <see cref="IQueryable{T}"/> ready for
/// further composition or execution.
/// </para>
/// </remarks>
public static class QuerySpecLinqExtensions
{
    /// <summary>
    /// Applies a <see cref="QuerySpec{T}"/> to an <see cref="IQueryable{T}"/> source.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="spec">The specification to apply.</param>
    /// <returns>A queryable with all specification criteria, ordering, and pagination applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="spec"/> is <see langword="null"/></exception>
    public static IQueryable<T> Apply<T>(this IQueryable<T> source, QuerySpec<T> spec)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(spec);

        var query = source;

        // Apply filter criteria (AND-combined)
        var predicate = spec.BuildCombinedPredicate();
        if (predicate is not null)
            query = query.Where(predicate);

        // Apply cursor pagination filter
        if (spec.Cursor is not null)
        {
            var cursorPredicate = BuildCursorPredicate(spec.Cursor);
            query = query.Where(cursorPredicate);
        }

        // Apply ordering
        if (!spec.OrderClauses.IsEmpty)
        {
            IOrderedQueryable<T>? ordered = null;
            foreach (var clause in spec.OrderClauses)
            {
                if (ordered is null)
                {
                    ordered = clause.Direction == OrderDirection.Ascending
                        ? query.OrderBy(clause.KeySelector)
                        : query.OrderByDescending(clause.KeySelector);
                }
                else
                {
                    ordered = clause.Direction == OrderDirection.Ascending
                        ? ordered.ThenBy(clause.KeySelector)
                        : ordered.ThenByDescending(clause.KeySelector);
                }
            }
            query = ordered!;
        }

        // Apply distinct
        if (spec.IsDistinct)
            query = query.Distinct();

        // Apply pagination
        if (spec.SkipCount.HasValue)
            query = query.Skip(spec.SkipCount.Value);

        if (spec.TakeCount.HasValue)
            query = query.Take(spec.TakeCount.Value);

        return query;
    }

    /// <summary>
    /// Applies a projected <see cref="QuerySpec{T, TResult}"/> to an <see cref="IQueryable{T}"/> source.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="spec">The projected specification to apply.</param>
    /// <returns>A queryable of projected results.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="spec"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException"><paramref name="spec"/> does not define a projection selector</exception>
    public static IQueryable<TResult> Apply<T, TResult>(
        this IQueryable<T> source,
        QuerySpec<T, TResult> spec)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(spec);

        var query = source;

        // Apply filter criteria
        var predicate = spec.BuildCombinedPredicate();
        if (predicate is not null)
            query = query.Where(predicate);

        // Apply cursor pagination filter
        if (spec.Cursor is not null)
        {
            var cursorPredicate = BuildCursorPredicate(spec.Cursor);
            query = query.Where(cursorPredicate);
        }

        // Apply ordering before projection
        if (!spec.OrderClauses.IsEmpty)
        {
            IOrderedQueryable<T>? ordered = null;
            foreach (var clause in spec.OrderClauses)
            {
                if (ordered is null)
                {
                    ordered = clause.Direction == OrderDirection.Ascending
                        ? query.OrderBy(clause.KeySelector)
                        : query.OrderByDescending(clause.KeySelector);
                }
                else
                {
                    ordered = clause.Direction == OrderDirection.Ascending
                        ? ordered.ThenBy(clause.KeySelector)
                        : ordered.ThenByDescending(clause.KeySelector);
                }
            }
            query = ordered!;
        }

        // Apply distinct
        if (spec.IsDistinct)
            query = query.Distinct();

        // Apply pagination before projection
        if (spec.SkipCount.HasValue)
            query = query.Skip(spec.SkipCount.Value);

        if (spec.TakeCount.HasValue)
            query = query.Take(spec.TakeCount.Value);

        // Apply projection (must be last)
        if (spec.Selector is not null)
            return query.Select(spec.Selector);

        // Stryker disable once String : Trivial exception message
        throw new InvalidOperationException(
            "QuerySpec<T, TResult> requires a projection defined via .Select(x => ...). " +
            "Use QuerySpec<T> if no projection is needed.");
    }

    /// <summary>
    /// Returns the combined AND predicate from a projected <see cref="QuerySpec{T, TResult}"/>.
    /// </summary>
    internal static System.Linq.Expressions.Expression<Func<T, bool>>? BuildCombinedPredicate<T, TResult>(
        this QuerySpec<T, TResult> spec)
    {
        if (spec.Criteria.IsEmpty) return null;
        if (spec.Criteria.Length == 1) return spec.Criteria[0];
        return ExpressionComposer.AndAll<T>(spec.Criteria.AsSpan());
    }

    internal static System.Linq.Expressions.Expression<Func<T, bool>> BuildCursorPredicate<T>(CursorClause<T> cursor)
    {
        var param = cursor.KeySelector.Parameters[0];
        var body = cursor.KeySelector.Body;

        var actualExpr = body;
        if (body is System.Linq.Expressions.UnaryExpression u)
        {
            if (u.NodeType == System.Linq.Expressions.ExpressionType.Convert)
            {
                actualExpr = u.Operand;
            }
        }

        var valueConstant = System.Linq.Expressions.Expression.Constant(cursor.Value, actualExpr.Type);

        var comparison = cursor.Direction == CursorDirection.After
            ? System.Linq.Expressions.Expression.GreaterThan(actualExpr, valueConstant)
            : System.Linq.Expressions.Expression.LessThan(actualExpr, valueConstant);

        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(comparison, param);
    }

    /// <summary>
    /// Determines whether any element in the source satisfies the specified specification predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The specification whose predicate to test.</param>
    /// <returns><see langword="true"/> if any element satisfies the predicate; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static bool Any<T>(this IQueryable<T> source, IExpressionSpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.Any(specification.ToExpression());
    }

    /// <summary>
    /// Calculates the number of elements in the source that satisfy the specified specification predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The specification whose predicate to test.</param>
    /// <returns>The number of elements satisfying the specification.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static int Count<T>(this IQueryable<T> source, IExpressionSpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.Count(specification.ToExpression());
    }
}



