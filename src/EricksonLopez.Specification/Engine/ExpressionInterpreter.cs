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
    private const int MaxDepth = 512;

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
        var result = EvaluateNode(expression.Body, expression.Parameters[0], candidate, 0);
        return (bool)result!;
    }

    private static object? EvaluateNode<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        Expression node,
        ParameterExpression param,
        T candidate,
        int depth)
    {
        if (depth > MaxDepth)
            throw new InvalidOperationException($"Expression tree exceeds maximum supported evaluation depth of {MaxDepth}.");

        return node switch
        {
            ConstantExpression c => c.Value,
            ParameterExpression p when ReferenceEquals(p, param) => candidate,
            MemberExpression m => EvaluateMember(m, param, candidate, depth + 1),
            BinaryExpression b => EvaluateBinary(b, param, candidate, depth + 1),
            UnaryExpression u => EvaluateUnary(u, param, candidate, depth + 1),
            ConditionalExpression cond => EvaluateConditional(cond, param, candidate, depth + 1),
            TypeBinaryExpression tb => EvaluateTypeBinary(tb, param, candidate, depth + 1),
            MethodCallExpression mc => EvaluateMethodCall(mc, param, candidate, depth + 1),
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
        T candidate,
        int depth)
    {
        var instance = m.Expression is null ? null : EvaluateNode(m.Expression, param, candidate, depth);
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
        T candidate,
        int depth)
    {
        if (b.NodeType == ExpressionType.AndAlso)
        {
            var left = EvaluateNode(b.Left, param, candidate, depth);
            if (left is false) return false;
            var right = EvaluateNode(b.Right, param, candidate, depth);
            if (right is false) return false;
            if (left is null || right is null) return null;
            return true;
        }

        if (b.NodeType == ExpressionType.OrElse)
        {
            var left = EvaluateNode(b.Left, param, candidate, depth);
            if (left is true) return true;
            var right = EvaluateNode(b.Right, param, candidate, depth);
            if (right is true) return true;
            if (left is null || right is null) return null;
            return false;
        }

        if (b.NodeType == ExpressionType.Coalesce)
        {
            var left = EvaluateNode(b.Left, param, candidate, depth);
            return left ?? EvaluateNode(b.Right, param, candidate, depth);
        }

        var leftVal = EvaluateNode(b.Left, param, candidate, depth);
        var rightVal = EvaluateNode(b.Right, param, candidate, depth);

        if (b.Left is not ConstantExpression && (leftVal is null || rightVal is null))
        {
            return b.NodeType switch
            {
                ExpressionType.Equal => Equals(leftVal, rightVal),
                ExpressionType.NotEqual => !Equals(leftVal, rightVal),
                ExpressionType.GreaterThan or
                ExpressionType.GreaterThanOrEqual or
                ExpressionType.LessThan or
                ExpressionType.LessThanOrEqual => false,
                _ => throw new NotSupportedException($"Binary operator '{b.NodeType}' is not supported by the interpreted evaluator.")
            };
        }

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
        T candidate,
        int depth)
    {
        var test = EvaluateNode(cond.Test, param, candidate, depth);
        return test is true
            ? EvaluateNode(cond.IfTrue, param, candidate, depth)
            : EvaluateNode(cond.IfFalse, param, candidate, depth);
    }

    private static object? EvaluateTypeBinary<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        TypeBinaryExpression tb,
        ParameterExpression param,
        T candidate,
        int depth)
    {
        if (tb.NodeType == ExpressionType.TypeIs)
        {
            var operand = EvaluateNode(tb.Expression, param, candidate, depth);
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
        T candidate,
        int depth)
    {
        var operand = EvaluateNode(u.Operand, param, candidate, depth);
        return u.NodeType switch
        {
            ExpressionType.Not => operand is false || operand is null,
            ExpressionType.Convert or ExpressionType.ConvertChecked => ConvertOperand(operand, u.Type),
            _ => throw new NotSupportedException($"Unary operator '{u.NodeType}' is not supported by the interpreted evaluator.")
        };
    }

    private static object? ConvertOperand(object? operand, Type targetType)
    {
        if (operand is null)
            return null;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsInstanceOfType(operand))
            return operand;

        if (underlying.IsEnum)
            return Enum.ToObject(underlying, operand);

        return Convert.ChangeType(operand, underlying, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static object? EvaluateMethodCall<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(
        MethodCallExpression mc,
        ParameterExpression param,
        T candidate,
        int depth)
    {
        EnsureSafeMethod(mc.Method);

        var instance = mc.Object is null ? null : EvaluateNode(mc.Object, param, candidate, depth);
        var args = new object?[mc.Arguments.Count];
        for (var i = 0; i < mc.Arguments.Count; i++)
            args[i] = EvaluateNode(mc.Arguments[i], param, candidate, depth);

        return mc.Method.Invoke(instance, args);
    }

    private static void EnsureSafeMethod(System.Reflection.MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        // Stryker disable once Statement : Defensive null guard for methods without declaring type
        if (declaringType is null) return;

        // Stryker disable once String : Fallback for global namespace methods
        var ns = declaringType.Namespace ?? string.Empty;
        if (ns.StartsWith("System.Diagnostics", StringComparison.Ordinal) ||
            ns.StartsWith("System.IO", StringComparison.Ordinal) ||
            ns.StartsWith("System.Reflection", StringComparison.Ordinal) ||
            declaringType == typeof(Environment))
        {
            throw new InvalidOperationException($"Method '{method.Name}' on type '{declaringType.FullName}' is not permitted in interpreted specification evaluation for security reasons.");
        }
    }

    private static int CompareValues(object? left, object? right)
    {
        if (left is IComparable comparable)
        {
            if (right is not null && right.GetType() != left.GetType())
            {
                try
                {
                    right = Convert.ChangeType(right, left.GetType(), System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (InvalidCastException)
                {
                    // Fall back to direct comparison
                }
                catch (FormatException)
                {
                    // Fall back to direct comparison
                }
                catch (OverflowException)
                {
                    // Fall back to direct comparison
                }
            }
            return comparable.CompareTo(right);
        }

        // Stryker disable once all : Non-IComparable comparison is guarded by Expression tree construction
        throw new NotSupportedException(
            $"Cannot compare values of type '{left?.GetType().Name ?? "null"}'. " +
            "Type must implement IComparable.");
    }
}



