// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using EricksonLopez.Specification;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Specification.EntityFrameworkCore;

/// <summary>
/// Provides EF Core-specific extension methods for query specification configuration and execution.
/// </summary>
public static class QuerySpecEfCoreExtensions
{
    /// <summary>
    /// Applies the specification to the source <see cref="IQueryable{T}"/> with optional split query and auto-includes configuration.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The query specification.</param>
    /// <param name="asSplitQuery">If <see langword="true"/>, configures the query to use multiple SQL queries (<c>AsSplitQuery</c>).</param>
    /// <param name="ignoreAutoIncludes">If <see langword="true"/>, configures the query to ignore auto-included navigations (<c>IgnoreAutoIncludes</c>).</param>
    /// <returns>The configured queryable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static IQueryable<T> Apply<T>(
        this IQueryable<T> source,
        QuerySpec<T> specification,
        bool asSplitQuery = false,
        bool ignoreAutoIncludes = false) where T : class
    {
        // Stryker disable once Statement : Delegated null validation to EfSpecificationEvaluator.GetQuery
        ArgumentNullException.ThrowIfNull(source);
        // Stryker disable once Statement : Delegated null validation to EfSpecificationEvaluator.GetQuery
        ArgumentNullException.ThrowIfNull(specification);

        if (asSplitQuery)
        {
            source = RelationalQueryableExtensions.AsSplitQuery(source);
        }

        if (ignoreAutoIncludes)
        {
            source = EntityFrameworkQueryableExtensions.IgnoreAutoIncludes(source);
        }

        return EfSpecificationEvaluator.GetQuery(source, specification);
    }

    /// <summary>
    /// Applies the projected specification to the source <see cref="IQueryable{T}"/> with optional split query and auto-includes configuration.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="specification">The projected query specification.</param>
    /// <param name="asSplitQuery">If <see langword="true"/>, configures the query to use multiple SQL queries (<c>AsSplitQuery</c>).</param>
    /// <param name="ignoreAutoIncludes">If <see langword="true"/>, configures the query to ignore auto-included navigations (<c>IgnoreAutoIncludes</c>).</param>
    /// <returns>The configured queryable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static IQueryable<TResult> Apply<T, TResult>(
        this IQueryable<T> source,
        QuerySpec<T, TResult> specification,
        bool asSplitQuery = false,
        bool ignoreAutoIncludes = false) where T : class
    {
        // Stryker disable once Statement : Delegated null validation to EfSpecificationEvaluator.GetQuery
        ArgumentNullException.ThrowIfNull(source);
        // Stryker disable once Statement : Delegated null validation to EfSpecificationEvaluator.GetQuery
        ArgumentNullException.ThrowIfNull(specification);

        if (asSplitQuery)
        {
            source = RelationalQueryableExtensions.AsSplitQuery(source);
        }

        if (ignoreAutoIncludes)
        {
            source = EntityFrameworkQueryableExtensions.IgnoreAutoIncludes(source);
        }

        return EfSpecificationEvaluator.GetQuery(source, specification);
    }
}

