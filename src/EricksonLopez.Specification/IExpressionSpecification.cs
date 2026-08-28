// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Defines a specification that can be converted to an expression tree for query providers.
/// </summary>
/// <typeparam name="T">The entity type evaluated by this specification.</typeparam>
public interface IExpressionSpecification<
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicFields)] T> : ISpecification<T>
{
    /// <summary>
    /// Converts this specification to a LINQ expression tree.
    /// </summary>
    /// <returns>An expression tree representing the specification predicate.</returns>
    Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Returns a human-readable string representation of this specification's expression.
    /// </summary>
    /// <returns>A string representation of the specification expression.</returns>
    /// <remarks>
    /// The default implementation delegates to <see cref="ExpressionDebugFormatter"/>.
    /// </remarks>
    string ToDebugString() => ExpressionDebugFormatter.Format(ToExpression());
}


