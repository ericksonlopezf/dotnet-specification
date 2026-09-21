// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Specification;

/// <summary>
/// Encapsulates a domain specification as an expression tree that can be evaluated in memory and translated by query providers.
/// </summary>
/// <typeparam name="T">The entity or value object type this specification evaluates.</typeparam>
/// <remarks>
/// This class is thread-safe. The underlying expression tree is lazily built and cached upon first access.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1724:Type names should not match namespaces",
    Justification = "Specification<T> intentionally shares its name with the EricksonLopez.Specification namespace — " +
                    "this is a deliberate design choice for API clarity and follows the same pattern as Task.")]
public abstract class Specification<
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicFields)] T> : IExpressionSpecification<T>
{
    private readonly Lazy<Expression<Func<T, bool>>> _expression;

    static Specification()
    {
        ExpressionDebugFormatterRegistry.Formatter = ExpressionDebugFormatter.Format;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Specification{T}"/> class.
    /// </summary>
    /// <remarks>
    /// Increments the <see cref="Diagnostics.SpecificationDiagnostics.SpecificationsCreated"/> telemetry counter.
    /// This is an observable side-effect: if you monitor specification instantiation counts,
    /// this counter will reflect every subclass constructor invocation.
    /// </remarks>
    protected Specification()
    {
        _expression = new Lazy<Expression<Func<T, bool>>>(BuildExpression, isThreadSafe: true);
        Diagnostics.SpecificationDiagnostics.SpecificationsCreated.Add(1);
    }

    /// <summary>
    /// Builds the expression tree that represents this specification business rule.
    /// </summary>
    /// <returns>An expression tree representing the predicate.</returns>
    protected abstract Expression<Func<T, bool>> BuildExpression();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Expression<Func<T, bool>> ToExpression() => _expression.Value;

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="candidate"/> is <see langword="null"/></exception>
    public bool IsSatisfiedBy(T candidate)
    {
        // Stryker disable once Statement : Redundant null check, delegated to ExpressionInterpreter
        ArgumentNullException.ThrowIfNull(candidate);
        return ExpressionInterpreter.Evaluate(ToExpression(), candidate);
    }

    /// <inheritdoc/>
    public string ToDebugString() => ExpressionDebugFormatter.Format(ToExpression());

    /// <summary>
    /// Compiles or retrieves the cached delegate for this specification expression.
    /// </summary>
    /// <returns>A compiled predicate delegate.</returns>
    /// <remarks>
    /// Results are cached globally by expression structural equality.
    /// </remarks>
    [RequiresDynamicCode("Compiles the specification expression to a delegate at runtime. Not compatible with Native AOT.")]
    [RequiresUnreferencedCode("Expression compilation may require types that are trimmed.")]
    public Func<T, bool> ToCompiledPredicate()
        => ExpressionCompilationCache.GetOrCompile(ToExpression());

    /// <summary>
    /// Composes this specification with another using logical AND.
    /// </summary>
    /// <param name="other">The specification to combine with.</param>
    /// <returns>A new composite specification.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/></exception>
    public Specification<T> And(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new CompositeSpecification<T>(this, other, CompositionKind.And);
    }

    /// <summary>
    /// Composes this specification with another using logical OR.
    /// </summary>
    /// <param name="other">The specification to combine with.</param>
    /// <returns>A new composite specification.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/></exception>
    public Specification<T> Or(Specification<T> other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new CompositeSpecification<T>(this, other, CompositionKind.Or);
    }

    /// <summary>
    /// Creates a new specification that represents the logical negation of this specification.
    /// </summary>
    /// <returns>A new specification that is satisfied when this specification is not.</returns>
    public Specification<T> Not()
        => new NegatedSpecification<T>(this);

    /// <summary>
    /// Converts this specification to an equivalent <see cref="QuerySpec{T}"/>.
    /// </summary>
    /// <returns>A new query specification constrained by this specification predicate.</returns>
    public QuerySpec<T> ToQuerySpec()
        => QuerySpec<T>.Empty.Where(ToExpression());

    /// <summary>
    /// Applies this specification predicate to the specified query specification.
    /// </summary>
    /// <param name="querySpec">The query specification to apply this predicate to.</param>
    /// <returns>A new query specification containing the existing query settings and this predicate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="querySpec"/> is <see langword="null"/></exception>
    public QuerySpec<T> ToQuerySpec(QuerySpec<T> querySpec)
    {
        ArgumentNullException.ThrowIfNull(querySpec);
        return querySpec.Where(ToExpression());
    }

    /// <summary>
    /// Converts a <see cref="Specification{T}"/> to a <see cref="QuerySpec{T}"/>.
    /// </summary>
    /// <param name="specification">The specification to convert.</param>
    /// <returns>A new query specification constrained by the specification predicate.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/></exception>
    public static implicit operator QuerySpec<T>(Specification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return specification.ToQuerySpec();
    }

    /// <summary>
    /// Combines two specifications using logical AND.
    /// </summary>
    /// <param name="left">The left specification.</param>
    /// <param name="right">The right specification.</param>
    /// <returns>A new composite specification representing (left AND right).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/></exception>
    public static Specification<T> operator &(Specification<T> left, Specification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.And(right);
    }

    /// <summary>
    /// Combines two specifications using logical AND as a named alternative to <see cref="operator &amp;"/>.
    /// </summary>
    /// <param name="left">The left specification.</param>
    /// <param name="right">The right specification.</param>
    /// <returns>A new composite specification representing (<paramref name="left"/> AND <paramref name="right"/>).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/></exception>
    public static Specification<T> BitwiseAnd(Specification<T> left, Specification<T> right) => left & right;

    /// <summary>
    /// Combines two specifications using logical OR.
    /// </summary>
    /// <param name="left">The left specification.</param>
    /// <param name="right">The right specification.</param>
    /// <returns>A new composite specification representing (left OR right).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/></exception>
    public static Specification<T> operator |(Specification<T> left, Specification<T> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Or(right);
    }

    /// <summary>
    /// Combines two specifications using logical OR as a named alternative to <see cref="operator |"/>.
    /// </summary>
    /// <param name="left">The left specification.</param>
    /// <param name="right">The right specification.</param>
    /// <returns>A new composite specification representing (<paramref name="left"/> OR <paramref name="right"/>).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <see langword="null"/></exception>
    public static Specification<T> BitwiseOr(Specification<T> left, Specification<T> right) => left | right;

    /// <summary>
    /// Negates the specified specification.
    /// </summary>
    /// <param name="specification">The specification to negate.</param>
    /// <returns>A new negated specification representing (NOT specification).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/></exception>
    public static Specification<T> operator !(Specification<T> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        return specification.Not();
    }

    /// <summary>
    /// Negates the specified specification as a named alternative to <see cref="operator !"/>.
    /// </summary>
    /// <param name="specification">The specification to negate.</param>
    /// <returns>A new negated specification representing (NOT <paramref name="specification"/>).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null"/></exception>
    public static Specification<T> LogicalNot(Specification<T> specification) => !specification;

    /// <summary>
    /// Determines whether the specification evaluates to false for short-circuit evaluation in logical AND expressions.
    /// </summary>
    /// <param name="specification">The specification to evaluate.</param>
    /// <returns>Always <see langword="false"/> to ensure both operands are evaluated.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
        Justification = "Short-circuit operator pair (true/false) is an internal C# language idiom without a standard named alternative.")]
    public static bool operator false(Specification<T> specification) => false;

    /// <summary>
    /// Determines whether the specification evaluates to true for short-circuit evaluation in logical OR expressions.
    /// </summary>
    /// <param name="specification">The specification to evaluate.</param>
    /// <returns>Always <see langword="false"/> to ensure both operands are evaluated.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2225:Operator overloads have named alternates",
        Justification = "Short-circuit operator pair (true/false) is an internal C# language idiom without a standard named alternative.")]
    public static bool operator true(Specification<T> specification) => false;
}





