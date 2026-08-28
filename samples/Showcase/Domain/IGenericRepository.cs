// Copyright © Erickson Lopez. MIT License.
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Read/Write repository contract for application layer demonstration.
/// Combines <see cref="IReadRepository{T}"/> with write operations.
/// </summary>
/// <typeparam name="T">The entity type managed by the repository.</typeparam>
public interface IGenericRepository<T> : IReadRepository<T>
{
    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    Task RemoveAsync(T entity, CancellationToken cancellationToken = default);
}
