// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using EricksonLopez.Specification;

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Translates a <see cref="QuerySpec{T}"/> into a provider-agnostic <see cref="QueryModel"/>.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <remarks>
/// <para>
/// The translator walks the expression trees in the <see cref="QuerySpec{T}"/> and maps
/// them to provider-agnostic <see cref="SqlPredicateNode"/> instances.
/// </para>
/// <para>
/// The mapping from C# property names to SQL column names uses snake_case by default.
/// Provide a custom <see cref="IColumnNameResolver"/> to override naming conventions.
/// </para>
/// </remarks>
public sealed class QuerySpecTranslator<T>
{
    private readonly string _tableName;
    private readonly IColumnNameResolver _columnNameResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuerySpecTranslator{T}"/> class.
    /// </summary>
    /// <param name="tableName">The SQL table name for type <typeparamref name="T"/>.</param>
    /// <param name="columnNameResolver">The resolver for mapping property names to column names.</param>
    /// <exception cref="ArgumentException"><paramref name="tableName"/> is <see langword="null"/> or whitespace</exception>
    public QuerySpecTranslator(string tableName, IColumnNameResolver? columnNameResolver = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        _tableName = tableName;
        _columnNameResolver = columnNameResolver ?? SnakeCaseColumnNameResolver.Default;
    }

    /// <summary>
    /// Translates the specified <see cref="QuerySpec{T}"/> to a <see cref="QueryModel"/>.
    /// </summary>
    /// <param name="spec">The specification to translate.</param>
    /// <returns>A <see cref="QueryModel"/> representing the query.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="spec"/> is <see langword="null"/></exception>
    [RequiresUnreferencedCode(
        "SQL translation extracts constant values from expression closures using reflection. " +
        "In trimmed/NativeAOT builds, ensure the entity type T and any closure types retain " +
        "their fields and properties. See the EricksonLopez.Specification.Sql documentation for trimming guidance.")]
    public QueryModel Translate(QuerySpec<T> spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        using var activity = Diagnostics.SpecificationDiagnostics.ActivitySource.StartActivity("QuerySpecTranslator.Translate");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Cache lookup: compose all criteria into a structurally-unique key before caching.
        // - 0 criteria: no key (nothing to cache)
        // - 1 criterion: use the expression directly
        // - N criteria: compose with AndAll to produce a single canonical key
        //
        // IMPORTANT: The cache stores and returns the QueryModel built from spec.Criteria
        // (the original array), NOT from the composed expression. So cached multi-criteria
        // models correctly have N separate filter nodes. The composed key is used ONLY as
        // a cache lookup key, never as the expression that is translated.
        var cacheKey = spec.Criteria.Length switch
        {
            0 => null,
            1 => spec.Criteria[0],
            _ => ExpressionComposer.AndAll<T>(spec.Criteria.AsSpan())
        };

        // Cache hit: pure-filter specs (no ordering, no pagination, no distinct, no cursor)
        if (cacheKey is not null && QueryPlanCache.TryGetPlan(cacheKey, _tableName, out var cachedPlan))
        {
            if (spec.SkipCount == null && spec.TakeCount == null && spec.OrderClauses.IsEmpty && !spec.IsDistinct && spec.Cursor == null)
            {
                Diagnostics.SpecificationDiagnostics.SqlTranslations.Add(1);
                Diagnostics.SpecificationDiagnostics.SqlTranslationDuration.Record(sw.Elapsed.TotalMilliseconds);
                return cachedPlan;
            }
        }

        var parameters = new List<SqlParameter>();
        var paramCounter = 0;

        // Translate filters from the ORIGINAL criteria array — not the composed cacheKey.
        // This ensures cached models have the correct number of filter nodes (N, not 1).
        var filters = TranslateFilters(spec.Criteria, parameters, ref paramCounter);

        // Translate cursor clause if present
        if (spec.Cursor is not null)
        {
            var cursorColumn = ExtractColumnNameFromOrderSelector(spec.Cursor.KeySelector)
                ?? throw new NotSupportedException("Cannot determine column name from cursor key selector.");
            var cursorParamName = $"p{++paramCounter}";
            parameters.Add(new SqlParameter(cursorParamName, spec.Cursor.Value));
            var cursorOp = spec.Cursor.Direction == CursorDirection.After
                ? SqlBinaryOperator.GreaterThan
                : SqlBinaryOperator.LessThan;
            var cursorNode = new BinaryPredicateNode(cursorColumn, cursorOp, cursorParamName);
            filters = filters.Add(cursorNode);
        }

        // Translate ordering
        var orders = TranslateOrdering(spec.OrderClauses);

        var plan = new QueryModel
        {
            TableName = _tableName,
            Filters = filters,
            Orders = orders,
            Skip = spec.SkipCount,
            Take = spec.TakeCount,
            IsDistinct = spec.IsDistinct,
            Parameters = [.. parameters]
        };

        // Cache pure-filter specs (single and multi-criteria)
        if (cacheKey is not null && spec.SkipCount == null && spec.TakeCount == null && spec.OrderClauses.IsEmpty && !spec.IsDistinct && spec.Cursor == null)
        {
            QueryPlanCache.SetPlan(cacheKey, _tableName, plan);
        }

        Diagnostics.SpecificationDiagnostics.SqlTranslations.Add(1);
        Diagnostics.SpecificationDiagnostics.SqlTranslationDuration.Record(sw.Elapsed.TotalMilliseconds);

        return plan;
    }

