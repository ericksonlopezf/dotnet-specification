// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Provides factory methods for creating specifications from lambda expressions without subclassing.
/// </summary>
/// <remarks>
/// Use this factory for simple, one-off specifications. For reusable domain rules,
/// prefer subclassing <see cref="Specification{T}"/> for better discoverability and naming.
/// </remarks>
public static class Spec
{
    /// <summary>
    /// Creates a specification from the specified predicate expression.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="predicate">The predicate expression defining the rule.</param>
    /// <returns>A specification backed by the given predicate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/></exception>
    /// <example>
    /// <code>
    /// var activeSpec = Spec.For&lt;Customer&gt;(c =&gt; c.IsActive);
    /// var result = activeSpec.IsSatisfiedBy(customer);
    /// </code>
    /// </example>
    public static Specification<T> For<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(Expression<Func<T, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new LambdaSpecification<T>(predicate);
    }

    /// <summary>
    /// Creates a specification that is always satisfied (tautology).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A specification that always returns <see langword="true"/>.</returns>
    public static Specification<T> True<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>()
    {
        return new LambdaSpecification<T>(_ => true);
    }

    /// <summary>
    /// Creates a specification that is never satisfied (contradiction).
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>A specification that always returns <see langword="false"/>.</returns>
    public static Specification<T> False<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>()
    {
        return new LambdaSpecification<T>(_ => false);
    }

    /// <summary>
    /// Composes all specifications using logical AND.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="specifications">The specifications to compose with AND.</param>
    /// <returns>A specification that is satisfied only when all of <paramref name="specifications"/> are satisfied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="specifications"/> is <see langword="null"/></exception>
    /// <example>
    /// <code>
    /// var spec = Spec.All(new ActiveSpec(), new PremiumSpec(), new HasCreditSpec());
    /// // Equivalent to: new ActiveSpec().And(new PremiumSpec()).And(new HasCreditSpec())
    /// </code>
    /// </example>
    public static Specification<T> All<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(params Specification<T>[] specifications)
    {
        ArgumentNullException.ThrowIfNull(specifications);

        if (specifications.Length == 0) return True<T>();
        if (specifications.Length == 1) return specifications[0];

        var expressions = System.Array.ConvertAll(specifications, s => s.ToExpression());
        return new LambdaSpecification<T>(ExpressionComposer.AndAll<T>(expressions.AsSpan()));
    }

    /// <summary>
    /// Composes all specifications using logical OR.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="specifications">The specifications to compose with OR.</param>
    /// <returns>A specification that is satisfied when any of <paramref name="specifications"/> is satisfied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="specifications"/> is <see langword="null"/></exception>
    /// <example>
    /// <code>
    /// var spec = Spec.Any(new PremiumSpec(), new VipSpec(), new StaffSpec());
    /// // Equivalent to: new PremiumSpec().Or(new VipSpec()).Or(new StaffSpec())
    /// </code>
    /// </example>
    public static Specification<T> Any<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(params Specification<T>[] specifications)
    {
        ArgumentNullException.ThrowIfNull(specifications);

        if (specifications.Length == 0) return False<T>();
        if (specifications.Length == 1) return specifications[0];

        var expressions = System.Array.ConvertAll(specifications, s => s.ToExpression());
        return new LambdaSpecification<T>(ExpressionComposer.OrAny<T>(expressions.AsSpan()));
    }

