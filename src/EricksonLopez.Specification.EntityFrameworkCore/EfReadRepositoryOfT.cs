// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace EricksonLopez.Specification.EntityFrameworkCore;

/// <summary>
/// Provides an Entity Framework Core implementation of <see cref="IReadRepository{T}"/> for the default <see cref="DbContext"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class EfReadRepository<
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicFields |
        DynamicallyAccessedMemberTypes.NonPublicFields |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.NonPublicProperties |
        DynamicallyAccessedMemberTypes.Interfaces)] TEntity> : EfReadRepository<DbContext, TEntity>
    where TEntity : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfReadRepository{TEntity}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="evaluator">The optional specification evaluator.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dbContext"/> is <see langword="null"/></exception>
    public EfReadRepository(DbContext dbContext, ISpecificationEvaluator? evaluator = null)
        : base(dbContext, evaluator)
    {
    }
}