    private ImmutableArray<SqlPredicateNode> TranslateFilters(
        ImmutableArray<Expression<Func<T, bool>>> criteria,
        List<SqlParameter> parameters,
        ref int paramCounter)
    {
        if (criteria.IsEmpty)
            return [];

        var nodes = new SqlPredicateNode[criteria.Length];
        for (var i = 0; i < criteria.Length; i++)
            nodes[i] = TranslateExpression(criteria[i].Body, parameters, ref paramCounter);

        return [.. nodes];
    }

    private SqlPredicateNode TranslateExpression(
        Expression node,
        List<SqlParameter> parameters,
        ref int paramCounter)
    {
        return node switch
        {
            BinaryExpression b when b.NodeType == ExpressionType.AndAlso
                => new AndPredicateNode(
                    TranslateExpression(b.Left, parameters, ref paramCounter),
                    TranslateExpression(b.Right, parameters, ref paramCounter)),

            BinaryExpression b when b.NodeType == ExpressionType.OrElse
                => new OrPredicateNode(
                    TranslateExpression(b.Left, parameters, ref paramCounter),
                    TranslateExpression(b.Right, parameters, ref paramCounter)),

            // Handle negated boolean member BEFORE general Not: c => !c.IsDeleted → column = false
            UnaryExpression u when u.NodeType == ExpressionType.Not && u.Operand is MemberExpression bm && bm.Type == typeof(bool)
                => TranslateBoolMember((MemberExpression)u.Operand, isNegated: true, parameters, ref paramCounter),

            UnaryExpression u when u.NodeType == ExpressionType.Not
                => new NotPredicateNode(TranslateExpression(u.Operand, parameters, ref paramCounter)),

            BinaryExpression b when IsComparisonOperator(b.NodeType)
                => TranslateBinaryComparison(b, parameters, ref paramCounter),

            MethodCallExpression mc when IsStringMethod(mc)
                => TranslateStringMethod(mc, parameters, ref paramCounter),

            // Handle Enumerable.Contains(collection, item) and ICollection.Contains(item): c => ids.Contains(c.Id)
            MethodCallExpression mc when IsEnumerableContains(mc)
                => TranslateContains(mc, parameters, ref paramCounter),

            MethodCallExpression mc when mc.Method.Name == "Between"
                => TranslateBetween(mc, parameters, ref paramCounter),

            MethodCallExpression mc when mc.Method.Name == "MatchesFullText"
                => TranslateFullText(mc, parameters, ref paramCounter),

            MethodCallExpression mc when mc.Method.Name is "InRange" or "ContainedByRange"
                => TranslateRange(mc, parameters, ref paramCounter),

            // Handle boolean member access: c => c.IsActive → column = true
            MemberExpression m when m.Type == typeof(bool)
                => TranslateBoolMember(m, isNegated: false, parameters, ref paramCounter),

            _ => throw new NotSupportedException(
                $"Expression node '{node.NodeType}' ({node.GetType().Name}) is not supported in SQL translation. " +
                $"Expression: {node}")
        };
    }

