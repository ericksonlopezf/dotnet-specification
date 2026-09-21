// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using EricksonLopez.Specification;
using MongoDB.Driver;

namespace EricksonLopez.Specification.MongoDB;

/// <summary>
/// Evaluates and compiles <see cref="QuerySpec{T}"/> descriptors into MongoDB filter and sort definitions.
/// </summary>
public static class MongoSpecificationEvaluator
{
    /// <summary>
    /// Builds a MongoDB <see cref="FilterDefinition{TDocument}"/> from the specified specification criteria.
    /// </summary>
    /// <typeparam name="TDocument">The MongoDB document type.</typeparam>
    /// <param name="specification">The query specification.</param>
    /// <returns>A combined <see cref="FilterDefinition{TDocument}"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/></exception>
    public static FilterDefinition<TDocument> GetFilter<TDocument>(QuerySpec<TDocument> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        var builder = Builders<TDocument>.Filter;
        var filter = builder.Empty;

        foreach (var criterion in specification.Criteria)
        {
            filter &= builder.Where(criterion);
        }

        return filter;
    }

    /// <summary>
    /// Builds a MongoDB <see cref="SortDefinition{TDocument}"/> from the specified specification ordering expressions.
    /// </summary>
    /// <typeparam name="TDocument">The MongoDB document type.</typeparam>
    /// <param name="specification">The query specification.</param>
    /// <returns>A combined <see cref="SortDefinition{TDocument}"/>, or <see langword="null"/> if no order was specified.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/></exception>
    public static SortDefinition<TDocument>? GetSort<TDocument>(QuerySpec<TDocument> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);

        // Stryker disable Block,Equality : Early return optimization for empty order clauses
        if (specification.OrderClauses.Length == 0)
        {
            return null;
        }
        // Stryker restore Block,Equality

        var builder = Builders<TDocument>.Sort;
        SortDefinition<TDocument>? combinedSort = null;

        foreach (var order in specification.OrderClauses)
        {
            var sortDef = order.Direction == OrderDirection.Ascending
                ? builder.Ascending(new ExpressionFieldDefinition<TDocument, object?>(order.KeySelector))
                : builder.Descending(new ExpressionFieldDefinition<TDocument, object?>(order.KeySelector));

            combinedSort = combinedSort is null ? sortDef : builder.Combine(combinedSort, sortDef);
        }

        return combinedSort;
    }

    /// <summary>
    /// Applies specification criteria, sorting, and pagination to an <see cref="IFindFluent{TDocument, TDocument}"/> query.
    /// </summary>
    /// <typeparam name="TDocument">The MongoDB document type.</typeparam>
    /// <param name="findFluent">The source fluent find instance.</param>
    /// <param name="specification">The query specification.</param>
    /// <returns>The configured fluent find query.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="findFluent"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static IFindFluent<TDocument, TDocument> ApplySpecification<TDocument>(
        this IFindFluent<TDocument, TDocument> findFluent,
        QuerySpec<TDocument> specification)
    {
        ArgumentNullException.ThrowIfNull(findFluent);
        // Stryker disable once Statement : Delegated null validation to GetSort
        ArgumentNullException.ThrowIfNull(specification);

        var filter = GetFilter(specification);
        if (filter != Builders<TDocument>.Filter.Empty)
        {
            findFluent.Filter = (findFluent.Filter is null || findFluent.Filter == Builders<TDocument>.Filter.Empty)
                ? filter
                : Builders<TDocument>.Filter.And(findFluent.Filter, filter);
        }

        var sort = GetSort(specification);
        if (sort is not null)
        {
            findFluent = findFluent.Sort(sort);
        }

        if (specification.SkipCount.HasValue)
        {
            findFluent = findFluent.Skip(specification.SkipCount.Value);
        }

        if (specification.TakeCount.HasValue)
        {
            findFluent = findFluent.Limit(specification.TakeCount.Value);
        }

        return findFluent;
    }

    /// <summary>
    /// Executes a find query on the specified collection using the criteria, ordering, and pagination from a <see cref="QuerySpec{T}"/>.
    /// </summary>
    /// <typeparam name="TDocument">The MongoDB document type.</typeparam>
    /// <param name="collection">The source MongoDB collection.</param>
    /// <param name="specification">The query specification.</param>
    /// <returns>A configured fluent find instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static IFindFluent<TDocument, TDocument> Find<TDocument>(
        this IMongoCollection<TDocument> collection,
        QuerySpec<TDocument> specification)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(specification);

        var filter = GetFilter(specification);
        var findFluent = collection.Find(filter);
        return findFluent.ApplySpecification(specification);
    }
}
