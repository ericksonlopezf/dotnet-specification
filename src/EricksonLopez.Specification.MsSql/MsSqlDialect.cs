// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text;
using EricksonLopez.Specification.Sql;

namespace EricksonLopez.Specification.MsSql;

/// <summary>
/// Renders <see cref="QueryModel"/> instances as SQL Server (T-SQL)-compatible SQL.
/// </summary>
/// <remarks>
/// <para>Key SQL Server-specific behaviors:</para>
/// <list type="bullet">
/// <item>Identifiers quoted with square brackets: <c>[table_name]</c></item>
/// <item>Parameters use named format: <c>@paramName</c></item>
/// <item>Pagination uses OFFSET ... ROWS FETCH NEXT ... ROWS ONLY (SQL Server 2012+)</item>
/// <item>TOP N used when only Take is specified without Skip</item>
/// <item>IN predicate expands collections into individual named parameters</item>
/// </list>
/// </remarks>
public sealed class MsSqlDialect : SqlDialectBase
{
    /// <summary>Gets the shared singleton instance with default configuration.</summary>
    public static readonly MsSqlDialect Default = new();

    /// <inheritdoc/>
    public override string DialectName => "SQL Server";

    /// <inheritdoc/>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is <see langword="null"/> or whitespace</exception>
    public override string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        // Stryker disable once String : Replacing empty string is invalid in identifier quoting
        return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/>, <paramref name="model"/>, or <paramref name="parameters"/> is <see langword="null"/></exception>
    protected override void RenderSelectClause(StringBuilder sql, QueryModel model, Dictionary<string, object?> parameters)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(model);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(parameters);

        if (model.QueryType == SqlQueryType.Count)
        {
            sql.Append("SELECT COUNT(*)");
        }
        else if (model.QueryType == SqlQueryType.Exists)
        {
            sql.Append("SELECT 1");
        }
        else
        {
            sql.Append("SELECT ");
            if (model.IsDistinct)
                sql.Append("DISTINCT ");

            // TOP N: only when Take is specified WITHOUT Skip (with Skip we use OFFSET/FETCH)
            if (model.Take.HasValue && !model.Skip.HasValue)
                sql.Append("TOP (@_take) ");

            if (model.Projections.IsEmpty)
                sql.Append('*');
            else
            {
                for (var i = 0; i < model.Projections.Length; i++)
                {
                    if (i > 0) sql.Append(", ");
                    sql.Append(QuoteIdentifier(model.Projections[i]));
                }
            }
        }
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
                sql.Append(" ORDER BY (SELECT NULL)");

            sql.Append(" OFFSET @_skip ROWS");
            if (model.Take.HasValue)
                sql.Append(" FETCH NEXT @_take ROWS ONLY");
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

        sql.Append("CONTAINS(").Append(QuoteIdentifier(fullText.ColumnName)).Append(", @").Append(fullText.ParameterName).Append(')');
    }
}
