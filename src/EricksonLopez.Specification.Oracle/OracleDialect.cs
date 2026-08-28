// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
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
public sealed class OracleDialect : ISqlDialect
{
    /// <summary>Gets the shared singleton instance with default configuration.</summary>
    public static readonly OracleDialect Default = new();

    /// <inheritdoc/>
    public string DialectName => "Oracle";

    /// <inheritdoc/>
    public string ParameterPrefix => ":";

    /// <inheritdoc/>
    /// <exception cref="ArgumentException"><paramref name="identifier"/> is <see langword="null"/> or whitespace</exception>
    public string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        // Stryker disable once String : Replacing empty string is invalid in identifier quoting
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/></exception>
    public SqlQuery Render(QueryModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var sql = new StringBuilder(256);
        var parameters = new Dictionary<string, object?>(model.Parameters.Length);

        // Expand collection parameters for IN predicates before building SQL.
        var inExpansions = BuildInExpansions(model, parameters);

        // Populate scalar parameters
        foreach (var p in model.Parameters)
        {
            if (!inExpansions.ContainsKey(p.Name))
                parameters[p.Name] = p.Value;
        }

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

        // FROM clause (Oracle does not allow AS in table aliases)
        sql.Append(" FROM ").Append(QuoteIdentifier(model.TableName));
        if (model.TableAlias is not null)
            sql.Append(' ').Append(QuoteIdentifier(model.TableAlias));

        // WHERE clause
        if (!model.Filters.IsEmpty)
        {
            sql.Append(" WHERE ");
            RenderPredicate(sql, model.Filters[0], inExpansions);
            for (var i = 1; i < model.Filters.Length; i++)
            {
                sql.Append(" AND ");
                RenderPredicate(sql, model.Filters[i], inExpansions);
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

        // OFFSET ... FETCH NEXT ... (Oracle 12c+ standard)
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

        return new SqlQuery { Sql = sql.ToString(), Parameters = parameters };
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildInExpansions(
        QueryModel model, Dictionary<string, object?> parameters)
    {
        var expansions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var p in model.Parameters)
        {
            if (p.Value is System.Collections.IEnumerable enumerable and not string)
            {
                var names = new List<string>();
                var idx = 0;
                foreach (var item in enumerable)
                {
                    var expandedName = $"{p.Name}_{idx++}";
                    names.Add(expandedName);
                    parameters[expandedName] = item;
                }
                expansions[p.Name] = names;
            }
        }
        return expansions;
    }

    private void RenderPredicate(StringBuilder sql, SqlPredicateNode node,
        Dictionary<string, IReadOnlyList<string>> inExpansions)
    {
        switch (node)
        {
            case BinaryPredicateNode b:
                sql.Append(QuoteIdentifier(b.ColumnName));
                sql.Append(RenderOperator(b.Operator, b.ParameterName));
                break;

            case InPredicateNode inNode:
                if (inExpansions.TryGetValue(inNode.ParameterName, out var names) && names.Count > 0)
                {
                    sql.Append(QuoteIdentifier(inNode.ColumnName));
                    sql.Append(inNode.Negated ? " NOT IN (" : " IN (");
                    for (var i = 0; i < names.Count; i++)
                    {
                        if (i > 0) sql.Append(", ");
                        sql.Append(':').Append(names[i]);
                    }
                    sql.Append(')');
                }
                else
                {
                    // Empty collection: IN () is invalid SQL. Use always-false/always-true.
                    sql.Append(inNode.Negated ? "1 = 1" : "1 = 0");
                }
                break;

            case AndPredicateNode and:
                sql.Append('(');
                RenderPredicate(sql, and.Left, inExpansions);
                sql.Append(" AND ");
                RenderPredicate(sql, and.Right, inExpansions);
                sql.Append(')');
                break;

            case OrPredicateNode or:
                sql.Append('(');
                RenderPredicate(sql, or.Left, inExpansions);
                sql.Append(" OR ");
                RenderPredicate(sql, or.Right, inExpansions);
                sql.Append(')');
                break;

            case NotPredicateNode not:
                sql.Append("NOT (");
                RenderPredicate(sql, not.Inner, inExpansions);
                sql.Append(')');
                break;

            case BetweenPredicateNode between:
                sql.Append(QuoteIdentifier(between.ColumnName));
                sql.Append(between.Negated ? " NOT BETWEEN :" : " BETWEEN :");
                sql.Append(between.LowerParameterName);
                sql.Append(" AND :");
                sql.Append(between.UpperParameterName);
                break;

            case FullTextPredicateNode ft:
                sql.Append("CONTAINS(").Append(QuoteIdentifier(ft.ColumnName)).Append(", :").Append(ft.ParameterName).Append(") > 0");
                break;

            case RangePredicateNode range:
                sql.Append('(');
                sql.Append(QuoteIdentifier(range.ColumnName)).Append(" >= :").Append(range.LowerParameterName);
                sql.Append(" AND ");
                sql.Append(QuoteIdentifier(range.ColumnName)).Append(" <= :").Append(range.UpperParameterName);
                sql.Append(')');
                break;

            case RawPredicateNode raw:
                sql.Append(raw.Sql);
                break;

            default:
                throw new NotSupportedException($"Unknown predicate node type: {node.GetType().Name}");
        }
    }

    private static string RenderOperator(SqlBinaryOperator op, string paramName) => op switch
    {
        SqlBinaryOperator.Equal => $" = :{paramName}",
        SqlBinaryOperator.NotEqual => $" <> :{paramName}",
        SqlBinaryOperator.GreaterThan => $" > :{paramName}",
        SqlBinaryOperator.GreaterThanOrEqual => $" >= :{paramName}",
        SqlBinaryOperator.LessThan => $" < :{paramName}",
        SqlBinaryOperator.LessThanOrEqual => $" <= :{paramName}",
        SqlBinaryOperator.Like => $" LIKE :{paramName}",
        SqlBinaryOperator.LikeStartsWith => $" LIKE :{paramName}",
        SqlBinaryOperator.LikeEndsWith => $" LIKE :{paramName}",
        SqlBinaryOperator.NotLike => $" NOT LIKE :{paramName}",
        SqlBinaryOperator.IsNull => " IS NULL",
        SqlBinaryOperator.IsNotNull => " IS NOT NULL",
        _ => throw new NotSupportedException($"Operator '{op}' is not supported by Oracle dialect.")
    };
}
