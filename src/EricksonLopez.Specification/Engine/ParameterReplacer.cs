// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace EricksonLopez.Specification;

/// <summary>
/// Replaces parameter expressions within an expression tree.
/// </summary>
/// <remarks>
/// <para>
/// This visitor performs a single-pass tree walk to replace parameter references.
/// It avoids creating any dictionary when replacing a single parameter, using direct
/// field comparison instead for optimal performance.
/// </para>
/// </remarks>
internal sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _source;
    private readonly ParameterExpression _target;

    private ParameterReplacer(ParameterExpression source, ParameterExpression target)
    {
        _source = source;
        _target = target;
    }

    /// <summary>
    /// Returns an expression equivalent to <paramref name="expression"/> but with all
    /// references to <paramref name="source"/> replaced by <paramref name="target"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Expression Replace(
        Expression expression,
        ParameterExpression source,
        ParameterExpression target)
    {
        if (ReferenceEquals(source, target))
            return expression;

        return new ParameterReplacer(source, target).Visit(expression);
    }

    /// <inheritdoc/>
    protected override Expression VisitParameter(ParameterExpression node)
        => ReferenceEquals(node, _source) ? _target : base.VisitParameter(node);
}

