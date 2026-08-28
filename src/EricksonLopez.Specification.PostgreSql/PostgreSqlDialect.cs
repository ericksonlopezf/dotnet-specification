// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text;
using EricksonLopez.Specification.Sql;

namespace EricksonLopez.Specification.PostgreSql;

/// <summary>
/// Renders <see cref="QueryModel"/> instances as PostgreSQL-compatible SQL.
/// </summary>
/// <remarks>
/// <para>
/// Key PostgreSQL-specific behaviors:
/// </para>
/// <list type="bullet">
/// <item>Identifiers are quoted with double-quotes: <c>"table_name"</c></item>
/// <item>Parameters use the named format: <c>@paramName</c> (Npgsql named parameters)</item>
/// <item>LIMIT and OFFSET for pagination</item>
/// <item>ILIKE for case-insensitive string matching (opt-in)</item>
/// </list>
/// </remarks>
public sealed class PostgreSqlDialect : ISqlDialect
{
    /// <summary>Gets the shared singleton instance with default configuration.</summary>
    public static readonly PostgreSqlDialect Default = new();

    /// <inheritdoc/>
    public string DialectName => "PostgreSQL";

    /// <inheritdoc/>
    public string ParameterPrefix => "@";

    private readonly bool _useCaseInsensitiveLike;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlDialect"/> class.
    /// </summary>
    /// <param name="useCaseInsensitiveLike">
    /// When <see langword="true"/>, LIKE is rendered as ILIKE for case-insensitive matching.
    /// Default is <see langword="false"/>.
    /// </param>
    public PostgreSqlDialect(bool useCaseInsensitiveLike = false)
    {
        _useCaseInsensitiveLike = useCaseInsensitiveLike;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is <see langword="null"/> or whitespace</exception>
    public string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        // Stryker disable once String : Quoting empty identifier is rejected by ThrowIfNullOrWhiteSpace
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/></exception>
    public SqlQuery Render(QueryModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var sql = new StringBuilder(256);
        var parameters = new Dictionary<string, object?>(model.Parameters.Length);

        // Populate parameters dictionary
        foreach (var p in model.Parameters)
            parameters[p.Name] = p.Value;

        // SELECT clause
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

        // FROM clause
        sql.Append(" FROM ").Append(QuoteIdentifier(model.TableName));
        if (model.TableAlias is not null)
            sql.Append(" AS ").Append(QuoteIdentifier(model.TableAlias));

        // WHERE clause
        if (!model.Filters.IsEmpty)
        {
            sql.Append(" WHERE ");
            if (model.Filters.Length == 1)
                RenderPredicate(sql, model.Filters[0]);
            else
            {
                // Multiple independent criteria (all AND-combined at the spec level already)
                RenderPredicate(sql, model.Filters[0]);
                for (var i = 1; i < model.Filters.Length; i++)
                {
                    sql.Append(" AND ");
                    RenderPredicate(sql, model.Filters[i]);
                }
            }
        }

        // ORDER BY clause
        if (!model.Orders.IsEmpty)
        {
            sql.Append(" ORDER BY ");
            for (var i = 0; i < model.Orders.Length; i++)
            {
                if (i > 0) sql.Append(", ");
                sql.Append(QuoteIdentifier(model.Orders[i].ColumnName));
                sql.Append(model.Orders[i].Direction == OrderDirection.Ascending ? " ASC" : " DESC");
            }
        }

        // LIMIT / OFFSET
        if (model.Take.HasValue)
        {
            sql.Append(" LIMIT @_take");
            parameters["_take"] = model.Take.Value;
        }

        if (model.Skip.HasValue)
        {
            sql.Append(" OFFSET @_skip");
            parameters["_skip"] = model.Skip.Value;
        }

        return new SqlQuery
        {
            Sql = sql.ToString(),
            Parameters = parameters
        };
    }

    private void RenderPredicate(StringBuilder sql, SqlPredicateNode node)
    {
        switch (node)
        {
            case BinaryPredicateNode b:
                sql.Append(QuoteIdentifier(b.ColumnName));
                sql.Append(RenderOperator(b.Operator, b.ParameterName));
                break;

            case InPredicateNode inNode:
                // PostgreSQL: column = ANY(@param) for arrays, or NOT (column = ANY(@param)) for negation.
                // Npgsql handles IEnumerable<T> parameters bound to ANY(@p) natively.
                if (inNode.Negated)
                {
                    sql.Append("NOT (")
                       .Append(QuoteIdentifier(inNode.ColumnName))
                       .Append(" = ANY(@")
                       .Append(inNode.ParameterName)
                       .Append("))");
                }
                else
                {
                    sql.Append(QuoteIdentifier(inNode.ColumnName))
                       .Append(" = ANY(@")
                       .Append(inNode.ParameterName)
                       .Append(')');
                }
                break;

            case AndPredicateNode and:
                sql.Append('(');
                RenderPredicate(sql, and.Left);
                sql.Append(" AND ");
                RenderPredicate(sql, and.Right);
                sql.Append(')');
                break;

            case OrPredicateNode or:
                sql.Append('(');
                RenderPredicate(sql, or.Left);
                sql.Append(" OR ");
                RenderPredicate(sql, or.Right);
                sql.Append(')');
                break;

            case NotPredicateNode not:
                sql.Append("NOT (");
                RenderPredicate(sql, not.Inner);
                sql.Append(')');
                break;

            case BetweenPredicateNode between:
                sql.Append(QuoteIdentifier(between.ColumnName));
                sql.Append(between.Negated ? " NOT BETWEEN @" : " BETWEEN @");
                sql.Append(between.LowerParameterName);
                sql.Append(" AND @");
                sql.Append(between.UpperParameterName);
                break;

            case FullTextPredicateNode ft:
                sql.Append("to_tsvector('")
                   .Append(ft.Language)
                   .Append("', ")
                   .Append(QuoteIdentifier(ft.ColumnName))
                   .Append(") @@ plainto_tsquery('")
                   .Append(ft.Language)
                   .Append("', @")
                   .Append(ft.ParameterName)
                   .Append(')');
                break;

            case RangePredicateNode range:
                sql.Append(QuoteIdentifier(range.ColumnName))
                   .Append(" <@ int4range(@")
                   .Append(range.LowerParameterName)
                   .Append(", @")
                   .Append(range.UpperParameterName)
                   .Append(", '[]')");
                break;

            case RawPredicateNode raw:
                sql.Append(raw.Sql);
                break;

            default:
                // Stryker disable once Statement : defensive unreachable code
                throw new NotSupportedException($"Unknown predicate node type: {node.GetType().Name}");
        }
    }

    private string RenderOperator(SqlBinaryOperator op, string paramName) => op switch
    {
        SqlBinaryOperator.Equal => $" = @{paramName}",
        SqlBinaryOperator.NotEqual => $" <> @{paramName}",
        SqlBinaryOperator.GreaterThan => $" > @{paramName}",
        SqlBinaryOperator.GreaterThanOrEqual => $" >= @{paramName}",
        SqlBinaryOperator.LessThan => $" < @{paramName}",
        SqlBinaryOperator.LessThanOrEqual => $" <= @{paramName}",
        SqlBinaryOperator.Like => _useCaseInsensitiveLike
            ? $" ILIKE @{paramName}"
            : $" LIKE @{paramName}",
        SqlBinaryOperator.LikeStartsWith => _useCaseInsensitiveLike
            ? $" ILIKE @{paramName}"
            : $" LIKE @{paramName}",
        SqlBinaryOperator.LikeEndsWith => _useCaseInsensitiveLike
            ? $" ILIKE @{paramName}"
            : $" LIKE @{paramName}",
        SqlBinaryOperator.NotLike => _useCaseInsensitiveLike
            ? $" NOT ILIKE @{paramName}"
            : $" NOT LIKE @{paramName}",
        SqlBinaryOperator.IsNull => " IS NULL",
        SqlBinaryOperator.IsNotNull => " IS NOT NULL",
        _ => throw new NotSupportedException($"Operator '{op}' is not supported by PostgreSQL dialect.")
    };
}


