// Copyright © Erickson Lopez. MIT License.
using System;

namespace Microsoft.Extensions.DependencyInjection;

using System.Diagnostics.CodeAnalysis;
using EricksonLopez.Specification;
using EricksonLopez.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Provides extension methods for registering EF Core specification services and repositories in an <see cref="IServiceCollection"/>.
/// </summary>
public static class SpecificationEntityFrameworkServiceCollectionExtensions
{
    /// <summary>
    /// Registers generic <see cref="IReadRepository{T}"/> mapped to <see cref="EfReadRepository{T}"/>.
    /// </summary>
    /// <typeparam name="TDbContext">The concrete database context type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddSpecificationEntityFramework<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        // Stryker disable once Statement : Delegated null validation to ServiceCollection
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISpecificationEvaluator>(EfSpecificationEvaluator.Default);
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());
        services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
        return services;
    }

    /// <summary>
    /// Registers generic <see cref="IReadRepository{T}"/> mapped to <see cref="EfReadRepository{T}"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddSpecificationEntityFramework(this IServiceCollection services)
    {
        // Stryker disable once Statement : Delegated null validation to ServiceCollection
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISpecificationEvaluator>(EfSpecificationEvaluator.Default);
        services.AddScoped(typeof(IReadRepository<>), typeof(EfReadRepository<>));
        return services;
    }

    /// <summary>
    /// Registers strongly-typed <see cref="IReadRepository{TEntity}"/> backed by <see cref="EfReadRepository{TDbContext, TEntity}"/>.
    /// </summary>
    /// <typeparam name="TDbContext">The concrete database context type.</typeparam>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The modified service collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddEfReadRepository<
        TDbContext,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.NonPublicConstructors |
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.NonPublicFields |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.NonPublicProperties |
            DynamicallyAccessedMemberTypes.Interfaces)] TEntity>(this IServiceCollection services)
        where TDbContext : DbContext
        where TEntity : class
    {
        // Stryker disable once Statement : Delegated null validation to ServiceCollection
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IReadRepository<TEntity>, EfReadRepository<TDbContext, TEntity>>();
        return services;
    }
}



