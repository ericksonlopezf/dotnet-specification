// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace EricksonLopez.Specification;

/// <summary>
/// Represents an immutable query specification descriptor defining criteria, ordering, and pagination for entities of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entity type being queried.</typeparam>
/// <remarks>
/// This type is immutable and thread-safe. Combinator methods return a new instance without mutating the original.
/// </remarks>
public sealed record QuerySpec<T>
{
    /// <summary>Gets an empty specification with no constraints.</summary>
    public static readonly QuerySpec<T> Empty = new();

    /// <summary>Gets the filter predicates combined using logical AND.</summary>
    public ImmutableArray<Expression<Func<T, bool>>> Criteria { get; init; } = [];

    /// <summary>Gets the ordering clauses applied in sequence.</summary>
    public ImmutableArray<OrderClause<T>> OrderClauses { get; init; } = [];

    /// <summary>Gets the number of records to skip for offset-based pagination.</summary>
    public int? SkipCount { get; init; }

    /// <summary>Gets the maximum number of records to return.</summary>
    public int? TakeCount { get; init; }

    /// <summary>Gets a value indicating whether duplicate results should be eliminated.</summary>
    public bool IsDistinct { get; init; }

    /// <summary>Gets the diagnostic query tag or comment, if configured.</summary>
    public string? Tag { get; init; }

    /// <summary>Gets the cursor clause for keyset pagination, if configured.</summary>
    public CursorClause<T>? Cursor { get; init; }

    /// <summary>
    /// Creates a new specification configured for keyset pagination seeking records after the specified cursor.
    /// </summary>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <param name="cursorValue">The cursor value to seek after.</param>
    /// <param name="take">The number of records to return.</param>
    /// <returns>A new specification with the cursor and limit applied.</returns>
    public QuerySpec<T> SeekAfter<TKey>(Expression<Func<T, TKey>> keySelector, TKey cursorValue, int take) =>
        WithCursor(keySelector, cursorValue, CursorDirection.After, take);

    /// <summary>
    /// Creates a new specification configured for keyset pagination seeking records before the specified cursor.
    /// </summary>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <param name="cursorValue">The cursor value to seek before.</param>
    /// <param name="take">The number of records to return.</param>
    /// <returns>A new specification with the cursor and limit applied.</returns>
    public QuerySpec<T> SeekBefore<TKey>(Expression<Func<T, TKey>> keySelector, TKey cursorValue, int take) =>
        WithCursor(keySelector, cursorValue, CursorDirection.Before, take);

