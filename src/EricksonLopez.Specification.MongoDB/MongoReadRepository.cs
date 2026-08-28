// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification;
using MongoDB.Driver;

namespace EricksonLopez.Specification.MongoDB;

/// <summary>
/// Provides a specification-driven asynchronous read repository implementation for MongoDB collections.
/// </summary>
/// <typeparam name="TDocument">The document type.</typeparam>
public class MongoReadRepository<TDocument>
{
    private readonly IMongoCollection<TDocument> _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoReadRepository{TDocument}"/> class.
    /// </summary>
    /// <param name="collection">The Mongo collection instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/></exception>
    public MongoReadRepository(IMongoCollection<TDocument> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        _collection = collection;
    }

    /// <summary>
    /// Retrieves all documents matching the specified query specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of matching documents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/></exception>
    public virtual Task<List<TDocument>> ListAsync(QuerySpec<TDocument> specification, CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to MongoSpecificationExtensions.FindAsync
        ArgumentNullException.ThrowIfNull(specification);
        return _collection.FindAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Retrieves the first document matching the specified query specification, or default if none match.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching document, or <see langword="null"/> if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/></exception>
    public virtual Task<TDocument?> FirstOrDefaultAsync(QuerySpec<TDocument> specification, CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to MongoSpecificationExtensions.FirstOrDefaultAsync
        ArgumentNullException.ThrowIfNull(specification);
        return _collection.FirstOrDefaultAsync(specification, cancellationToken);
    }

    /// <summary>
    /// Calculates the number of documents matching the specified query specification.
    /// </summary>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The count of matching documents.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/></exception>
    public virtual Task<long> CountAsync(QuerySpec<TDocument> specification, CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to MongoSpecificationExtensions.CountDocumentsAsync
        ArgumentNullException.ThrowIfNull(specification);
        return _collection.CountDocumentsAsync(specification, cancellationToken);
    }
}
