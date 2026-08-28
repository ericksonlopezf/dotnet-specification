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
/// <item>Parameters use the named format: <c>@paramName</c></item>
/// <item>LIMIT and OFFSET for pagination</item>
/// <item>ILIKE for case-insensitive string matching (opt-in)</item>
/// </list>
/// </remarks>
public sealed class PostgreSqlDialect : SqlDialectBase
{
    /// <summary>Gets the shared singleton instance with default configuration.</summary>
    public static readonly PostgreSqlDialect Default = new();

    /// <inheritdoc/>
    public override string DialectName => "PostgreSQL";

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
    public override string QuoteIdentifier(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        // Stryker disable once String : Quoting empty identifier is rejected by ThrowIfNullOrWhiteSpace
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="model"/> or <paramref name="parameters"/> is <see langword="null"/></exception>
    protected override Dictionary<string, IReadOnlyList<string>> BuildInExpansions(
        QueryModel model, Dictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(parameters);

        // PostgreSQL uses native array binding (= ANY(@param)), so collections are not expanded.
        return new Dictionary<string, IReadOnlyList<string>>(0);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/>, <paramref name="inNode"/>, or <paramref name="inExpansions"/> is <see langword="null"/></exception>
    protected override void RenderInPredicate(StringBuilder sql, InPredicateNode inNode, Dictionary<string, IReadOnlyList<string>> inExpansions)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(inNode);
        ArgumentNullException.ThrowIfNull(inExpansions);

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
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="fullText"/> is <see langword="null"/></exception>
    protected override void RenderFullTextPredicate(StringBuilder sql, FullTextPredicateNode fullText)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(fullText);

        sql.Append("to_tsvector('")
           .Append(fullText.Language)
           .Append("', ")
           .Append(QuoteIdentifier(fullText.ColumnName))
           .Append(") @@ plainto_tsquery('")
           .Append(fullText.Language)
           .Append("', @")
           .Append(fullText.ParameterName)
           .Append(')');
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="sql"/> or <paramref name="range"/> is <see langword="null"/></exception>
    protected override void RenderRangePredicate(StringBuilder sql, RangePredicateNode range)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(range);

        sql.Append(QuoteIdentifier(range.ColumnName))
           .Append(" <@ int4range(@")
           .Append(range.LowerParameterName)
           .Append(", @")
           .Append(range.UpperParameterName)
           .Append(", '[]')");
    }

    /// <inheritdoc/>
    protected override string RenderOperator(SqlBinaryOperator op, string paramName) => op switch
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
