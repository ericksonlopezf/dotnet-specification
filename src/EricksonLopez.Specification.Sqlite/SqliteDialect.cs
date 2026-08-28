// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Specification.Sql;

namespace EricksonLopez.Specification.Sqlite;

/// <summary>
/// Renders <see cref="QueryModel"/> instances as SQLite-compatible SQL.
/// </summary>
/// <remarks>
/// <para>
/// Key SQLite-specific behaviors:
/// </para>
/// <list type="bullet">
/// <item>Identifiers are quoted with double-quotes: <c>"table_name"</c></item>
/// <item>Parameters use the named format: <c>@paramName</c></item>
/// <item><c>LIMIT n OFFSET m</c> for pagination</item>
/// <item><c>IN (@p_0, @p_1, ...)</c> for collection membership — parameters are expanded inline</item>
/// <item><c>LIKE</c> only — no ILIKE (SQLite LIKE is case-insensitive for ASCII by default)</item>
/// </list>
/// <para>
/// Primary use case: integration and unit testing without an external server.
/// For production use cases, prefer <c>PostgreSqlDialect</c> or <c>MsSqlDialect</c>.
/// </para>
/// <para>
/// <b>Note on case-insensitive LIKE</b>: SQLite's LIKE is case-insensitive for ASCII by default.
/// Enable <c>PRAGMA case_sensitive_like = ON</c> at the connection level to opt into case-sensitive matching.
/// </para>
/// </remarks>
public sealed class SqliteDialect : SqlDialectBase
{
    /// <summary>Gets the shared singleton instance with default configuration.</summary>
    public static readonly SqliteDialect Default = new();

    /// <inheritdoc/>
    public override string DialectName => "SQLite";

    /// <inheritdoc/>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is <see langword="null"/> or whitespace</exception>
    public override string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        // Stryker disable once String : Quoting empty identifier is rejected by ThrowIfNullOrWhiteSpace
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
