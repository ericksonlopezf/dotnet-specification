// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Represents a specification constructed directly from an expression predicate without requiring a subclass.
/// </summary>
/// <typeparam name="T">The entity type evaluated by this specification.</typeparam>
internal sealed class LambdaSpecification<
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicFields)] T> : Specification<T>
{
    private readonly Expression<Func<T, bool>> _expression;

    internal LambdaSpecification(Expression<Func<T, bool>> expression) => _expression = expression;

    /// <inheritdoc/>
    protected override Expression<Func<T, bool>> BuildExpression() => _expression;
}
