// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Represents the logical AND or OR composition of two specifications.
/// </summary>
/// <typeparam name="T">The entity type evaluated by this specification.</typeparam>
internal sealed class CompositeSpecification<
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicFields)] T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;
    private readonly CompositionKind _kind;

    internal CompositeSpecification(Specification<T> left, Specification<T> right, CompositionKind kind)
    {
        _left = left;
        _right = right;
        _kind = kind;
        Diagnostics.SpecificationDiagnostics.SpecificationsComposed.Add(1);
    }

    /// <inheritdoc/>
    protected override Expression<Func<T, bool>> BuildExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var composed = _kind switch
        {
            CompositionKind.And => ExpressionComposer.And(leftExpr, rightExpr),
            CompositionKind.Or => ExpressionComposer.Or(leftExpr, rightExpr),
            _ => throw new InvalidOperationException($"Unknown composition kind: {_kind}")
        };

        // Simplify constant-identity compositions: Spec.True<T>().And(x) → x, Spec.False<T>().Or(x) → x.
        // We only simplify when at least one operand body is a ConstantExpression to minimize overhead.
        if (leftExpr.Body is ConstantExpression || rightExpr.Body is ConstantExpression)
            return ExpressionSimplifier.Simplify(composed);

        return composed;
    }
}



