// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Specification;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Specification.EntityFrameworkCore;

/// <summary>
/// Provides an Entity Framework Core implementation of <see cref="IReadRepository{T}"/> for a specific <typeparamref name="TDbContext"/>.
/// </summary>
/// <typeparam name="TDbContext">The concrete database context type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class EfReadRepository<
    TDbContext,
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicFields |
        DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.NonPublicProperties |
        DynamicallyAccessedMemberTypes.Interfaces)] TEntity> : IReadRepository<TEntity>
    where TDbContext : DbContext
    where TEntity : class
{
    private readonly TDbContext _dbContext;
    private readonly ISpecificationEvaluator _evaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfReadRepository{TDbContext, TEntity}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="evaluator">The optional specification evaluator.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/></exception>
    public EfReadRepository(TDbContext dbContext, ISpecificationEvaluator? evaluator = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _evaluator = evaluator ?? EfSpecificationEvaluator.Default;
    }

    /// <summary>
    /// Gets the entity set for <typeparamref name="TEntity"/>.
    /// </summary>
    protected DbSet<TEntity> DbSet => _dbContext.Set<TEntity>();

    /// <inheritdoc/>
    public virtual async Task<TEntity?> GetByIdAsync<TId>(
        TId id,
        CancellationToken cancellationToken = default) where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(id);
        cancellationToken.ThrowIfCancellationRequested();
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await DbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<TEntity?> FirstOrDefaultAsync(
        QuerySpec<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to EfSpecificationEvaluator
        ArgumentNullException.ThrowIfNull(specification);
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await _evaluator.GetQuery(DbSet.AsNoTracking(), specification)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TEntity>> ListAsync(
        QuerySpec<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to EfSpecificationEvaluator
        ArgumentNullException.ThrowIfNull(specification);
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        var results = await _evaluator.GetQuery(DbSet.AsNoTracking(), specification)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return results;
    }

    /// <inheritdoc/>
    public async Task<int> CountAsync(
        QuerySpec<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to EfSpecificationEvaluator
        ArgumentNullException.ThrowIfNull(specification);
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await _evaluator.GetQuery(DbSet.AsNoTracking(), specification)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> AnyAsync(
        QuerySpec<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to EfSpecificationEvaluator
        ArgumentNullException.ThrowIfNull(specification);
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await _evaluator.GetQuery(DbSet.AsNoTracking(), specification)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<TEntity?> SingleOrDefaultAsync(
        QuerySpec<TEntity> specification,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to EfSpecificationEvaluator
        ArgumentNullException.ThrowIfNull(specification);
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await _evaluator.GetQuery(DbSet.AsNoTracking(), specification)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        QuerySpec<TEntity, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        // Stryker disable once Statement : Delegated null validation to EfSpecificationEvaluator
        ArgumentNullException.ThrowIfNull(specification);
        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        var results = await _evaluator.GetQuery(DbSet.AsNoTracking(), specification)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return results;
    }
}