    /// <summary>
    /// Creates a new specification with keyset pagination applied.
    /// </summary>
    /// <typeparam name="TKey">The type of the cursor key.</typeparam>
    /// <param name="keySelector">The key selector expression.</param>
    /// <param name="cursorValue">The cursor value.</param>
    /// <param name="direction">The seek direction.</param>
    /// <param name="take">The maximum number of records to return.</param>
    /// <returns>A new specification with keyset pagination applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="take"/> is less than 1</exception>
    public QuerySpec<T> WithCursor<TKey>(
        Expression<Func<T, TKey>> keySelector,
        TKey cursorValue,
        CursorDirection direction,
        int take)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);

        var clause = new CursorClause<T>(ConvertKeySelector(keySelector), cursorValue, direction);
        return this with { Cursor = clause, TakeCount = take, SkipCount = null };
    }

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> with an additional AND predicate.
    /// </summary>
    /// <param name="predicate">The predicate to add.</param>
    /// <returns>A new specification with the predicate appended to criteria.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/></exception>
    public QuerySpec<T> Where(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return this with { Criteria = Criteria.Add(predicate) };
    }

    private static readonly System.Reflection.MethodInfo StringContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> with a search filter across one or more string properties using logical OR.
    /// </summary>
    /// <param name="searchPhrase">The search term to look for. The comparison is <b>case-sensitive</b> (uses <see cref="string.Contains(string)"/>).</param>
    /// <param name="propertySelectors">The string property selectors to search within.</param>
    /// <returns>A new specification with the search criteria applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="searchPhrase"/>, <paramref name="propertySelectors"/>, or any element of <paramref name="propertySelectors"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="propertySelectors"/> is empty</exception>
    /// <remarks>
    /// The search uses <c>string.Contains(searchPhrase)</c> which is <b>case-sensitive</b>.
    /// For case-insensitive search, normalize the search phrase and property value to the same case
    /// before calling this method, or apply a provider-specific approach (e.g., <c>EF.Functions.Like</c>).
    /// </remarks>
    public QuerySpec<T> Search(string searchPhrase, params Expression<Func<T, string?>>[] propertySelectors)
    {
        ArgumentNullException.ThrowIfNull(searchPhrase);
        ArgumentNullException.ThrowIfNull(propertySelectors);
        if (propertySelectors.Length == 0)
        {
            throw new ArgumentException("At least one property selector must be provided.", nameof(propertySelectors));
        }

        var first = propertySelectors[0];
        ArgumentNullException.ThrowIfNull(first);
        var param = first.Parameters[0];
        var phraseExpr = Expression.Constant(searchPhrase, typeof(string));

        Expression? combined = null;
        foreach (var selector in propertySelectors)
        {
            ArgumentNullException.ThrowIfNull(selector);
            var replacedBody = ReplaceParameter(selector.Body, selector.Parameters[0], param);
            var call = Expression.Call(replacedBody, StringContainsMethod, phraseExpr);
            combined = combined is null ? call : Expression.OrElse(combined, call);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(combined!, param);
        return Where(lambda);
    }

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> with the specified diagnostic query tag applied.
    /// </summary>
    /// <param name="tag">The diagnostic query tag or comment.</param>
    /// <returns>A new specification with the tag applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="tag"/> is <see langword="null"/> or whitespace</exception>
    public QuerySpec<T> TagWith(string tag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);
        return this with { Tag = tag };
    }

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> with an ascending order applied.
    /// </summary>
    /// <typeparam name="TKey">The type of the ordering key.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <returns>A new specification with the ordering applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/></exception>
    public QuerySpec<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        var clause = new OrderClause<T>(ConvertKeySelector(keySelector), OrderDirection.Ascending);
        return this with { OrderClauses = OrderClauses.Add(clause) };
    }

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> with a descending order applied.
    /// </summary>
    /// <typeparam name="TKey">The type of the ordering key.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <returns>A new specification with the ordering applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/></exception>
    public QuerySpec<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        ArgumentNullException.ThrowIfNull(keySelector);
        var clause = new OrderClause<T>(ConvertKeySelector(keySelector), OrderDirection.Descending);
        return this with { OrderClauses = OrderClauses.Add(clause) };
    }

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> with a secondary ascending order applied.
    /// </summary>
    /// <typeparam name="TKey">The type of the ordering key.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <returns>A new specification with the secondary ordering applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/></exception>
    public QuerySpec<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector) =>
        OrderBy(keySelector);

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> with a secondary descending order applied.
    /// </summary>
    /// <typeparam name="TKey">The type of the ordering key.</typeparam>
    /// <param name="keySelector">The key selector.</param>
    /// <returns>A new specification with the secondary ordering applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is <see langword="null"/></exception>
    public QuerySpec<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector) =>
        OrderByDescending(keySelector);

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> with offset-based pagination applied.
    /// </summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The number of records per page. Must be positive.</param>
    /// <returns>A new specification with pagination applied.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="page"/> or <paramref name="pageSize"/> is less than 1
    /// </exception>
    public QuerySpec<T> Page(int page, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return this with
        {
            SkipCount = (page - 1) * pageSize,
            TakeCount = pageSize
        };
    }

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> that skips the first <paramref name="count"/> records.
    /// </summary>
    /// <param name="count">The number of records to skip. Must be non-negative.</param>
    /// <returns>A new specification with the skip count applied.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative</exception>
    public QuerySpec<T> Skip(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return this with { SkipCount = count };
    }

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> that returns at most <paramref name="count"/> records.
    /// </summary>
    /// <param name="count">The maximum number of records to return. Must be positive.</param>
    /// <returns>A new specification with the take count applied.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1</exception>
    public QuerySpec<T> Take(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        return this with { TakeCount = count };
    }

    /// <summary>
    /// Creates a new <see cref="QuerySpec{T}"/> that eliminates duplicate results.
    /// </summary>
    /// <returns>A new specification with distinct enabled.</returns>
    public QuerySpec<T> Distinct() => this with { IsDistinct = true };

    private static Expression<Func<T, object?>> ConvertKeySelector<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        var param = keySelector.Parameters[0];
        var body = Expression.Convert(keySelector.Body, typeof(object));
        return Expression.Lambda<Func<T, object?>>(body, param);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression source, ParameterExpression target)
        => new ParameterRewriter(source, target).Visit(expression);

    private sealed class ParameterRewriter : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;

        public ParameterRewriter(ParameterExpression source, ParameterExpression target)
        {
            _source = source;
            _target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => ReferenceEquals(node, _source) ? _target : node;
    }
}