    /// <summary>
    /// Creates a specification that determines whether a property value is inclusively between the lower and upper bounds.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">The property selector expression.</param>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns>A specification checking the range.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertySelector"/> is <see langword="null"/></exception>
    public static Specification<T> Between<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T, TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        TProperty lower,
        TProperty upper)
        where TProperty : IComparable<TProperty>
    {
        ArgumentNullException.ThrowIfNull(propertySelector);

        var param = propertySelector.Parameters[0];
        var propExpr = propertySelector.Body;

        var lowerConstant = Expression.Constant(lower, typeof(TProperty));
        var upperConstant = Expression.Constant(upper, typeof(TProperty));

        var gte = Expression.GreaterThanOrEqual(propExpr, lowerConstant);
        var lte = Expression.LessThanOrEqual(propExpr, upperConstant);
        var and = Expression.AndAlso(gte, lte);

        var lambda = Expression.Lambda<Func<T, bool>>(and, param);
        return new LambdaSpecification<T>(lambda);
    }

    /// <summary>
    /// Creates a specification that determines whether a property value is inclusively between the lower and upper bounds.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertySelector">The property selector expression.</param>
    /// <param name="lower">The inclusive lower bound.</param>
    /// <param name="upper">The inclusive upper bound.</param>
    /// <returns>A specification checking the range.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertySelector"/> is <see langword="null"/></exception>
    public static Specification<T> InRange<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T, TProperty>(
        Expression<Func<T, TProperty>> propertySelector,
        TProperty lower,
        TProperty upper)
        where TProperty : IComparable<TProperty>
    {
        return Between(propertySelector, lower, upper);
    }

    private static readonly System.Reflection.MethodInfo MatchesFullTextMethod =
        ((MethodCallExpression)((Expression<Func<string, bool>>)(s => FullTextExtensions.MatchesFullText(s, string.Empty))).Body).Method;

    private static readonly System.Reflection.MethodInfo StringContainsMethod =
        ((MethodCallExpression)((Expression<Func<string, bool>>)(s => s.Contains(string.Empty))).Body).Method;

    /// <summary>
    /// Creates a specification that performs a case-sensitive full-text search match against a string property.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="propertySelector">The property selector expression.</param>
    /// <param name="searchPhrase">The search phrase or keywords. The comparison is <b>case-sensitive</b>.</param>
    /// <returns>A specification checking the full-text search match.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertySelector"/> or <paramref name="searchPhrase"/> is <see langword="null"/></exception>
    /// <remarks>
    /// This is the preferred entry point and delegates to <see cref="MatchesFullText{T}(Expression{Func{T, string?}}, string)"/>.
    /// Both methods are semantically identical; prefer <c>FullText</c> in new code for consistency with the cookbook.
    /// The comparison is <b>case-sensitive</b> (uses <see cref="string.Contains(string)"/> internally).
    /// </remarks>
    /// <seealso cref="MatchesFullText{T}(Expression{Func{T, string?}}, string)"/>
    public static Specification<T> FullText<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        Expression<Func<T, string?>> propertySelector,
        string searchPhrase)
    {
        return MatchesFullText(propertySelector, searchPhrase);
    }

    /// <summary>
    /// Creates a specification that performs a case-sensitive full-text search match against a string property.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="propertySelector">The property selector expression.</param>
    /// <param name="searchPhrase">The search phrase. The comparison is <b>case-sensitive</b>.</param>
    /// <returns>A specification matching full text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertySelector"/> or <paramref name="searchPhrase"/> is <see langword="null"/></exception>
    /// <remarks>
    /// This is the underlying implementation; prefer <see cref="FullText{T}(Expression{Func{T, string?}}, string)"/>
    /// in new code for API consistency. Both are functionally identical.
    /// </remarks>
    /// <seealso cref="FullText{T}(Expression{Func{T, string?}}, string)"/>
    public static Specification<T> MatchesFullText<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        Expression<Func<T, string?>> propertySelector,
        string searchPhrase)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);
        ArgumentNullException.ThrowIfNull(searchPhrase);

        var param = propertySelector.Parameters[0];
        var propExpr = propertySelector.Body;
        var queryExpr = Expression.Constant(searchPhrase, typeof(string));

        var call = Expression.Call(MatchesFullTextMethod, propExpr, queryExpr);
        var lambda = Expression.Lambda<Func<T, bool>>(call, param);
        return new LambdaSpecification<T>(lambda);
    }

    /// <summary>
    /// Creates a specification that searches for a phrase across one or more string properties using logical OR.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="searchPhrase">The text to search for.</param>
    /// <param name="propertySelectors">The string property selectors to search within.</param>
    /// <returns>A specification checking if any of the specified properties contain <paramref name="searchPhrase"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="searchPhrase"/>, <paramref name="propertySelectors"/>, or any element of <paramref name="propertySelectors"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="propertySelectors"/> is empty</exception>
    /// <example>
    /// <code>
    /// var searchSpec = Spec.Search&lt;Customer&gt;("Acme", c =&gt; c.Name, c =&gt; c.Description);
    /// </code>
    /// </example>
    public static Specification<T> Search<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        string searchPhrase,
        params Expression<Func<T, string?>>[] propertySelectors)
    {
        ArgumentNullException.ThrowIfNull(searchPhrase);
        ArgumentNullException.ThrowIfNull(propertySelectors);
        if (propertySelectors.Length == 0)
        {
            throw new ArgumentException("At least one property selector must be provided.", nameof(propertySelectors));
        }

        var param = Expression.Parameter(typeof(T), "x");
        var phraseExpr = Expression.Constant(searchPhrase, typeof(string));

        Expression? combined = null;
        foreach (var selector in propertySelectors)
        {
            ArgumentNullException.ThrowIfNull(selector);
            var replacedBody = ParameterReplacer.Replace(selector.Body, selector.Parameters[0], param);
            var call = Expression.Call(replacedBody, StringContainsMethod, phraseExpr);

            combined = combined is null
                ? call
                : Expression.OrElse(combined, call);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(combined!, param);
        return new LambdaSpecification<T>(lambda);
    }

}



