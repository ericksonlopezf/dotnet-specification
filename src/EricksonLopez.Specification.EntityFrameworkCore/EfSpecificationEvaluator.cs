// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using EricksonLopez.Specification;
using EricksonLopez.Specification.Linq;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Specification.EntityFrameworkCore;

/// <summary>
/// Evaluates and applies <see cref="QuerySpec{T}"/> and <see cref="QuerySpec{T, TResult}"/> descriptors to EF Core <see cref="IQueryable{T}"/> query pipelines.
/// </summary>
public class EfSpecificationEvaluator : ISpecificationEvaluator
{
    /// <summary>
    /// Gets the default singleton instance of <see cref="EfSpecificationEvaluator"/>.
    /// </summary>
    public static EfSpecificationEvaluator Default { get; } = new();

    /// <summary>
    /// Applies the specification to the specified source queryable.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The query specification.</param>
    /// <returns>The modified queryable ready for execution.</returns>
    public static IQueryable<T> GetQuery<T>(IQueryable<T> source, QuerySpec<T> specification)
    {
        // Stryker disable once Statement : Delegated null validation to QuerySpecLinqExtensions.Apply
        ArgumentNullException.ThrowIfNull(source);
        // Stryker disable once Statement : Delegated null validation to QuerySpecLinqExtensions.Apply
        ArgumentNullException.ThrowIfNull(specification);

        var query = source.Apply(specification);

        if (!string.IsNullOrWhiteSpace(specification.Tag))
        {
            query = query.TagWith(specification.Tag);
        }

        return query;
    }

    /// <summary>
    /// Applies the projected specification to the specified source queryable.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The projected query specification.</param>
    /// <returns>The modified queryable projected to <typeparamref name="TResult"/>.</returns>
    public static IQueryable<TResult> GetQuery<T, TResult>(IQueryable<T> source, QuerySpec<T, TResult> specification)
    {
        // Stryker disable once Statement : Delegated null validation to QuerySpecLinqExtensions.Apply
        ArgumentNullException.ThrowIfNull(source);
        // Stryker disable once Statement : Delegated null validation to QuerySpecLinqExtensions.Apply
        ArgumentNullException.ThrowIfNull(specification);

        var query = source.Apply(specification);

        if (!string.IsNullOrWhiteSpace(specification.Tag))
        {
            query = query.TagWith(specification.Tag);
        }

        return query;
    }

    /// <inheritdoc/>
    IQueryable<T> ISpecificationEvaluator.GetQuery<T>(IQueryable<T> source, QuerySpec<T> specification)
        => GetQuery(source, specification);

    /// <inheritdoc/>
    IQueryable<TResult> ISpecificationEvaluator.GetQuery<T, TResult>(IQueryable<T> source, QuerySpec<T, TResult> specification)
        => GetQuery(source, specification);
}



