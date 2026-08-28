// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using EricksonLopez.Specification;

namespace EricksonLopez.Specification.EntityFrameworkCore;

/// <summary>
/// Defines a contract for evaluating and applying specifications to EF Core queryable pipelines.
/// </summary>
public interface ISpecificationEvaluator
{
    /// <summary>
    /// Applies a query specification to the specified source queryable.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The query specification.</param>
    /// <returns>The modified queryable ready for execution.</returns>
    IQueryable<T> GetQuery<T>(IQueryable<T> source, QuerySpec<T> specification);

    /// <summary>
    /// Applies a projected query specification to the specified source queryable.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The projected query specification.</param>
    /// <returns>The modified queryable projected to <typeparamref name="TResult"/>.</returns>
    IQueryable<TResult> GetQuery<T, TResult>(IQueryable<T> source, QuerySpec<T, TResult> specification);
}
