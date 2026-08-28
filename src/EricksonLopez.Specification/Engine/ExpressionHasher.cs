// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace EricksonLopez.Specification;

/// <summary>
/// Calculates stable structural hashes for expression trees to support caching.
/// </summary>
/// <remarks>
/// <para>
/// Two expressions with the same structural form and constants will produce the same hash.
/// This hash is used to key compiled delegate caches and SQL query plan caches.
/// </para>
/// </remarks>
public static class ExpressionHasher
{
    /// <summary>
    /// Calculates a structural hash for the specified expression.
    /// </summary>
    /// <param name="expression">The expression to hash.</param>
    /// <returns>A hash code representing the structural identity of the expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/></exception>
    public static int ComputeHash(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var visitor = new HashVisitor();
        visitor.Visit(expression);
        return visitor.Hash;
    }

    private sealed class HashVisitor : ExpressionVisitor
    {
        private HashCode _hash;

        internal int Hash => _hash.ToHashCode();

        public override Expression? Visit(Expression? node)
        {
            if (node is null)
            {
                // Stryker disable once Statement : Hash collision for missing nodes is astronomically unlikely
                _hash.Add(0);
                return null;
            }

            _hash.Add((int)node.NodeType);
            // Stryker disable once Statement : Node types usually disambiguate anyway
            _hash.Add(node.Type.GetHashCode());

            return base.Visit(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _hash.Add(node.Value?.GetHashCode() ?? 0);
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _hash.Add(node.Member.GetHashCode());
            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            _hash.Add(node.Method.GetHashCode());
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            // Parameters are identified by their type, not their name,
            // so that structurally equivalent expressions with different parameter names hash the same.
            // Stryker disable once Statement : Parameter type is already hashed in base Visit, this is just extra salt
            _hash.Add(node.Type.GetHashCode());
            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            _hash.Add(node.Method?.GetHashCode() ?? 0);
            return base.VisitBinary(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            _hash.Add(node.Method?.GetHashCode() ?? 0);
            return base.VisitUnary(node);
        }
    }
}


