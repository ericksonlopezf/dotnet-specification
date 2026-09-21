// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

        // Apply filter criteria
        foreach (var criterion in spec.Criteria)
        {
            query = query.Where(criterion);
        }

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
        foreach (var criterion in spec.Criteria)
        {
            query = query.Where(criterion);
        }

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

        throw new InvalidOperationException(
            "Projected QuerySpec<T, TResult> must define a Selector expression. " +
            "Use QuerySpec<T> if no projection is needed.");
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

        if (!typeof(IComparable).IsAssignableFrom(actualExpr.Type) &&
            Nullable.GetUnderlyingType(actualExpr.Type) is null)
        {
            throw new NotSupportedException(
                $"Keyset cursor pagination on type '{actualExpr.Type.Name}' is not supported. " +
                "Cursor key selector must target a comparable scalar property.");
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
    public static bool Any<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>(this IQueryable<T> source, IExpressionSpecification<T> specification)
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
    public static int Count<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>(this IQueryable<T> source, IExpressionSpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.Count(specification.ToExpression());
    }

    /// <summary>
    /// Filters a sequence of values based on a predicate defined by an expression specification.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The specification whose predicate to test.</param>
    /// <returns>An <see cref="IQueryable{T}"/> that contains elements from the input sequence that satisfy the specification.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static IQueryable<T> Where<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>(this IQueryable<T> source, IExpressionSpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.Where(specification.ToExpression());
    }

    /// <summary>
    /// Determines whether all elements in the source satisfy the specified specification predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The specification whose predicate to test.</param>
    /// <returns><see langword="true"/> if every element satisfies the predicate, or if the source is empty; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static bool All<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>(this IQueryable<T> source, IExpressionSpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.All(specification.ToExpression());
    }

    /// <summary>
    /// Returns the first element of a sequence that satisfies an expression specification, or a default value if no such element is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The specification whose predicate to test.</param>
    /// <returns><see langword="default"/>(<typeparamref name="T"/>) if <paramref name="source"/> is empty or if no element passes the test; otherwise, the first matching element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static T? FirstOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>(this IQueryable<T> source, IExpressionSpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.FirstOrDefault(specification.ToExpression());
    }

    /// <summary>
    /// Filters an in-memory sequence of values based on a domain specification.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to filter.</param>
    /// <param name="specification">A domain specification to test each element for a condition.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> that contains elements from the input sequence that satisfy the specification.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static IEnumerable<T> Where<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>(this IEnumerable<T> source, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.Where(specification.IsSatisfiedBy);
    }

    /// <summary>
    /// Determines whether any element of an in-memory sequence satisfies a domain specification.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to evaluate.</param>
    /// <param name="specification">A domain specification to test each element for a condition.</param>
    /// <returns><see langword="true"/> if any elements in the source sequence satisfy the condition; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static bool Any<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>(this IEnumerable<T> source, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.Any(specification.IsSatisfiedBy);
    }

    /// <summary>
    /// Determines whether all elements of an in-memory sequence satisfy a domain specification.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to evaluate.</param>
    /// <param name="specification">A domain specification to test each element for a condition.</param>
    /// <returns><see langword="true"/> if every element passes the test in the specified specification, or if the sequence is empty; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static bool All<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>(this IEnumerable<T> source, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.All(specification.IsSatisfiedBy);
    }

    /// <summary>
    /// Returns the number of elements in an in-memory sequence that satisfy a domain specification.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to evaluate.</param>
    /// <param name="specification">A domain specification to test each element for a condition.</param>
    /// <returns>A number that represents how many elements in the sequence satisfy the condition in the specification.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static int Count<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>(this IEnumerable<T> source, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.Count(specification.IsSatisfiedBy);
    }

    /// <summary>
    /// Returns the first element of an in-memory sequence that satisfies a domain specification, or a default value if no such element is found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from.</param>
    /// <param name="specification">A domain specification to test each element for a condition.</param>
    /// <returns><see langword="default"/>(<typeparamref name="T"/>) if <paramref name="source"/> is empty or if no element passes the test; otherwise, the first matching element.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static T? FirstOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>(this IEnumerable<T> source, ISpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(specification);
        return source.FirstOrDefault(specification.IsSatisfiedBy);
    }
}



