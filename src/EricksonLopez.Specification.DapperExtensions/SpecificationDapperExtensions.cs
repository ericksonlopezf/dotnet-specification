// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.DapperExtensions.UnitOfWork;
using EricksonLopez.Specification;
using EricksonLopez.Specification.Sql;

namespace EricksonLopez.Specification.DapperExtensions;

/// <summary>
/// Provides extension methods for evaluating <see cref="QuerySpec{T}"/> against <see cref="IUnitOfWork"/> within the Dapper extensions ecosystem.
/// </summary>
public static class SpecificationDapperExtensions
{
    /// <summary>
    /// Executes the specification against the connection and transaction managed by the <see cref="IUnitOfWork"/> and returns all matching entities.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="unitOfWork">The active unit of work.</param>
    /// <param name="spec">The query specification.</param>
    /// <param name="translator">The translator for mapping the specification to SQL.</param>
    /// <param name="dialect">The SQL dialect to use for rendering.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with matching entities.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/>, <paramref name="spec"/>, <paramref name="translator"/>, or <paramref name="dialect"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">The UnitOfWork transaction has no associated connection</exception>
    [RequiresUnreferencedCode("SQL translation uses reflection for expression extraction.")]
    public static async Task<IEnumerable<T>> QueryAsync<T>(
        this IUnitOfWork unitOfWork,
        QuerySpec<T> spec,
        QuerySpecTranslator<T> translator,
        ISqlDialect dialect,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(translator);
        ArgumentNullException.ThrowIfNull(dialect);

        var connection = unitOfWork.Transaction.Connection
            ?? throw new InvalidOperationException("The UnitOfWork transaction has no associated connection.");

        var model = translator.Translate(spec);
        var query = dialect.Render(model);

        var cmd = new CommandDefinition(
            commandText: query.Sql,
            parameters: query.Parameters,
            transaction: unitOfWork.Transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await connection.QueryAsync<T>(cmd).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the specification against the <see cref="IUnitOfWork"/> and returns the first matching entity or default.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="unitOfWork">The active unit of work.</param>
    /// <param name="spec">The query specification.</param>
    /// <param name="translator">The translator for mapping the specification to SQL.</param>
    /// <param name="dialect">The SQL dialect to use for rendering.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the entity or default.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/>, <paramref name="spec"/>, <paramref name="translator"/>, or <paramref name="dialect"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">The UnitOfWork transaction has no associated connection</exception>
    [RequiresUnreferencedCode("SQL translation uses reflection for expression extraction.")]
    public static async Task<T?> QueryFirstOrDefaultAsync<T>(
        this IUnitOfWork unitOfWork,
        QuerySpec<T> spec,
        QuerySpecTranslator<T> translator,
        ISqlDialect dialect,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(translator);
        ArgumentNullException.ThrowIfNull(dialect);

        var connection = unitOfWork.Transaction.Connection
            ?? throw new InvalidOperationException("The UnitOfWork transaction has no associated connection.");

        var model = translator.Translate(spec);
        var query = dialect.Render(model);

        var cmd = new CommandDefinition(
            commandText: query.Sql,
            parameters: query.Parameters,
            transaction: unitOfWork.Transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await connection.QueryFirstOrDefaultAsync<T>(cmd).ConfigureAwait(false);
    }

    /// <summary>
    /// Counts the number of entities matching the specification using the <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="unitOfWork">The active unit of work.</param>
    /// <param name="spec">The query specification.</param>
    /// <param name="translator">The translator for mapping the specification to SQL.</param>
    /// <param name="dialect">The SQL dialect to use for rendering.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the count of matching entities.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="unitOfWork"/>, <paramref name="spec"/>, <paramref name="translator"/>, or <paramref name="dialect"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">The UnitOfWork transaction has no associated connection</exception>
    [RequiresUnreferencedCode("SQL translation uses reflection for expression extraction.")]
    public static async Task<int> CountAsync<T>(
        this IUnitOfWork unitOfWork,
        QuerySpec<T> spec,
        QuerySpecTranslator<T> translator,
        ISqlDialect dialect,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(translator);
        ArgumentNullException.ThrowIfNull(dialect);

        var connection = unitOfWork.Transaction.Connection
            ?? throw new InvalidOperationException("The UnitOfWork transaction has no associated connection.");

        var countSpec = spec with
        {
            OrderClauses = [],
            SkipCount = null,
            TakeCount = null
        };

        var model = translator.Translate(countSpec) with { QueryType = SqlQueryType.Count };
        var query = dialect.Render(model);

        var cmd = new CommandDefinition(
            commandText: query.Sql,
            parameters: query.Parameters,
            transaction: unitOfWork.Transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await connection.ExecuteScalarAsync<int>(cmd).ConfigureAwait(false);
    }
}
