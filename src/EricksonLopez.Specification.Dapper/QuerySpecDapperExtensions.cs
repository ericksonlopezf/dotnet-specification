// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Specification.Sql;

namespace EricksonLopez.Specification.Dapper;

/// <summary>
/// Provides extension methods for executing <see cref="QuerySpec{T}"/> against an <see cref="IDbConnection"/>
/// using Dapper and a configured SQL dialect.
/// </summary>
/// <remarks>
/// <para>
/// Usage requires a configured <see cref="ISqlDialect"/> (e.g., <c>PostgreSqlDialect.Default</c>)
/// and a <see cref="QuerySpecTranslator{T}"/> for the entity type.
/// </para>
/// <para>
/// All queries are fully parameterized. No SQL injection is possible through the specification API.
/// </para>
/// </remarks>
public static class QuerySpecDapperExtensions
{
    /// <summary>
    /// Executes the specification as a SQL query and returns all matching results.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="spec">The query specification.</param>
    /// <param name="translator">The translator for mapping the specification to SQL.</param>
    /// <param name="dialect">The SQL dialect to use for rendering.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains all entities matching the specification.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="connection"/>, <paramref name="spec"/>, <paramref name="translator"/>, or <paramref name="dialect"/> is <see langword="null"/>
    /// </exception>
    [RequiresUnreferencedCode(
        "SQL translation via QuerySpecTranslator uses reflection to extract closure values. " +
        "See QuerySpecTranslator.Translate for full trimming guidance.")]
    public static async Task<IEnumerable<T>> QueryAsync<T>(
        this IDbConnection connection,
        QuerySpec<T> spec,
        QuerySpecTranslator<T> translator,
        ISqlDialect dialect,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(translator);
        ArgumentNullException.ThrowIfNull(dialect);

        var model = translator.Translate(spec);
        var query = dialect.Render(model);

        var commandDefinition = new CommandDefinition(
            commandText: query.Sql,
            parameters: query.Parameters,
            transaction: transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await connection.QueryAsync<T>(commandDefinition).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the specification and returns the first matching result, or default.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="spec">The query specification.</param>
    /// <param name="translator">The translator for mapping the specification to SQL.</param>
    /// <param name="dialect">The SQL dialect to use for rendering.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="commandTimeout">An optional commandTimeout in seconds.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the first matching entity, or <see langword="null"/> if no entity matches.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="connection"/>, <paramref name="spec"/>, <paramref name="translator"/>, or <paramref name="dialect"/> is <see langword="null"/>
    /// </exception>
    [RequiresUnreferencedCode(
        "SQL translation via QuerySpecTranslator uses reflection to extract closure values. " +
        "See QuerySpecTranslator.Translate for full trimming guidance.")]
    public static async Task<T?> QueryFirstOrDefaultAsync<T>(
        this IDbConnection connection,
        QuerySpec<T> spec,
        QuerySpecTranslator<T> translator,
        ISqlDialect dialect,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(translator);
        ArgumentNullException.ThrowIfNull(dialect);

        // Add LIMIT 1 for efficient first-or-default query
        var limitedSpec = spec.TakeCount.HasValue ? spec : spec.Take(1);

        var model = translator.Translate(limitedSpec);
        var query = dialect.Render(model);

        var commandDefinition = new CommandDefinition(
            commandText: query.Sql,
            parameters: query.Parameters,
            transaction: transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await connection.QueryFirstOrDefaultAsync<T>(commandDefinition).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a COUNT query based on the specification's filters.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="spec">The query specification (ordering and pagination are ignored for count).</param>
    /// <param name="translator">The translator for mapping the specification to SQL.</param>
    /// <param name="dialect">The SQL dialect to use for rendering.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result contains the count of entities matching the specification's filters.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="connection"/>, <paramref name="spec"/>, <paramref name="translator"/>, or <paramref name="dialect"/> is <see langword="null"/>
    /// </exception>
    [RequiresUnreferencedCode(
        "SQL translation via QuerySpecTranslator uses reflection to extract closure values. " +
        "See QuerySpecTranslator.Translate for full trimming guidance.")]
    public static async Task<int> CountAsync<T>(
        this IDbConnection connection,
        QuerySpec<T> spec,
        QuerySpecTranslator<T> translator,
        ISqlDialect dialect,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(translator);
        ArgumentNullException.ThrowIfNull(dialect);

        // Strip ordering and pagination for count query
        var countSpec = spec with
        {
            OrderClauses = [],
            SkipCount = null,
            TakeCount = null
        };

        var model = translator.Translate(countSpec) with { QueryType = SqlQueryType.Count };
        var query = dialect.Render(model);

        var commandDefinition = new CommandDefinition(
            commandText: query.Sql,
            parameters: query.Parameters,
            transaction: transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        return await connection.ExecuteScalarAsync<int>(commandDefinition).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes an EXISTS query based on the specification's filters.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="spec">The query specification.</param>
    /// <param name="translator">The translator for mapping the specification to SQL.</param>
    /// <param name="dialect">The SQL dialect to use for rendering.</param>
    /// <param name="transaction">An optional transaction to participate in.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// The task result is <see langword="true"/> if at least one entity matches the specification; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="connection"/>, <paramref name="spec"/>, <paramref name="translator"/>, or <paramref name="dialect"/> is <see langword="null"/>
    /// </exception>
    [RequiresUnreferencedCode(
        "SQL translation via QuerySpecTranslator uses reflection to extract closure values. " +
        "See QuerySpecTranslator.Translate for full trimming guidance.")]
    public static async Task<bool> AnyAsync<T>(
        this IDbConnection connection,
        QuerySpec<T> spec,
        QuerySpecTranslator<T> translator,
        ISqlDialect dialect,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(translator);
        ArgumentNullException.ThrowIfNull(dialect);

        // Use LIMIT 1 for efficient exists check
        var existsSpec = spec with
        {
            OrderClauses = [],
            SkipCount = null,
            TakeCount = 1
        };

        var model = translator.Translate(existsSpec) with { QueryType = SqlQueryType.Exists };
        var query = dialect.Render(model);

        var commandDefinition = new CommandDefinition(
            commandText: query.Sql,
            parameters: query.Parameters,
            transaction: transaction,
            commandTimeout: commandTimeout,
            cancellationToken: cancellationToken);

        // Stryker disable once Boolean : Library best practice ConfigureAwait(false) prevents synchronization context capture
        var result = await connection.ExecuteScalarAsync<int?>(commandDefinition).ConfigureAwait(false);
        return result.HasValue;
    }
}





