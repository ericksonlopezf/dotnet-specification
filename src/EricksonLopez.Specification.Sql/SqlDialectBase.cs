// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text;

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Provides a base implementation of <see cref="ISqlDialect"/> containing standard AST traversal,
/// parameter management, predicate rendering, and extensible SQL clause composition.
/// </summary>
public abstract class SqlDialectBase : ISqlDialect
{
    /// <inheritdoc/>
    public abstract string DialectName { get; }

    /// <inheritdoc/>
    public virtual string ParameterPrefix => "@";

    /// <inheritdoc/>
    public abstract string QuoteIdentifier(string identifier);

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> is <see langword="null"/></exception>
    public virtual SqlQuery Render(QueryModel model)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(model);

        var sql = new StringBuilder(256);
        var parameters = new Dictionary<string, object?>(model.Parameters.Length + 2);

        // Expand collection parameters for IN predicates before building SQL
        var inExpansions = BuildInExpansions(model, parameters);

        // Non-collection parameters pass through directly
        foreach (var p in model.Parameters)
        {
            if (!inExpansions.ContainsKey(p.Name))
                parameters[p.Name] = p.Value;
        }

        // SELECT clause
        RenderSelectClause(sql, model, parameters);

        // FROM clause
        RenderFromClause(sql, model);

        // WHERE clause
        RenderWhereClause(sql, model, inExpansions);

        // ORDER BY clause
        RenderOrderByClause(sql, model);

        // LIMIT / OFFSET / FETCH pagination
        RenderPagination(sql, model, parameters);

