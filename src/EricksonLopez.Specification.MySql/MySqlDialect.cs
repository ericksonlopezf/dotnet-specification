// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text;
using EricksonLopez.Specification.Sql;

namespace EricksonLopez.Specification.MySql;

/// <summary>
/// Renders <see cref="QueryModel"/> instances as MySQL- and MariaDB-compatible SQL.
/// </summary>
/// <remarks>
/// <para>
/// Key MySQL/MariaDB-specific behaviors:
/// </para>
/// <list type="bullet">
/// <item>Identifiers are quoted with backticks: <c>`table_name`</c></item>
/// <item>Parameters use the named format: <c>@paramName</c></item>
/// <item><c>LIMIT n OFFSET m</c> for pagination</item>
/// <item><c>IN (@p_0, @p_1, ...)</c> for collection membership — parameters are expanded inline</item>
/// <item><c>LIKE</c> string matching with <c>%</c> wildcards</item>
/// </list>
/// </remarks>
public sealed class MySqlDialect : SqlDialectBase
{
    /// <summary>Gets the shared singleton instance with default configuration.</summary>
    public static readonly MySqlDialect Default = new();

    /// <inheritdoc/>
    public override string DialectName => "MySQL";

    /// <inheritdoc/>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is <see langword="null"/> or whitespace</exception>
    public override string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        // Stryker disable once String : Quoting empty identifier is rejected by ThrowIfNullOrWhiteSpace
        return $"`{identifier.Replace("`", "``", StringComparison.Ordinal)}`";
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/>, <paramref name="model"/>, or <paramref name="parameters"/> is <see langword="null"/></exception>
    protected override void RenderPagination(StringBuilder sql, QueryModel model, Dictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(parameters);

        if (model.Take.HasValue)
        {
            sql.Append(" LIMIT @_take");
            parameters["_take"] = model.Take.Value;
        }

        if (model.Skip.HasValue)
        {
            if (!model.Take.HasValue)
            {
                // MySQL requires a LIMIT when OFFSET is provided
                sql.Append(" LIMIT 18446744073709551615 OFFSET @_skip");
            }
            else
            {
                sql.Append(" OFFSET @_skip");
            }
            parameters["_skip"] = model.Skip.Value;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="fullText"/> is <see langword="null"/></exception>
    protected override void RenderFullTextPredicate(StringBuilder sql, FullTextPredicateNode fullText)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(fullText);

        sql.Append("MATCH(").Append(QuoteIdentifier(fullText.ColumnName)).Append(") AGAINST(@").Append(fullText.ParameterName).Append(" IN NATURAL LANGUAGE MODE)");
    }
}
