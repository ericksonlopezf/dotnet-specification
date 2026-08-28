// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification;
using MongoDB.Driver;

namespace EricksonLopez.Specification.MongoDB;

/// <summary>
/// Provides extension methods for executing specifications against MongoDB collections.
/// </summary>
public static class MongoSpecificationExtensions
{
    /// <summary>
    /// Finds documents matching the specified specification asynchronously.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <param name="collection">The Mongo collection.</param>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of matching documents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static async Task<List<TDocument>> FindAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        QuerySpec<TDocument> specification,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to collection.Find
        ArgumentNullException.ThrowIfNull(collection);
        // Stryker disable once Statement : Delegated null validation to MongoSpecificationEvaluator.GetFilter
        ArgumentNullException.ThrowIfNull(specification);

        var filter = MongoSpecificationEvaluator.GetFilter(specification);
        var findFluent = collection.Find(filter).ApplySpecification(specification);

        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        using var cursor = await findFluent.ToCursorAsync(cancellationToken).ConfigureAwait(false);
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await cursor.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Finds the first document matching the specified specification, or returns default if none match.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <param name="collection">The Mongo collection.</param>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching document, or <see langword="null"/> if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static async Task<TDocument?> FirstOrDefaultAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        QuerySpec<TDocument> specification,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to collection.Find
        ArgumentNullException.ThrowIfNull(collection);
        // Stryker disable once Statement : Delegated null validation to MongoSpecificationEvaluator.GetFilter
        ArgumentNullException.ThrowIfNull(specification);

        var filter = MongoSpecificationEvaluator.GetFilter(specification);
        var findFluent = collection.Find(filter).ApplySpecification(specification);

        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await findFluent.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Counts documents matching the specified specification asynchronously.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <param name="collection">The Mongo collection.</param>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The total count of matching documents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> or <paramref name="specification"/> is <see langword="null"/></exception>
    public static async Task<long> CountDocumentsAsync<TDocument>(
        this IMongoCollection<TDocument> collection,
        QuerySpec<TDocument> specification,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to collection.CountDocumentsAsync
        ArgumentNullException.ThrowIfNull(collection);
        // Stryker disable once Statement : Delegated null validation to MongoSpecificationEvaluator.GetFilter
        ArgumentNullException.ThrowIfNull(specification);

        var filter = MongoSpecificationEvaluator.GetFilter(specification);
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
