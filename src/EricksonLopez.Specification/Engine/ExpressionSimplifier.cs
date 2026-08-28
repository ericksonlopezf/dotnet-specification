// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Performs constant folding and Boolean simplification on predicate expressions.
/// </summary>
/// <remarks>
/// <para>
/// These simplifications are applied during expression composition and eliminate
/// trivial sub-expressions before they reach the database query translator:
/// </para>
/// <list type="bullet">
/// <item><c>A AND TRUE  → A</c></item>
/// <item><c>A AND FALSE → FALSE</c></item>
/// <item><c>A OR TRUE   → TRUE</c></item>
/// <item><c>A OR FALSE  → A</c></item>
/// <item><c>NOT(TRUE)   → FALSE</c></item>
/// <item><c>NOT(FALSE)  → TRUE</c></item>
/// <item><c>NOT(NOT(A)) → A</c></item>
/// </list>
/// <para>
/// <strong>When to use:</strong> Apply simplification when composing specifications
/// that may introduce Boolean constants (e.g., a specification that is always-true
/// or always-false based on a conditional flag).
/// </para>
/// <para>
/// <strong>When NOT to use:</strong> Do not apply blindly to every expression — it
/// adds a tree traversal overhead. Use it selectively when constants are known to appear.
/// </para>
/// </remarks>
public sealed class ExpressionSimplifier : ExpressionVisitor
{
    /// <summary>Gets the singleton instance of the simplifier.</summary>
    public static readonly ExpressionSimplifier Default = new();

    private ExpressionSimplifier() { }

    /// <summary>
    /// Simplifies the specified predicate expression by applying constant folding rules.
    /// </summary>
    /// <typeparam name="T">The predicate input type.</typeparam>
    /// <param name="expression">The expression to simplify.</param>
    /// <returns>
    /// A simplified expression, or the original expression if no simplification was possible.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/></exception>
    public static Expression<Func<T, bool>> Simplify<T>(Expression<Func<T, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var simplified = Default.Visit(expression);
        return (Expression<Func<T, bool>>)simplified;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/></exception>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (node.NodeType == ExpressionType.AndAlso)
        {
            // TRUE AND x → x
            if (IsConstantTrue(left)) return right;
            // x AND TRUE → x
            if (IsConstantTrue(right)) return left;
            // FALSE AND x → FALSE
            if (IsConstantFalse(left)) return Expression.Constant(false);
            // x AND FALSE → FALSE
            if (IsConstantFalse(right)) return Expression.Constant(false);
        }
        else if (node.NodeType == ExpressionType.OrElse)
        {
            // TRUE OR x → TRUE
            if (IsConstantTrue(left)) return Expression.Constant(true);
            // x OR TRUE → TRUE
            if (IsConstantTrue(right)) return Expression.Constant(true);
            // FALSE OR x → x
            if (IsConstantFalse(left)) return right;
            // x OR FALSE → x
            if (IsConstantFalse(right)) return left;
        }

        // No simplification possible — return rebuilt node (may be identical)
        return ReferenceEquals(left, node.Left) && ReferenceEquals(right, node.Right)
            ? node
            : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/></exception>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.NodeType != ExpressionType.Not)
            return base.VisitUnary(node);

        var operand = Visit(node.Operand);

        // NOT(TRUE) → FALSE
        if (IsConstantTrue(operand)) return Expression.Constant(false);
        // NOT(FALSE) → TRUE
        if (IsConstantFalse(operand)) return Expression.Constant(true);
        // NOT(NOT(A)) → A
        if (operand is UnaryExpression inner && inner.NodeType == ExpressionType.Not)
            return inner.Operand;

        return ReferenceEquals(operand, node.Operand)
            ? node
            : Expression.Not(operand);
    }

    private static bool IsConstantTrue(Expression expr)
        => expr is ConstantExpression { Value: true };

    private static bool IsConstantFalse(Expression expr)
        => expr is ConstantExpression { Value: false };
}


