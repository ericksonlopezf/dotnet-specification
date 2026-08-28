// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text;
using EricksonLopez.Specification.Sql;

namespace EricksonLopez.Specification.Oracle;

/// <summary>
/// Renders <see cref="QueryModel"/> instances as Oracle Database-compatible SQL.
/// </summary>
/// <remarks>
/// <para>Key Oracle-specific behaviors:</para>
/// <list type="bullet">
/// <item>Identifiers quoted with double quotes: <c>"table_name"</c></item>
/// <item>Parameters use colon prefix: <c>:paramName</c></item>
/// <item>Table aliases in FROM clause do not use <c>AS</c> keyword (Oracle syntax: <c>FROM "table" "alias"</c>)</item>
/// <item>Pagination uses standard Oracle 12c+ <c>OFFSET ... ROWS FETCH NEXT ... ROWS ONLY</c></item>
/// <item>IN predicate expands collections into individual positional/named parameters (<c>:param_0, :param_1</c>)</item>
/// <item>Full-text search uses Oracle Text syntax: <c>CONTAINS("col", :param) &gt; 0</c></item>
/// </list>
/// </remarks>
public sealed class OracleDialect : SqlDialectBase
{
    /// <summary>Gets the shared singleton instance with default configuration.</summary>
    public static readonly OracleDialect Default = new();

    /// <inheritdoc/>
    public override string DialectName => "Oracle";

    /// <inheritdoc/>
    public override string ParameterPrefix => ":";

    /// <inheritdoc/>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is <see langword="null"/> or whitespace</exception>
    public override string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        // Stryker disable once String : Replacing empty string is invalid in identifier quoting
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="model"/> is <see langword="null"/></exception>
    protected override void RenderFromClause(StringBuilder sql, QueryModel model)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(model);

        // Oracle does not allow AS in table aliases
        sql.Append(" FROM ").Append(QuoteIdentifier(model.TableName));
        if (model.TableAlias is not null)
            sql.Append(' ').Append(QuoteIdentifier(model.TableAlias));
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/>, <paramref name="model"/>, or <paramref name="parameters"/> is <see langword="null"/></exception>
    protected override void RenderPagination(StringBuilder sql, QueryModel model, Dictionary<string, object?> parameters)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(model);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(parameters);

        if (model.Skip.HasValue)
        {
            if (model.Orders.IsEmpty)
                sql.Append(" ORDER BY (SELECT NULL FROM DUAL)");

            sql.Append(" OFFSET :_skip ROWS");
            if (model.Take.HasValue)
                sql.Append(" FETCH NEXT :_take ROWS ONLY");
        }
        else if (model.Take.HasValue)
        {
            sql.Append(" FETCH NEXT :_take ROWS ONLY");
        }

        if (model.Take.HasValue) parameters["_take"] = model.Take.Value;
        if (model.Skip.HasValue) parameters["_skip"] = model.Skip.Value;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="fullText"/> is <see langword="null"/></exception>
    protected override void RenderFullTextPredicate(StringBuilder sql, FullTextPredicateNode fullText)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(fullText);

        sql.Append("CONTAINS(").Append(QuoteIdentifier(fullText.ColumnName)).Append(", :").Append(fullText.ParameterName).Append(") > 0");
    }
}
