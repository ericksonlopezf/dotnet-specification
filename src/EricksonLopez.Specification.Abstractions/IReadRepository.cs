// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Specification;

/// <summary>
/// Defines a read-only repository contract that operates on <see cref="QuerySpec{T}"/> descriptors.
/// Implementations translate specifications to provider-specific queries.
/// </summary>
/// <typeparam name="T">The entity type managed by this repository.</typeparam>
public interface IReadRepository<T>
{
    /// <summary>
    /// Retrieves the first entity matching the specification, or <see langword="null"/> if none found.
    /// </summary>
    /// <param name="specification">The query specification to apply.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the first matching entity, or <see langword="null"/> if not found.
    /// </returns>
    Task<T?> FirstOrDefaultAsync(
        QuerySpec<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities matching the specification.
    /// </summary>
    /// <param name="specification">The query specification to apply.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains a read-only list of matching entities.
    /// </returns>
    Task<IReadOnlyList<T>> ListAsync(
        QuerySpec<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the number of entities matching the specification.
    /// </summary>
    /// <param name="specification">The query specification to apply.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the count of matching entities.
    /// </returns>
    Task<int> CountAsync(
        QuerySpec<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any entity matches the specification.
    /// </summary>
    /// <param name="specification">The query specification to apply.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains <see langword="true"/> if at least one entity matches; otherwise, <see langword="false"/>.
    /// </returns>
    Task<bool> AnyAsync(
        QuerySpec<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the single entity matching the specification, or <see langword="null"/> if none found.
    /// </summary>
    /// <param name="specification">The query specification to apply.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the single matching entity, or <see langword="null"/> if not found.
    /// </returns>
    /// <exception cref="InvalidOperationException">More than one entity matches the specification</exception>
    Task<T?> SingleOrDefaultAsync(
        QuerySpec<T> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves projected results for entities matching the specification.
    /// </summary>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="specification">The query specification with projection.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains a read-only list of projected results.
    /// </returns>
    Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        QuerySpec<T, TResult> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an entity by its identifier.
    /// </summary>
    /// <typeparam name="TId">The identifier type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the matching entity, or <see langword="null"/> if not found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The default interface implementation always returns <see langword="null"/> (equivalent to
    /// <c>Task.FromResult&lt;T?&gt;(default)</c>).
    /// </para>
    /// <para>
    /// <strong>Implementors must override this method</strong> to provide actual entity lookup by identifier.
    /// If not overridden, any call to <c>GetByIdAsync</c> will silently return <see langword="null"/>
    /// regardless of whether the entity exists, which can cause <see cref="System.NullReferenceException"/>
    /// in callers that do not expect a <see langword="null"/> result.
    /// </para>
    /// </remarks>
    Task<T?> GetByIdAsync<TId>(
        TId id,
        CancellationToken cancellationToken = default) where TId : notnull
        => Task.FromResult<T?>(default);
}





