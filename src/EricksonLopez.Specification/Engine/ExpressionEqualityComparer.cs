// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace EricksonLopez.Specification;

/// <summary>
/// Provides a deep structural equality comparison for expression trees.
/// </summary>
public sealed class ExpressionEqualityComparer : IEqualityComparer<Expression?>
{
    /// <summary>
    /// Gets the default singleton instance of the comparer.
    /// </summary>
    public static readonly ExpressionEqualityComparer Default = new();

    private ExpressionEqualityComparer() { }

    private const int MaxDepth = 512;

    [ThreadStatic]
    private static int s_depth;

    /// <inheritdoc/>
    [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "Guards against StackOverflowException DoS attack on deeply nested ASTs.")]
    public bool Equals(Expression? x, Expression? y)
    {
        if (s_depth > MaxDepth)
            throw new InvalidOperationException($"Expression tree exceeds maximum supported equality depth of {MaxDepth}.");

        s_depth++;
        try
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            if (x.NodeType != y.NodeType || x.Type != y.Type) return false;

            return x switch
            {
                BinaryExpression b => EqualsBinary(b, (BinaryExpression)y),
                UnaryExpression u => EqualsUnary(u, (UnaryExpression)y),
                MethodCallExpression m => EqualsMethodCall(m, (MethodCallExpression)y),
                MemberExpression m => EqualsMember(m, (MemberExpression)y),
                ConstantExpression c => EqualsConstant(c, (ConstantExpression)y),
                ParameterExpression p => EqualsParameter(p, (ParameterExpression)y),
                LambdaExpression l => EqualsLambda(l, (LambdaExpression)y),
                ConditionalExpression c => EqualsConditional(c, (ConditionalExpression)y),
                InvocationExpression i => EqualsInvocation(i, (InvocationExpression)y),
                NewExpression n => EqualsNew(n, (NewExpression)y),
                NewArrayExpression n => EqualsNewArray(n, (NewArrayExpression)y),
                MemberInitExpression m => EqualsMemberInit(m, (MemberInitExpression)y),
                ListInitExpression l => EqualsListInit(l, (ListInitExpression)y),
                TypeBinaryExpression t => EqualsTypeBinary(t, (TypeBinaryExpression)y),
                _ => false
            };
        }
        finally
        {
            s_depth--;
        }
    }

    /// <inheritdoc/>
    public int GetHashCode(Expression? obj)
    {
        if (obj is null) return 0;
        return ExpressionHasher.ComputeHash(obj);
    }

    private bool EqualsBinary(BinaryExpression x, BinaryExpression y) =>
        x.Method == y.Method &&
        x.IsLifted == y.IsLifted &&
        x.IsLiftedToNull == y.IsLiftedToNull &&
        Equals(x.Left, y.Left) &&
        Equals(x.Right, y.Right) &&
        Equals(x.Conversion, y.Conversion);

    private bool EqualsUnary(UnaryExpression x, UnaryExpression y) =>
        x.Method == y.Method &&
        x.IsLifted == y.IsLifted &&
        x.IsLiftedToNull == y.IsLiftedToNull &&
        Equals(x.Operand, y.Operand);

    private bool EqualsMethodCall(MethodCallExpression x, MethodCallExpression y) =>
        x.Method == y.Method &&
        Equals(x.Object, y.Object) &&
        EqualsReadOnlyCollection(x.Arguments, y.Arguments);

    private bool EqualsMember(MemberExpression x, MemberExpression y)
    {
        if (x.Member != y.Member) return false;

        // Stryker disable once Logical : Provably equivalent; x.Member == y.Member implies identical static/instance kind
        if (x.Expression is null && y.Expression is null)
        {
            var valX = GetStaticMemberValue(x.Member);
            var valY = GetStaticMemberValue(y.Member);
            return Equals(valX, valY);
        }

        return Equals(x.Expression, y.Expression);
    }

    private static object? GetStaticMemberValue(MemberInfo member)
    {
        if (member is PropertyInfo pi && (pi.GetMethod?.IsStatic ?? false))
            return pi.GetValue(null);
        if (member is FieldInfo fi && fi.IsStatic)
            return fi.GetValue(null);
        return null;
    }

    private static bool EqualsConstant(ConstantExpression x, ConstantExpression y) =>
        Equals(x.Value, y.Value);

    private static bool EqualsParameter(ParameterExpression x, ParameterExpression y) =>
        x.Type == y.Type; // Parameters are structurally equal if types match.

    private bool EqualsLambda(LambdaExpression x, LambdaExpression y) =>
        EqualsReadOnlyCollection(x.Parameters, y.Parameters) &&
        Equals(x.Body, y.Body);

    private bool EqualsConditional(ConditionalExpression x, ConditionalExpression y) =>
        Equals(x.Test, y.Test) &&
        Equals(x.IfTrue, y.IfTrue) &&
        Equals(x.IfFalse, y.IfFalse);

    private bool EqualsInvocation(InvocationExpression x, InvocationExpression y) =>
        Equals(x.Expression, y.Expression) &&
        EqualsReadOnlyCollection(x.Arguments, y.Arguments);

    private bool EqualsNew(NewExpression x, NewExpression y) =>
        x.Constructor == y.Constructor &&
        EqualsReadOnlyCollection(x.Arguments, y.Arguments) &&
        EqualsReadOnlyCollection(x.Members, y.Members);

    private bool EqualsNewArray(NewArrayExpression x, NewArrayExpression y) =>
        EqualsReadOnlyCollection(x.Expressions, y.Expressions);

    private bool EqualsMemberInit(MemberInitExpression x, MemberInitExpression y) =>
        EqualsNew(x.NewExpression, y.NewExpression) &&
        EqualsReadOnlyCollection(x.Bindings, y.Bindings, EqualsMemberBinding);

    private bool EqualsListInit(ListInitExpression x, ListInitExpression y) =>
        EqualsNew(x.NewExpression, y.NewExpression) &&
        EqualsReadOnlyCollection(x.Initializers, y.Initializers, EqualsElementInit);

    private bool EqualsTypeBinary(TypeBinaryExpression x, TypeBinaryExpression y) =>
        x.TypeOperand == y.TypeOperand &&
        Equals(x.Expression, y.Expression);

    private bool EqualsMemberBinding(MemberBinding x, MemberBinding y)
    {
        if (x.BindingType != y.BindingType) return false;
        if (x.Member != y.Member) return false;

        return x switch
        {
            MemberAssignment ma => Equals(ma.Expression, ((MemberAssignment)y).Expression),
            MemberMemberBinding mb => EqualsReadOnlyCollection(mb.Bindings, ((MemberMemberBinding)y).Bindings, EqualsMemberBinding),
            MemberListBinding mlb => EqualsReadOnlyCollection(mlb.Initializers, ((MemberListBinding)y).Initializers, EqualsElementInit),
            // Stryker disable once Boolean : Exhaustive on BCL MemberBinding hierarchy
            _ => false
        };
    }

    private bool EqualsElementInit(ElementInit x, ElementInit y) =>
        x.AddMethod == y.AddMethod &&
        EqualsReadOnlyCollection(x.Arguments, y.Arguments);

    private bool EqualsReadOnlyCollection<T>(IReadOnlyList<T>? x, IReadOnlyList<T>? y) where T : Expression
    {
        if (ReferenceEquals(x, y)) return true;
        // Stryker disable once Logical, Boolean : Defensive null guard; BCL expression properties return non-null collections
        if (x is null || y is null) return false;
        if (x.Count != y.Count) return false;

        for (int i = 0; i < x.Count; i++)
        {
            if (!Equals(x[i], y[i])) return false;
        }

        return true;
    }

    private static bool EqualsReadOnlyCollection<T>(IReadOnlyList<T>? x, IReadOnlyList<T>? y, Func<T, T, bool> equals)
    {
        // Stryker disable once Boolean : Defensive ReferenceEquals check; BCL always allocates distinct ReadOnlyCollection wrappers for expression tree bindings
        if (ReferenceEquals(x, y)) return true;
        // Stryker disable once Logical, Boolean : Defensive null guard; BCL expression properties return non-null collections
        if (x is null || y is null) return false;
        if (x.Count != y.Count) return false;

        for (int i = 0; i < x.Count; i++)
        {
            if (!equals(x[i], y[i])) return false;
        }

        return true;
    }

    private static bool EqualsReadOnlyCollection(System.Collections.ObjectModel.ReadOnlyCollection<MemberInfo>? x, System.Collections.ObjectModel.ReadOnlyCollection<MemberInfo>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        // Stryker disable once Boolean : Defensive count check; BCL NewExpression constructor argument validation guarantees member count equals constructor parameter count
        if (x.Count != y.Count) return false;

        for (int i = 0; i < x.Count; i++)
        {
            if (x[i] != y[i]) return false;
        }

        return true;
    }
}
