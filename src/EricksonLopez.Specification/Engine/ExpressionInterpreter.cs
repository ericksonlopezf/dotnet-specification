// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.Specification;

/// <summary>
/// Provides an AOT-safe interpreted evaluator for domain predicate expressions.
/// </summary>
/// <remarks>
/// <para>
/// This evaluator supports expression node types commonly used in domain specifications:
/// member access, constant comparisons, binary operators (and, or, coalesce, equality, comparison),
/// unary operators (not, convert), conditional ternary expressions, type binary tests (<c>is</c>),
/// and method calls.
/// </para>
/// </remarks>
public static class ExpressionInterpreter
{
    /// <summary>
    /// Evaluates whether a predicate expression is satisfied by a candidate value without runtime IL compilation.
    /// </summary>
    /// <typeparam name="T">The candidate type.</typeparam>
    /// <param name="expression">The predicate expression to evaluate.</param>
    /// <param name="candidate">The candidate value to test.</param>
    /// <returns><see langword="true"/> if the candidate satisfies the expression; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> or <paramref name="candidate"/> is <see langword="null"/></exception>
    public static bool Evaluate<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        Expression<Func<T, bool>> expression,
        T candidate)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(candidate);
        var result = EvaluateNode(expression.Body, expression.Parameters[0], candidate);
        return (bool)result!;
    }

    private static object? EvaluateNode<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        Expression node,
        ParameterExpression param,
        T candidate)
    {
        return node switch
        {
            ConstantExpression c => c.Value,
            ParameterExpression p when ReferenceEquals(p, param) => candidate,
            MemberExpression m => EvaluateMember(m, param, candidate),
            BinaryExpression b => EvaluateBinary(b, param, candidate),
            UnaryExpression u => EvaluateUnary(u, param, candidate),
            ConditionalExpression cond => EvaluateConditional(cond, param, candidate),
            TypeBinaryExpression tb => EvaluateTypeBinary(tb, param, candidate),
            MethodCallExpression mc => EvaluateMethodCall(mc, param, candidate),
            _ => throw new NotSupportedException(
                $"Expression node type '{node.NodeType}' is not supported by the interpreted evaluator. " +
                $"Use ToCompiledPredicate() in JIT environments for full expression support.")
        };
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2072",
        Justification = "EvaluateMember reflects over the candidate's own declared properties/fields preserved via DynamicallyAccessedMembers on T.")]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Defensive member switch: BCL Expression.MakeMemberAccess guarantees node is either FieldInfo or PropertyInfo")]
    private static object? EvaluateMember<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        MemberExpression m,
        ParameterExpression param,
        T candidate)
    {
        var instance = m.Expression is null ? null : EvaluateNode(m.Expression, param, candidate);
        return m.Member switch
        {
            System.Reflection.PropertyInfo p => p.GetValue(instance),
            System.Reflection.FieldInfo f => f.GetValue(instance),
            // Stryker disable once String: Impossible to reach since Expression.MakeMemberAccess guarantees Field or Property
            _ => throw new NotSupportedException($"Unsupported member type: {m.Member.GetType().Name}")
        };
    }

    private static object? EvaluateBinary<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        BinaryExpression b,
        ParameterExpression param,
        T candidate)
    {
        if (b.NodeType == ExpressionType.AndAlso)
        {
            var left = EvaluateNode(b.Left, param, candidate);
            if (left is false) return false;
            var right = EvaluateNode(b.Right, param, candidate);
            if (right is false) return false;
            if (left is null || right is null) return null;
            return true;
        }

        if (b.NodeType == ExpressionType.OrElse)
        {
            var left = EvaluateNode(b.Left, param, candidate);
            if (left is true) return true;
            var right = EvaluateNode(b.Right, param, candidate);
            if (right is true) return true;
            if (left is null || right is null) return null;
            return false;
        }

        if (b.NodeType == ExpressionType.Coalesce)
        {
            var left = EvaluateNode(b.Left, param, candidate);
            return left ?? EvaluateNode(b.Right, param, candidate);
        }

        var leftVal = EvaluateNode(b.Left, param, candidate);
        var rightVal = EvaluateNode(b.Right, param, candidate);

        return b.NodeType switch
        {
            ExpressionType.Equal => Equals(leftVal, rightVal),
            ExpressionType.NotEqual => !Equals(leftVal, rightVal),
            ExpressionType.GreaterThan => CompareValues(leftVal, rightVal) > 0,
            ExpressionType.GreaterThanOrEqual => CompareValues(leftVal, rightVal) >= 0,
            ExpressionType.LessThan => CompareValues(leftVal, rightVal) < 0,
            ExpressionType.LessThanOrEqual => CompareValues(leftVal, rightVal) <= 0,
            _ => throw new NotSupportedException($"Binary operator '{b.NodeType}' is not supported by the interpreted evaluator.")
        };
    }

    private static object? EvaluateConditional<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        ConditionalExpression cond,
        ParameterExpression param,
        T candidate)
    {
        var test = EvaluateNode(cond.Test, param, candidate);
        return test is true
            ? EvaluateNode(cond.IfTrue, param, candidate)
            : EvaluateNode(cond.IfFalse, param, candidate);
    }

    private static object? EvaluateTypeBinary<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        TypeBinaryExpression tb,
        ParameterExpression param,
        T candidate)
    {
        if (tb.NodeType == ExpressionType.TypeIs)
        {
            var operand = EvaluateNode(tb.Expression, param, candidate);
            return operand != null && tb.TypeOperand.IsAssignableFrom(operand.GetType());
        }

        throw new NotSupportedException($"TypeBinary operator '{tb.NodeType}' is not supported by the interpreted evaluator.");
    }

    private static object? EvaluateUnary<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        UnaryExpression u,
        ParameterExpression param,
        T candidate)
    {
        var operand = EvaluateNode(u.Operand, param, candidate);
        return u.NodeType switch
        {
            ExpressionType.Not => operand is false || operand is null,
            ExpressionType.Convert => Convert.ChangeType(operand, u.Type, System.Globalization.CultureInfo.InvariantCulture),
            _ => throw new NotSupportedException($"Unary operator '{u.NodeType}' is not supported by the interpreted evaluator.")
        };
    }

    private static object? EvaluateMethodCall<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        MethodCallExpression mc,
        ParameterExpression param,
        T candidate)
    {
        var instance = mc.Object is null ? null : EvaluateNode(mc.Object, param, candidate);
        var args = new object?[mc.Arguments.Count];
        for (var i = 0; i < mc.Arguments.Count; i++)
            args[i] = EvaluateNode(mc.Arguments[i], param, candidate);

        return mc.Method.Invoke(instance, args);
    }

    private static int CompareValues(object? left, object? right)
    {
        if (left is IComparable comparable)
            return comparable.CompareTo(right);

        // Stryker disable once all : Non-IComparable comparison is guarded by Expression tree construction
        throw new NotSupportedException(
            $"Cannot compare values of type '{left?.GetType().Name ?? "null"}'. " +
            "Type must implement IComparable.");
    }
}



