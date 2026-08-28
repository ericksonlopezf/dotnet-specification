// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Represents the logical NOT negation of a specification.
/// </summary>
/// <typeparam name="T">The entity type evaluated by this specification.</typeparam>
internal sealed class NegatedSpecification<
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicFields)] T> : Specification<T>
{
    private readonly Specification<T> _inner;

    internal NegatedSpecification(Specification<T> inner) => _inner = inner;

    /// <inheritdoc/>
    protected override Expression<Func<T, bool>> BuildExpression()
        => ExpressionComposer.Not(_inner.ToExpression());
}
