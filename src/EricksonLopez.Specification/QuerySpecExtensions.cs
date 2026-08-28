// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Provides extension methods for combining and working with <see cref="QuerySpec{T}"/> instances.
/// </summary>
public static class QuerySpecExtensions
{
    /// <summary>
    /// Combines a query specification with a domain specification using logical AND.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="querySpec">The query specification to extend.</param>
    /// <param name="specification">The domain specification to apply.</param>
    /// <returns>A new query specification with the specification predicate appended.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="querySpec"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static QuerySpec<T> And<
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties |
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] T>(
        this QuerySpec<T> querySpec,
        Specification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(querySpec);
        ArgumentNullException.ThrowIfNull(specification);
        return querySpec.Where(specification.ToExpression());
    }

    /// <summary>
    /// Adds a domain specification predicate as an AND filter to the query specification.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="querySpec">The query specification to extend.</param>
    /// <param name="specification">The domain specification to apply as a filter.</param>
    /// <returns>A new query specification with the specification predicate appended.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="querySpec"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    /// <remarks>
    /// This overload accepts any <see cref="IExpressionSpecification{T}"/> allowing both
    /// <see cref="Specification{T}"/> subclasses and lambda-based specifications created via <see cref="Spec.For{T}"/>
    /// to be used interchangeably with <c>.Where()</c> syntax.
    /// </remarks>
    public static QuerySpec<T> Where<
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties |
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] T>(
        this QuerySpec<T> querySpec,
        IExpressionSpecification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(querySpec);
        ArgumentNullException.ThrowIfNull(specification);
        return querySpec.Where(specification.ToExpression());
    }

    /// <summary>
    /// Combines all criteria in the specification into a single composed AND predicate.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="querySpec">The query specification.</param>
    /// <returns>A combined AND predicate, or <see langword="null"/> if no criteria exist.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="querySpec"/> is <see langword="null"/></exception>
    public static Expression<Func<T, bool>>? BuildCombinedPredicate<T>(this QuerySpec<T> querySpec)
    {
        ArgumentNullException.ThrowIfNull(querySpec);

        if (querySpec.Criteria.IsEmpty)
            return null;

        if (querySpec.Criteria.Length == 1)
            return querySpec.Criteria[0];

        return ExpressionComposer.AndAll<T>(querySpec.Criteria.AsSpan());
    }

    /// <summary>
    /// Determines whether the query specification has any ordering defined.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="querySpec">The query specification to inspect.</param>
    /// <returns><see langword="true"/> if at least one ordering clause is defined; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="querySpec"/> is <see langword="null"/></exception>
    public static bool HasOrdering<T>(this QuerySpec<T> querySpec)
    {
        ArgumentNullException.ThrowIfNull(querySpec);
        return !querySpec.OrderClauses.IsEmpty;
    }

    /// <summary>
    /// Determines whether the query specification has pagination defined.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="querySpec">The query specification to inspect.</param>
    /// <returns><see langword="true"/> if a skip or take count is defined; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="querySpec"/> is <see langword="null"/></exception>
    public static bool HasPagination<T>(this QuerySpec<T> querySpec)
    {
        ArgumentNullException.ThrowIfNull(querySpec);
        return querySpec.SkipCount.HasValue || querySpec.TakeCount.HasValue;
    }

    /// <summary>
    /// Determines whether the query specification has any filter criteria.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="querySpec">The query specification to inspect.</param>
    /// <returns><see langword="true"/> if at least one filter predicate is defined; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="querySpec"/> is <see langword="null"/></exception>
    public static bool HasCriteria<T>(this QuerySpec<T> querySpec)
    {
        ArgumentNullException.ThrowIfNull(querySpec);
        return !querySpec.Criteria.IsEmpty;
    }
}