        return new SqlQuery
        {
            Sql = sql.ToString(),
            Parameters = parameters
        };
    }

    /// <summary>
    /// Builds expanded parameter mapping for collection-based parameters used in IN clauses.
    /// </summary>
    /// <param name="model">The query model.</param>
    /// <param name="parameters">The target dictionary for expanded parameter values.</param>
    /// <returns>A map of original parameter names to expanded parameter names.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> or <paramref name="parameters"/> is <see langword="null"/></exception>
    protected virtual Dictionary<string, IReadOnlyList<string>> BuildInExpansions(
        QueryModel model, Dictionary<string, object?> parameters)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(model);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(parameters);

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

    /// <summary>
    /// Renders the SELECT projection clause.
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="model">The query model.</param>
    /// <param name="parameters">The parameter dictionary.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/>, <paramref name="model"/>, or <paramref name="parameters"/> is <see langword="null"/></exception>
    protected virtual void RenderSelectClause(StringBuilder sql, QueryModel model, Dictionary<string, object?> parameters)
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

    /// <summary>
    /// Renders the FROM table and optional alias clause.
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="model">The query model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="model"/> is <see langword="null"/></exception>
    protected virtual void RenderFromClause(StringBuilder sql, QueryModel model)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(model);

        sql.Append(" FROM ").Append(QuoteIdentifier(model.TableName));
        if (model.TableAlias is not null)
            sql.Append(" AS ").Append(QuoteIdentifier(model.TableAlias));
    }

    /// <summary>
    /// Renders the WHERE filter predicates clause.
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="model">The query model.</param>
    /// <param name="inExpansions">The expanded IN parameter map.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/>, <paramref name="model"/>, or <paramref name="inExpansions"/> is <see langword="null"/></exception>
    protected virtual void RenderWhereClause(StringBuilder sql, QueryModel model, Dictionary<string, IReadOnlyList<string>> inExpansions)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(model);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(inExpansions);

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
    }

    /// <summary>
    /// Renders the ORDER BY sort columns clause.
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="model">The query model.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="model"/> is <see langword="null"/></exception>
    protected virtual void RenderOrderByClause(StringBuilder sql, QueryModel model)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(model);

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
    }

    /// <summary>
    /// Renders the dialect-specific pagination clause (e.g., LIMIT / OFFSET / FETCH).
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="model">The query model.</param>
    /// <param name="parameters">The parameter dictionary.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/>, <paramref name="model"/>, or <paramref name="parameters"/> is <see langword="null"/></exception>
    protected virtual void RenderPagination(StringBuilder sql, QueryModel model, Dictionary<string, object?> parameters)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(model);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(parameters);

        if (model.Take.HasValue)
        {
            sql.Append(" LIMIT ").Append(ParameterPrefix).Append("_take");
            parameters["_take"] = model.Take.Value;
        }

        if (model.Skip.HasValue)
        {
            sql.Append(" OFFSET ").Append(ParameterPrefix).Append("_skip");
            parameters["_skip"] = model.Skip.Value;
        }
    }

    /// <summary>
    /// Renders a predicate AST node into SQL.
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="node">The predicate node to render.</param>
    /// <param name="inExpansions">The expanded IN parameter map.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/>, <paramref name="node"/>, or <paramref name="inExpansions"/> is <see langword="null"/></exception>
    protected virtual void RenderPredicate(StringBuilder sql, SqlPredicateNode node, Dictionary<string, IReadOnlyList<string>> inExpansions)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(node);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(inExpansions);

        switch (node)
        {
            case BinaryPredicateNode b:
                RenderBinaryPredicate(sql, b);
                break;

            case InPredicateNode inNode:
                RenderInPredicate(sql, inNode, inExpansions);
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
                RenderBetweenPredicate(sql, between);
                break;

            case FullTextPredicateNode ft:
                RenderFullTextPredicate(sql, ft);
                break;

            case RangePredicateNode range:
                RenderRangePredicate(sql, range);
                break;

            case RawPredicateNode raw:
                sql.Append(raw.Sql);
                break;

            default:
                throw new NotSupportedException($"Unknown predicate node type: {node.GetType().Name}");
        }
    }

    /// <summary>
    /// Renders a binary predicate node (e.g. col = @val).
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="binary">The binary predicate node.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="binary"/> is <see langword="null"/></exception>
    protected virtual void RenderBinaryPredicate(StringBuilder sql, BinaryPredicateNode binary)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(binary);

        sql.Append(QuoteIdentifier(binary.ColumnName));
        sql.Append(RenderOperator(binary.Operator, binary.ParameterName));
    }

    /// <summary>
    /// Renders an IN / NOT IN predicate node.
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="inNode">The IN predicate node.</param>
    /// <param name="inExpansions">The expanded IN parameter map.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/>, <paramref name="inNode"/>, or <paramref name="inExpansions"/> is <see langword="null"/></exception>
    protected virtual void RenderInPredicate(StringBuilder sql, InPredicateNode inNode, Dictionary<string, IReadOnlyList<string>> inExpansions)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(inNode);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(inExpansions);

        if (inExpansions.TryGetValue(inNode.ParameterName, out var names) && names.Count > 0)
        {
            sql.Append(QuoteIdentifier(inNode.ColumnName));
            sql.Append(inNode.Negated ? " NOT IN (" : " IN (");
            for (var i = 0; i < names.Count; i++)
            {
                if (i > 0) sql.Append(", ");
                sql.Append(ParameterPrefix).Append(names[i]);
            }
            sql.Append(')');
        }
        else
        {
            // Empty collection: IN () is invalid SQL. Use always-false/always-true.
            sql.Append(inNode.Negated ? "1 = 1" : "1 = 0");
        }
    }

    /// <summary>
    /// Renders a BETWEEN predicate node.
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="between">The BETWEEN predicate node.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="between"/> is <see langword="null"/></exception>
    protected virtual void RenderBetweenPredicate(StringBuilder sql, BetweenPredicateNode between)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(between);

        sql.Append(QuoteIdentifier(between.ColumnName));
        sql.Append(between.Negated ? " NOT BETWEEN " : " BETWEEN ");
        sql.Append(ParameterPrefix).Append(between.LowerParameterName);
        sql.Append(" AND ");
        sql.Append(ParameterPrefix).Append(between.UpperParameterName);
    }

    /// <summary>
    /// Renders a full-text search predicate node.
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="fullText">The full-text predicate node.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="fullText"/> is <see langword="null"/></exception>
    protected virtual void RenderFullTextPredicate(StringBuilder sql, FullTextPredicateNode fullText)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(fullText);

        sql.Append(QuoteIdentifier(fullText.ColumnName))
           .Append(" LIKE '%' || ")
           .Append(ParameterPrefix)
           .Append(fullText.ParameterName)
           .Append(" || '%'");
    }

    /// <summary>
    /// Renders a range predicate node.
    /// </summary>
    /// <param name="sql">The target string builder.</param>
    /// <param name="range">The range predicate node.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="range"/> is <see langword="null"/></exception>
    protected virtual void RenderRangePredicate(StringBuilder sql, RangePredicateNode range)
    {
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(sql);
        // Stryker disable once Statement : Defensive null guard
        ArgumentNullException.ThrowIfNull(range);

        sql.Append('(');
        sql.Append(QuoteIdentifier(range.ColumnName)).Append(" >= ").Append(ParameterPrefix).Append(range.LowerParameterName);
        sql.Append(" AND ");
        sql.Append(QuoteIdentifier(range.ColumnName)).Append(" <= ").Append(ParameterPrefix).Append(range.UpperParameterName);
        sql.Append(')');
    }

    /// <summary>
    /// Renders a binary operator with its bound parameter name.
    /// </summary>
    /// <param name="op">The SQL binary operator.</param>
    /// <param name="paramName">The parameter name.</param>
    /// <returns>The operator SQL representation.</returns>
    protected virtual string RenderOperator(SqlBinaryOperator op, string paramName) => op switch
    {
        SqlBinaryOperator.Equal => $" = {ParameterPrefix}{paramName}",
        SqlBinaryOperator.NotEqual => $" <> {ParameterPrefix}{paramName}",
        SqlBinaryOperator.GreaterThan => $" > {ParameterPrefix}{paramName}",
        SqlBinaryOperator.GreaterThanOrEqual => $" >= {ParameterPrefix}{paramName}",
        SqlBinaryOperator.LessThan => $" < {ParameterPrefix}{paramName}",
        SqlBinaryOperator.LessThanOrEqual => $" <= {ParameterPrefix}{paramName}",
        SqlBinaryOperator.Like => $" LIKE {ParameterPrefix}{paramName}",
        SqlBinaryOperator.LikeStartsWith => $" LIKE {ParameterPrefix}{paramName}",
        SqlBinaryOperator.LikeEndsWith => $" LIKE {ParameterPrefix}{paramName}",
        SqlBinaryOperator.NotLike => $" NOT LIKE {ParameterPrefix}{paramName}",
        SqlBinaryOperator.IsNull => " IS NULL",
        SqlBinaryOperator.IsNotNull => " IS NOT NULL",
        _ => throw new NotSupportedException($"Operator '{op}' is not supported by {DialectName} dialect.")
    };
}