    private BinaryPredicateNode TranslateBinaryComparison(
        BinaryExpression b,
        List<SqlParameter> parameters,
        ref int paramCounter)
    {
        var columnOnLeft = ExtractColumnName(b.Left);
        var columnOnRight = ExtractColumnName(b.Right);

        var columnName = columnOnLeft ?? columnOnRight
            ?? throw new NotSupportedException($"Cannot determine column name from expression: {b}");

        // Stryker disable once NullCoalescing : Exactly one side is constant and the other is column (which returns null)
        var value = ExtractConstantValue(b.Right) ?? ExtractConstantValue(b.Left);
        var op = MapOperator(b.NodeType);

        // If the column is on the right (e.g. 100 <= c.CreditLimit), we must flip the operator
        // Stryker disable once Logical : defensive
        if (columnOnLeft is null && columnOnRight is not null)
        {
            op = op switch
            {
                SqlBinaryOperator.GreaterThan => SqlBinaryOperator.LessThan,
                SqlBinaryOperator.GreaterThanOrEqual => SqlBinaryOperator.LessThanOrEqual,
                SqlBinaryOperator.LessThan => SqlBinaryOperator.GreaterThan,
                SqlBinaryOperator.LessThanOrEqual => SqlBinaryOperator.GreaterThanOrEqual,
                _ => op
            };
        }

        if (value is null)
        {
            // IS NULL / IS NOT NULL
            op = b.NodeType == ExpressionType.Equal
                ? SqlBinaryOperator.IsNull
                : SqlBinaryOperator.IsNotNull;
            return new BinaryPredicateNode(columnName, op, string.Empty);
        }

        var paramName = $"p{++paramCounter}";
        parameters.Add(new SqlParameter(paramName, value));
        return new BinaryPredicateNode(columnName, op, paramName);
    }

    private BinaryPredicateNode TranslateBoolMember(
        MemberExpression m,
        bool isNegated,
        List<SqlParameter> parameters,
        ref int paramCounter)
    {
        var columnName = _columnNameResolver.Resolve(m.Member.Name);
        var paramName = $"p{++paramCounter}";
        // c => c.IsActive → column = true
        // c => !c.IsDeleted → column = false
        parameters.Add(new SqlParameter(paramName, !isNegated));
        return new BinaryPredicateNode(columnName, SqlBinaryOperator.Equal, paramName);
    }

    private BinaryPredicateNode TranslateStringMethod(
        MethodCallExpression mc,
        List<SqlParameter> parameters,
        ref int paramCounter)
    {
        var columnName = ExtractColumnName(mc.Object!)
            ?? throw new NotSupportedException($"String method '{mc.Method.Name}' on non-member not supported.");

        var arg = mc.Arguments.Count > 0
            ? ExtractConstantValue(mc.Arguments[0])
            : null;

        var paramName = $"p{++paramCounter}";

        switch (mc.Method.Name)
        {
            case nameof(string.Contains):
                parameters.Add(new SqlParameter(paramName, $"%{arg}%"));
                return new BinaryPredicateNode(columnName, SqlBinaryOperator.Like, paramName);
            case nameof(string.StartsWith):
                parameters.Add(new SqlParameter(paramName, $"{arg}%"));
                return new BinaryPredicateNode(columnName, SqlBinaryOperator.LikeStartsWith, paramName);
            case nameof(string.EndsWith):
                parameters.Add(new SqlParameter(paramName, $"%{arg}"));
                return new BinaryPredicateNode(columnName, SqlBinaryOperator.LikeEndsWith, paramName);
            default:
                throw new NotSupportedException($"String method '{mc.Method.Name}' is not supported.");
        }
    }

    private InPredicateNode TranslateContains(
        MethodCallExpression mc,
        List<SqlParameter> parameters,
        ref int paramCounter)
    {
        MemberExpression? memberExpr;
        Expression? collectionExpr;

        if (mc.Object is not null)
        {
            // Pattern A: ids.Contains(c.Id)
            collectionExpr = mc.Object;
            memberExpr = mc.Arguments[0] as MemberExpression;
        }
        else
        {
            // Pattern B: Enumerable.Contains(ids, c.Id)
            collectionExpr = mc.Arguments[0];
            memberExpr = mc.Arguments[1] as MemberExpression;
        }

        if (memberExpr is null)
            throw new NotSupportedException(
                $"Contains() translation requires a direct member access as the item argument. Expression: {mc}");

        var columnName = ExtractColumnName(memberExpr)
            ?? throw new NotSupportedException(
                $"Cannot determine column name from Contains() item argument: {memberExpr}");

        var collection = ExtractConstantValue(collectionExpr);
        var paramName = $"p{++paramCounter}";
        parameters.Add(new SqlParameter(paramName, collection));
        return new InPredicateNode(columnName, paramName);
    }

