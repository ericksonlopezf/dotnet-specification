// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Result;
using EricksonLopez.Specification;

namespace EricksonLopez.Specification.Result;

/// <summary>
/// Provides extension methods for <see cref="IReadRepository{T}"/> to execute specifications returning a result.
/// </summary>
public static class ReadRepositoryResultExtensions
{
    /// <summary>
    /// Retrieves the first entity matching the specification, returning a result.
    /// Maps null results to a standard NotFound error.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The read repository.</param>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the matching entity, or a failure result if not found or an error occurs.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/></exception>
#pragma warning disable CA1031 // Do not catch general exception types
    public static async Task<Result<T>> FirstOrDefaultResultAsync<T>(
        this IReadRepository<T> repository,
        QuerySpec<T> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        try
        {
            // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
            var result = await repository.FirstOrDefaultAsync(specification, cancellationToken).ConfigureAwait(false);
            return result is null
                ? EricksonLopez.Result.Result.Failure<T>(Error.NotFound($"{typeof(T).Name}.NotFound", $"No {typeof(T).Name} found matching the specification."))
                : EricksonLopez.Result.Result.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return EricksonLopez.Result.Result.Failure<T>(Error.Failure("Database.Error", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves all entities matching the specification, returning a result list.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The read repository.</param>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing all matching entities, or a failure result if an error occurs.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/></exception>
    public static async Task<Result<IReadOnlyList<T>>> ListResultAsync<T>(
        this IReadRepository<T> repository,
        QuerySpec<T> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        try
        {
            // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
            var result = await repository.ListAsync(specification, cancellationToken).ConfigureAwait(false);
            return EricksonLopez.Result.Result.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return EricksonLopez.Result.Result.Failure<IReadOnlyList<T>>(Error.Failure("Database.Error", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves the single entity matching the specification, returning a result.
    /// Maps null results to NotFound, and multiple matches to a Conflict error.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="repository">The read repository.</param>
    /// <param name="specification">The query specification.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the matching entity, or a failure result if not found, multiple entities match, or an error occurs.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/></exception>
    public static async Task<Result<T>> SingleOrDefaultResultAsync<T>(
        this IReadRepository<T> repository,
        QuerySpec<T> specification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(repository);

        try
        {
            // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
            var result = await repository.SingleOrDefaultAsync(specification, cancellationToken).ConfigureAwait(false);
            return result is null
                ? EricksonLopez.Result.Result.Failure<T>(Error.NotFound($"{typeof(T).Name}.NotFound", $"No {typeof(T).Name} found matching the specification."))
                : EricksonLopez.Result.Result.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            return EricksonLopez.Result.Result.Failure<T>(Error.Conflict("Database.MultipleMatches", "Multiple entities found matching the specification."));
        }
        catch (Exception ex)
        {
            return EricksonLopez.Result.Result.Failure<T>(Error.Failure("Database.Error", ex.Message));
        }
    }

    /// <summary>
    /// Retrieves an entity by its identifier, returning a result.
    /// Maps null results to a standard NotFound error.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TId">The type of the identifier.</typeparam>
    /// <param name="repository">The read repository.</param>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing the entity with the specified identifier, or a failure result if not found or an error occurs.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/></exception>
    public static async Task<Result<T>> GetByIdResultAsync<T, TId>(
        this IReadRepository<T> repository,
        TId id,
        CancellationToken cancellationToken = default) where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(repository);

        try
        {
            // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
            var result = await repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            return result is null
                ? EricksonLopez.Result.Result.Failure<T>(Error.NotFound($"{typeof(T).Name}.NotFound", $"No {typeof(T).Name} found with ID {id}."))
                : EricksonLopez.Result.Result.Success(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return EricksonLopez.Result.Result.Failure<T>(Error.Failure("Database.Error", ex.Message));
        }
    }
#pragma warning restore CA1031 // Do not catch general exception types
}