    private static (Expression? Target, Expression? Arg1, Expression? Arg2) ExtractThreeMethodArgs(MethodCallExpression mc)
    {
        if (mc.Object is not null)
        {
            return (
                mc.Object,
                mc.Arguments.Count > 0 ? mc.Arguments[0] : null,
                mc.Arguments.Count > 1 ? mc.Arguments[1] : null
            );
        }

        return (
            mc.Arguments.Count > 0 ? mc.Arguments[0] : null,
            mc.Arguments.Count > 1 ? mc.Arguments[1] : null,
            mc.Arguments.Count > 2 ? mc.Arguments[2] : null
        );
    }

    private static (Expression? Target, Expression? Arg) ExtractTwoMethodArgs(MethodCallExpression mc)
    {
        if (mc.Object is not null)
        {
            return (
                mc.Object,
                mc.Arguments.Count > 0 ? mc.Arguments[0] : null
            );
        }

        return (
            mc.Arguments.Count > 0 ? mc.Arguments[0] : null,
            mc.Arguments.Count > 1 ? mc.Arguments[1] : null
        );
    }

    private BetweenPredicateNode TranslateBetween(
        MethodCallExpression mc,
        List<SqlParameter> parameters,
        ref int paramCounter)
    {
        var (targetExpr, lowerExpr, upperExpr) = ExtractThreeMethodArgs(mc);

        // Stryker disable once Logical : In BCL MethodCallExpression, upperExpr is not null implies target and lower are not null
        if (targetExpr is null || lowerExpr is null || upperExpr is null)
            throw new NotSupportedException($"Between() call could not be parsed: {mc}");

        var columnName = ExtractColumnName(targetExpr)
            ?? throw new NotSupportedException($"Cannot determine column name from Between() target: {targetExpr}");

        var lowerVal = ExtractConstantValue(lowerExpr);
        var upperVal = ExtractConstantValue(upperExpr);

        var lowerParam = $"p{++paramCounter}";
        var upperParam = $"p{++paramCounter}";

        parameters.Add(new SqlParameter(lowerParam, lowerVal));
        parameters.Add(new SqlParameter(upperParam, upperVal));

        return new BetweenPredicateNode(columnName, lowerParam, upperParam);
    }

    private FullTextPredicateNode TranslateFullText(
        MethodCallExpression mc,
        List<SqlParameter> parameters,
        ref int paramCounter)
    {
        var (targetExpr, textExpr) = ExtractTwoMethodArgs(mc);

        if (targetExpr is null || textExpr is null)
            throw new NotSupportedException($"MatchesFullText() call could not be parsed: {mc}");

        var columnName = ExtractColumnName(targetExpr)
            ?? throw new NotSupportedException($"Cannot determine column name from FullText target: {targetExpr}");

        var textVal = ExtractConstantValue(textExpr);
        var paramName = $"p{++paramCounter}";
        parameters.Add(new SqlParameter(paramName, textVal));

        return new FullTextPredicateNode(columnName, paramName);
    }

    private RangePredicateNode TranslateRange(
        MethodCallExpression mc,
        List<SqlParameter> parameters,
        ref int paramCounter)
    {
        var (targetExpr, lowerExpr, upperExpr) = ExtractThreeMethodArgs(mc);

        // Stryker disable once Logical : In BCL MethodCallExpression, upperExpr is not null implies target and lower are not null
        if (targetExpr is null || lowerExpr is null || upperExpr is null)
            throw new NotSupportedException($"Range call could not be parsed: {mc}");

        var columnName = ExtractColumnName(targetExpr)
            ?? throw new NotSupportedException($"Cannot determine column name from Range target: {targetExpr}");

        var lowerVal = ExtractConstantValue(lowerExpr);
        var upperVal = ExtractConstantValue(upperExpr);

        var lowerParam = $"p{++paramCounter}";
        var upperParam = $"p{++paramCounter}";

        parameters.Add(new SqlParameter(lowerParam, lowerVal));
        parameters.Add(new SqlParameter(upperParam, upperVal));

        return new RangePredicateNode(columnName, lowerParam, upperParam);
    }



    private ImmutableArray<SqlOrderNode> TranslateOrdering(ImmutableArray<OrderClause<T>> clauses)
    {
        if (clauses.IsEmpty) return [];

        var orders = new SqlOrderNode[clauses.Length];
        for (var i = 0; i < clauses.Length; i++)
        {
            var column = ExtractColumnNameFromOrderSelector(clauses[i].KeySelector)
                ?? throw new NotSupportedException($"Cannot determine column name from order selector.");
            orders[i] = new SqlOrderNode(column, clauses[i].Direction);
        }
        return [.. orders];
    }

    private string? ExtractColumnName(Expression expr)
    {
        // Handle: x.Property or Convert(x.Property, object)
        if (expr is UnaryExpression { NodeType: ExpressionType.Convert } u)
            expr = u.Operand;

        if (expr is MemberExpression member && member.Expression?.NodeType == ExpressionType.Parameter)
        {
            return _columnNameResolver.Resolve(member.Member.Name);
        }

        return null;
    }

    private string? ExtractColumnNameFromOrderSelector(Expression<Func<T, object?>> expr)
    {
        var body = expr.Body;
        if (body is UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Convert)
                body = u.Operand;
        }

        return body is MemberExpression member
            ? _columnNameResolver.Resolve(member.Member.Name)
            : null;
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification =
            "The public Translate() method is already annotated with [RequiresUnreferencedCode]. " +
            "This private helper is only reachable through that annotated entry point.")]
    private static object? ExtractConstantValue(Expression? expr)
    {
        if (expr is null)
            return null;

        if (expr is ConstantExpression c)
            return c.Value;

        if (expr is UnaryExpression u)
        {
            if (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.ConvertChecked)
                return ExtractConstantValue(u.Operand);
            return null;
        }

        // Handle member access on closure objects
        if (expr is MemberExpression m)
        {
            if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
                return null;

            var obj = ExtractConstantValue(m.Expression);

            if (m.Member is FieldInfo fi)
            {
                if (fi.IsStatic)
                    return fi.GetValue(null);
                return obj is not null ? fi.GetValue(obj) : null;
            }

            var pi = (PropertyInfo)m.Member;
            if (pi.GetMethod is null)
                return null;
            if (pi.GetMethod.IsStatic)
                return pi.GetValue(null);
            return obj is not null ? pi.GetValue(obj) : null;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsComparisonOperator(ExpressionType nodeType) => nodeType switch
    {
        ExpressionType.Equal or
        ExpressionType.NotEqual or
        ExpressionType.GreaterThan or
        ExpressionType.GreaterThanOrEqual or
        ExpressionType.LessThan or
        ExpressionType.LessThanOrEqual => true,
        _ => false
    };

    private static bool IsStringMethod(MethodCallExpression mc) =>
        mc.Object?.Type == typeof(string) &&
        mc.Method.Name is nameof(string.Contains)
            or nameof(string.StartsWith)
            or nameof(string.EndsWith);

    /// <summary>Detects Enumerable.Contains(collection, item) or collection.Contains(item) where item is a column member.</summary>
    private static bool IsEnumerableContains(MethodCallExpression mc)
    {
        // Static: Enumerable.Contains<T>(IEnumerable<T> source, T value)
        if (mc.Object is null &&
            mc.Method.Name == nameof(Enumerable.Contains) &&
            mc.Arguments.Count == 2)
            return true;

        // Instance: ICollection<T>.Contains(T item) — e.g., List<T>, HashSet<T>, int[]
        if (mc.Object is not null &&
            mc.Method.Name == "Contains" &&
            mc.Arguments.Count == 1 &&
            mc.Object.Type != typeof(string)) // exclude string.Contains — handled by IsStringMethod
            return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SqlBinaryOperator MapOperator(ExpressionType nodeType) => nodeType switch
    {
        ExpressionType.Equal => SqlBinaryOperator.Equal,
        ExpressionType.NotEqual => SqlBinaryOperator.NotEqual,
        ExpressionType.GreaterThan => SqlBinaryOperator.GreaterThan,
        ExpressionType.GreaterThanOrEqual => SqlBinaryOperator.GreaterThanOrEqual,
        ExpressionType.LessThan => SqlBinaryOperator.LessThan,
        ExpressionType.LessThanOrEqual => SqlBinaryOperator.LessThanOrEqual,
        _ => throw new NotSupportedException($"Operator '{nodeType}' cannot be mapped to a SQL operator.")
    };
}


