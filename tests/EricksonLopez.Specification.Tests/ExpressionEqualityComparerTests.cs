// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

public class ExpressionEqualityComparerTests
{
    private readonly ExpressionEqualityComparer _comparer = ExpressionEqualityComparer.Default;

    [Fact]
    public void Equals_WhenSameReference_ReturnsTrue()
    {
        Expression<Func<string, bool>> expr = x => x == "test";
        _comparer.Equals(expr, expr).Should().BeTrue();
    }

    [Fact]
    public void Equals_WhenOneIsNull_ReturnsFalse()
    {
        Expression<Func<string, bool>> expr = x => x == "test";
        _comparer.Equals(expr, null).Should().BeFalse();
        _comparer.Equals(null, expr).Should().BeFalse();
    }

    [Fact]
    public void Equals_WhenStructurallyIdenticalLambda_ReturnsTrue()
    {
        Expression<Func<int, bool>> expr1 = x => x > 5;
        Expression<Func<int, bool>> expr2 = y => y > 5;

        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WhenDifferentConstants_ReturnsFalse()
    {
        Expression<Func<int, bool>> expr1 = x => x > 5;
        Expression<Func<int, bool>> expr2 = x => x > 10;

        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WhenDifferentOperators_ReturnsFalse()
    {
        Expression<Func<int, bool>> expr1 = x => x > 5;
        Expression<Func<int, bool>> expr2 = x => x < 5;

        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_MethodCalls_StructurallyIdentical_ReturnsTrue()
    {
        Expression<Func<string, bool>> expr1 = x => x.StartsWith('a');
        Expression<Func<string, bool>> expr2 = y => y.StartsWith('a');

        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_MethodCalls_DifferentArguments_ReturnsFalse()
    {
        Expression<Func<string, bool>> expr1 = x => x.StartsWith('a');
        Expression<Func<string, bool>> expr2 = x => x.StartsWith('b');

        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_MethodCalls_DifferentMethod_ReturnsFalse()
    {
        Expression<Func<string, bool>> expr1 = x => x.StartsWith('a');
        Expression<Func<string, bool>> expr2 = x => x.EndsWith('a');

        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberAccess_StructurallyIdentical_ReturnsTrue()
    {
        Expression<Func<DateTime, int>> expr1 = d => d.Year;
        Expression<Func<DateTime, int>> expr2 = d => d.Year;

        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_MemberAccess_DifferentMember_ReturnsFalse()
    {
        Expression<Func<DateTime, int>> expr1 = d => d.Year;
        Expression<Func<DateTime, int>> expr2 = d => d.Month;

        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_NewExpression_Identical_ReturnsTrue()
    {
        Expression<Func<DateTime>> expr1 = () => new DateTime(2025, 1, 1);
        Expression<Func<DateTime>> expr2 = () => new DateTime(2025, 1, 1);

        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_NewExpression_DifferentArgs_ReturnsFalse()
    {
        Expression<Func<DateTime>> expr1 = () => new DateTime(2025, 1, 1);
        Expression<Func<DateTime>> expr2 = () => new DateTime(2025, 2, 1);

        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_SameStructure_ReturnsSameHash()
    {
        Expression<Func<int, bool>> expr1 = x => x > 5;
        Expression<Func<int, bool>> expr2 = y => y > 5;

        _comparer.GetHashCode(expr1).Should().Be(_comparer.GetHashCode(expr2));
    }

    [Fact]
    public void GetHashCode_DifferentStructure_ReturnsDifferentHash()
    {
        Expression<Func<int, bool>> expr1 = x => x > 5;
        Expression<Func<int, bool>> expr2 = x => x > 10;

        // Note: Due to the nature of hashing, this is not strictly guaranteed not to collide,
        // but for these simple cases it should be different.
        _comparer.GetHashCode(expr1).Should().NotBe(_comparer.GetHashCode(expr2));
    }

    [Fact]
    public void Equals_UnaryExpression_Identical_ReturnsTrue()
    {
        Expression<Func<bool, bool>> expr1 = x => !x;
        Expression<Func<bool, bool>> expr2 = y => !y;
        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_UnaryExpression_Different_ReturnsFalse()
    {
        Expression<Func<int, int>> expr1 = x => -x;
        Expression<Func<int, int>> expr2 = x => +x;
        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ConditionalExpression_Identical_ReturnsTrue()
    {
        Expression<Func<int, string>> expr1 = x => x > 5 ? "Yes" : "No";
        Expression<Func<int, string>> expr2 = y => y > 5 ? "Yes" : "No";
        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ConditionalExpression_Different_ReturnsFalse()
    {
        Expression<Func<int, string>> expr1 = x => x > 5 ? "Yes" : "No";
        Expression<Func<int, string>> expr2 = x => x > 5 ? "Yes" : "Maybe"; // IfFalse differs
        _comparer.Equals(expr1, expr2).Should().BeFalse();

        Expression<Func<int, string>> expr3 = x => x > 5 ? "Yeah" : "No"; // IfTrue differs
        _comparer.Equals(expr1, expr3).Should().BeFalse();

        Expression<Func<int, string>> expr4 = x => x > 10 ? "Yes" : "No"; // Test differs
        _comparer.Equals(expr1, expr4).Should().BeFalse();
    }

    [Fact]
    public void Equals_InvocationExpression_Identical_ReturnsTrue()
    {
        Expression<Func<int, bool>> inner = x => x > 5;
        var expr1 = Expression.Invoke(inner, Expression.Constant(10));
        var expr2 = Expression.Invoke(inner, Expression.Constant(10));
        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_InvocationExpression_Different_ReturnsFalse()
    {
        Expression<Func<int, bool>> inner = x => x > 5;
        var expr1 = Expression.Invoke(inner, Expression.Constant(10));
        var expr2 = Expression.Invoke(inner, Expression.Constant(20));
        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_NewArrayExpression_Identical_ReturnsTrue()
    {
        Expression<Func<int[]>> expr1 = () => new[] { 1, 2, 3 };
        Expression<Func<int[]>> expr2 = () => new[] { 1, 2, 3 };
        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_NewArrayExpression_Different_ReturnsFalse()
    {
        Expression<Func<int[]>> expr1 = () => new[] { 1, 2, 3 };
        Expression<Func<int[]>> expr2 = () => new[] { 1, 2, 4 };
        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberInitExpression_Identical_ReturnsTrue()
    {
        Expression<Func<ExpressionEqualityComparerTests_CustomType>> expr1 = () => new ExpressionEqualityComparerTests_CustomType { Value = 1 };
        Expression<Func<ExpressionEqualityComparerTests_CustomType>> expr2 = () => new ExpressionEqualityComparerTests_CustomType { Value = 1 };
        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_MemberInitExpression_Different_ReturnsFalse()
    {
        Expression<Func<ExpressionEqualityComparerTests_CustomType>> expr1 = () => new ExpressionEqualityComparerTests_CustomType { Value = 1 };
        Expression<Func<ExpressionEqualityComparerTests_CustomType>> expr2 = () => new ExpressionEqualityComparerTests_CustomType { Value = 2 }; // Value differs
        _comparer.Equals(expr1, expr2).Should().BeFalse();

        Expression<Func<ExpressionEqualityComparerTests_CustomType>> expr3 = () => new ExpressionEqualityComparerTests_CustomType { OtherValue = 1 }; // Member differs
        _comparer.Equals(expr1, expr3).Should().BeFalse();
    }

    [Fact]
    public void Equals_ListInitExpression_Identical_ReturnsTrue()
    {
        Expression<Func<List<int>>> expr1 = () => new List<int> { 1, 2, 3 };
        Expression<Func<List<int>>> expr2 = () => new List<int> { 1, 2, 3 };
        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ListInitExpression_Different_ReturnsFalse()
    {
        Expression<Func<List<int>>> expr1 = () => new List<int> { 1, 2, 3 };
        Expression<Func<List<int>>> expr2 = () => new List<int> { 1, 2, 4 };
        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_TypeBinaryExpression_Identical_ReturnsTrue()
    {
        Expression<Func<object, bool>> expr1 = x => x is string;
        Expression<Func<object, bool>> expr2 = y => y is string;
        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_TypeBinaryExpression_Different_ReturnsFalse()
    {
        Expression<Func<object, bool>> expr1 = x => x is string;
        Expression<Func<object, bool>> expr2 = x => x is int;
        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_NullExpression_ReturnsZero()
    {
        _comparer.GetHashCode(null).Should().Be(0);
    }

    [Fact]
    public void Equals_UnsupportedExpression_ReturnsFalse()
    {
        var expr1 = Expression.Empty();
        var expr2 = Expression.Empty();
        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_BinaryExpression_DifferentMethod_ReturnsFalse()
    {
        var left = Expression.Constant(1);
        var right = Expression.Constant(2);
        var addMethod = typeof(ExpressionEqualityComparerTests).GetMethod(nameof(DummyAdd));
        var addMethod2 = typeof(ExpressionEqualityComparerTests).GetMethod(nameof(DummyAdd2));
        var bin1 = Expression.Add(left, right, addMethod);
        var bin2 = Expression.Add(left, right, addMethod2);

        _comparer.Equals(bin1, bin2).Should().BeFalse();
    }

    [Fact]
    public void Equals_UnaryExpression_DifferentMethod_ReturnsFalse()
    {
        var operand = Expression.Constant(1);
        var negMethod = typeof(ExpressionEqualityComparerTests).GetMethod(nameof(DummyNeg));
        var negMethod2 = typeof(ExpressionEqualityComparerTests).GetMethod(nameof(DummyNeg2));
        var un1 = Expression.Negate(operand, negMethod);
        var un2 = Expression.Negate(operand, negMethod2);

        _comparer.Equals(un1, un2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ReadOnlyCollection_CollectionsAreNull()
    {
        // One is null
        var expr1 = Expression.Lambda(Expression.Constant(1));
        var expr2 = Expression.Lambda(Expression.Constant(1), Expression.Parameter(typeof(int)));
        _comparer.Equals(expr1, expr2).Should().BeFalse();
    }

    [Fact]
    public void Equals_NewExpression_WithMembers_Identical_ReturnsTrue()
    {
        var constructor = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(new[] { typeof(int) })!;
        var member1 = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Value))!;
        var expr1 = Expression.New(constructor, new Expression[] { Expression.Constant(1) }, new System.Reflection.MemberInfo[] { member1 });
        var expr2 = Expression.New(constructor, new Expression[] { Expression.Constant(1) }, new System.Reflection.MemberInfo[] { member1 });

        _comparer.Equals(expr1, expr2).Should().BeTrue();
    }

    [Fact]
    public void Equals_NewExpression_WithMembers_Different_ReturnsFalse()
    {
        var constructor = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(new[] { typeof(int) })!;
        var constructor2 = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(new[] { typeof(int), typeof(int) })!;
        var member1 = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Value))!;
        var member2 = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.OtherValue))!;

        var expr1 = Expression.New(constructor, new Expression[] { Expression.Constant(1) }, new System.Reflection.MemberInfo[] { member1 });
        var expr2 = Expression.New(constructor, new Expression[] { Expression.Constant(1) }, new System.Reflection.MemberInfo[] { member2 });
        var expr3 = Expression.New(constructor2, new Expression[] { Expression.Constant(1), Expression.Constant(2) }, new System.Reflection.MemberInfo[] { member1, member2 });

        _comparer.Equals(expr1, expr2).Should().BeFalse();
        _comparer.Equals(expr1, expr3).Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberMemberBinding_IdenticalAndDifferent()
    {
        var constructor = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(Type.EmptyTypes)!;
        var memberProperty = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Child))!;
        var valueProperty = typeof(ExpressionEqualityComparerTests_ChildType).GetProperty(nameof(ExpressionEqualityComparerTests_ChildType.Value))!;

        var binding1 = Expression.MemberBind(memberProperty, Expression.Bind(valueProperty, Expression.Constant(1)));
        var expr1 = Expression.MemberInit(Expression.New(constructor), binding1);

        var binding2 = Expression.MemberBind(memberProperty, Expression.Bind(valueProperty, Expression.Constant(1)));
        var expr2 = Expression.MemberInit(Expression.New(constructor), binding2);

        var binding3 = Expression.MemberBind(memberProperty, Expression.Bind(valueProperty, Expression.Constant(2)));
        var expr3 = Expression.MemberInit(Expression.New(constructor), binding3);

        _comparer.Equals(expr1, expr2).Should().BeTrue();
        _comparer.Equals(expr1, expr3).Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberListBinding_IdenticalAndDifferent()
    {
        var constructor = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(Type.EmptyTypes)!;
        var listProperty = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.List))!;
        var addMethod = typeof(List<int>).GetMethod("Add")!;

        var binding1 = Expression.ListBind(listProperty, Expression.ElementInit(addMethod, Expression.Constant(1)));
        var expr1 = Expression.MemberInit(Expression.New(constructor), binding1);

        var binding2 = Expression.ListBind(listProperty, Expression.ElementInit(addMethod, Expression.Constant(1)));
        var expr2 = Expression.MemberInit(Expression.New(constructor), binding2);

        var binding3 = Expression.ListBind(listProperty, Expression.ElementInit(addMethod, Expression.Constant(2)));
        var expr3 = Expression.MemberInit(Expression.New(constructor), binding3);

        _comparer.Equals(expr1, expr2).Should().BeTrue();
        _comparer.Equals(expr1, expr3).Should().BeFalse();
    }

    [Fact]
    public void Equals_NewExpression_WithMembers_IdenticalAndDifferent()
    {
        var ctor2 = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(new[] { typeof(int), typeof(int) })!;
        var ctor1 = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(new[] { typeof(int) })!;
        var propVal = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Value))!;
        var propOther = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.OtherValue))!;

        var expr1 = Expression.New(ctor2, new Expression[] { Expression.Constant(1), Expression.Constant(2) }, propVal, propOther);
        var expr2 = Expression.New(ctor2, new Expression[] { Expression.Constant(1), Expression.Constant(2) }, propVal, propOther);
        var expr3 = Expression.New(ctor2, new Expression[] { Expression.Constant(1), Expression.Constant(2) }, propOther, propVal);
        var expr4 = Expression.New(ctor1, new Expression[] { Expression.Constant(1) }, propVal);

        _comparer.Equals(expr1, expr2).Should().BeTrue();
        _comparer.Equals(expr1, expr3).Should().BeFalse();
        _comparer.Equals(expr1, expr4).Should().BeFalse();
        _comparer.Equals(expr4, expr1).Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberInit_DifferentBindingsCount_ReturnsFalse()
    {
        var constructor = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(Type.EmptyTypes)!;
        var valProp = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Value))!;
        var otherProp = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.OtherValue))!;

        var b1 = Expression.Bind(valProp, Expression.Constant(1));
        var b2 = Expression.Bind(otherProp, Expression.Constant(2));

        var expr1 = Expression.MemberInit(Expression.New(constructor), b1);
        var expr2 = Expression.MemberInit(Expression.New(constructor), b1, b2);

        _comparer.Equals(expr1, expr2).Should().BeFalse();
        _comparer.Equals(expr2, expr1).Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberListBinding_DifferentInitializersCount_ReturnsFalse()
    {
        var constructor = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(Type.EmptyTypes)!;
        var listProperty = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.List))!;
        var addMethod = typeof(List<int>).GetMethod("Add")!;

        var init1 = Expression.ElementInit(addMethod, Expression.Constant(1));
        var init2 = Expression.ElementInit(addMethod, Expression.Constant(2));

        var binding1 = Expression.ListBind(listProperty, init1);
        var binding2 = Expression.ListBind(listProperty, init1, init2);

        var expr1 = Expression.MemberInit(Expression.New(constructor), binding1);
        var expr2 = Expression.MemberInit(Expression.New(constructor), binding2);

        _comparer.Equals(expr1, expr2).Should().BeFalse();
        _comparer.Equals(expr2, expr1).Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberMemberBinding_DifferentBindingCounts_ReturnsFalse()
    {
        var constructor = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(Type.EmptyTypes)!;
        var childProp = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Child))!;
        var childValProp = typeof(ExpressionEqualityComparerTests_ChildType).GetProperty(nameof(ExpressionEqualityComparerTests_ChildType.Value))!;

        var b1 = Expression.Bind(childValProp, Expression.Constant(10));
        var mb1 = Expression.MemberBind(childProp, b1);
        var mbEmpty = Expression.MemberBind(childProp);

        var expr1 = Expression.MemberInit(Expression.New(constructor), mb1);
        var expr2 = Expression.MemberInit(Expression.New(constructor), mbEmpty);

        _comparer.Equals(expr1, expr2).Should().BeFalse();
        _comparer.Equals(expr2, expr1).Should().BeFalse();
    }

    [Fact]
    public void Equals_NewExpression_NullMembersVsNonNullMembers_ReturnsFalse()
    {
        var ctor = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(new[] { typeof(int) })!;
        var propVal = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Value))!;

        var withMembers = Expression.New(ctor, new Expression[] { Expression.Constant(1) }, propVal);
        var withoutMembers = Expression.New(ctor, new Expression[] { Expression.Constant(1) });

        _comparer.Equals(withMembers, withoutMembers).Should().BeFalse();
        _comparer.Equals(withoutMembers, withMembers).Should().BeFalse();
        _comparer.Equals(withoutMembers, withoutMembers).Should().BeTrue();
    }

    [Fact]
    public void Equals_ListInit_DifferentInitializersCount_ReturnsFalse()
    {
        var constructor = typeof(List<int>).GetConstructor(Type.EmptyTypes)!;
        var addMethod = typeof(List<int>).GetMethod("Add")!;

        var init1 = Expression.ElementInit(addMethod, Expression.Constant(1));
        var init2 = Expression.ElementInit(addMethod, Expression.Constant(2));

        var list1 = Expression.ListInit(Expression.New(constructor), init1);
        var list2 = Expression.ListInit(Expression.New(constructor), init1, init2);

        _comparer.Equals(list1, list2).Should().BeFalse();
        _comparer.Equals(list2, list1).Should().BeFalse();
    }

    [Fact]
    public void Equals_MethodCall_DifferentArgumentCounts_ReturnsFalse()
    {
        var method1 = typeof(ExpressionEqualityComparerTests).GetMethod(nameof(DummyNeg))!;
        var method2 = typeof(ExpressionEqualityComparerTests).GetMethod(nameof(DummyAdd))!;

        var call1 = Expression.Call(method1, Expression.Constant(1));
        var call2 = Expression.Call(method2, Expression.Constant(1), Expression.Constant(2));

        _comparer.Equals(call1, call2).Should().BeFalse();
        _comparer.Equals(call2, call1).Should().BeFalse();
    }

    [Fact]
    public void Equals_NewArrayInit_DifferentElementCounts_ReturnsFalse()
    {
        var array1 = Expression.NewArrayInit(typeof(int), Expression.Constant(1));
        var array2 = Expression.NewArrayInit(typeof(int), Expression.Constant(1), Expression.Constant(2));

        _comparer.Equals(array1, array2).Should().BeFalse();
        _comparer.Equals(array2, array1).Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberInitAndListInit_SameInstance_ReturnsTrue()
    {
        var constructor = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(Type.EmptyTypes)!;
        var valProp = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Value))!;
        var b1 = Expression.Bind(valProp, Expression.Constant(1));
        var bindings = new MemberBinding[] { b1 };
        var memberInit1 = Expression.MemberInit(Expression.New(constructor), bindings);
        var memberInit2 = Expression.MemberInit(Expression.New(constructor), bindings);

        // Different MemberInit instances, but sharing the exact same bindings collection reference
        _comparer.Equals(memberInit1, memberInit2).Should().BeTrue();

        var listConstructor = typeof(List<int>).GetConstructor(Type.EmptyTypes)!;
        var addMethod = typeof(List<int>).GetMethod("Add")!;
        var init1 = Expression.ElementInit(addMethod, Expression.Constant(1));
        var inits = new ElementInit[] { init1 };
        var listInit1 = Expression.ListInit(Expression.New(listConstructor), inits);
        var listInit2 = Expression.ListInit(Expression.New(listConstructor), inits);

        // Different ListInit instances, but sharing the exact same initializers collection reference
        _comparer.Equals(listInit1, listInit2).Should().BeTrue();
    }


    [Fact]
    public void Equals_Lambda_DifferentBody_ReturnsFalse()
    {
        Expression<Func<int, int>> l1 = x => x + 1;
        Expression<Func<int, int>> l2 = x => x + 2;
        _comparer.Equals(l1, l2).Should().BeFalse();
    }

    [Fact]
    public void Equals_Invocation_DifferentTargetExpression_ReturnsFalse()
    {
        Expression<Func<int, int>> f1 = x => x + 1;
        Expression<Func<int, int>> f2 = x => x + 2;
        var inv1 = Expression.Invoke(f1, Expression.Constant(5));
        var inv2 = Expression.Invoke(f2, Expression.Constant(5));
        _comparer.Equals(inv1, inv2).Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberInit_DifferentNewExpression_ReturnsFalse()
    {
        var ctor1 = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(Type.EmptyTypes)!;
        var ctor2 = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(new[] { typeof(int) })!;
        var prop = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Value))!;
        var b = Expression.Bind(prop, Expression.Constant(1));

        var m1 = Expression.MemberInit(Expression.New(ctor1), b);
        var m2 = Expression.MemberInit(Expression.New(ctor2, Expression.Constant(0)), b);
        _comparer.Equals(m1, m2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ListInit_DifferentNewExpression_ReturnsFalse()
    {
        var ctor = typeof(List<int>).GetConstructor(Type.EmptyTypes)!;
        var ctorCap = typeof(List<int>).GetConstructor(new[] { typeof(int) })!;
        var add = typeof(List<int>).GetMethod("Add")!;
        var init = Expression.ElementInit(add, Expression.Constant(1));

        var l1 = Expression.ListInit(Expression.New(ctor), init);
        var l2 = Expression.ListInit(Expression.New(ctorCap, Expression.Constant(10)), init);
        _comparer.Equals(l1, l2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ElementInit_DifferentAddMethod_ReturnsFalse()
    {
        var constructor = typeof(ExpressionEqualityComparerTests_CustomCollection).GetConstructor(Type.EmptyTypes)!;
        var add1 = typeof(ExpressionEqualityComparerTests_CustomCollection).GetMethod(nameof(ExpressionEqualityComparerTests_CustomCollection.Add), new[] { typeof(int) })!;
        var add2 = typeof(ExpressionEqualityComparerTests_CustomCollection).GetMethod(nameof(ExpressionEqualityComparerTests_CustomCollection.Add), new[] { typeof(string) })!;

        var init1 = Expression.ElementInit(add1, Expression.Constant(1));
        var init2 = Expression.ElementInit(add2, Expression.Constant("1"));

        var list1 = Expression.ListInit(Expression.New(constructor), init1);
        var list2 = Expression.ListInit(Expression.New(constructor), init2);

        _comparer.Equals(list1, list2).Should().BeFalse("different AddMethod on element init must return false");
    }

    [Fact]
    public void Equals_Lambda_DifferentParameterTypes_ReturnsFalse()
    {
        Expression<Func<int, int, bool>> l1 = (a, b) => a == b;
        Expression<Func<int, string, bool>> l2 = (a, b) => true;
        _comparer.Equals(l1, l2).Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberInit_DifferentMemberInBinding_ReturnsFalse()
    {
        var ctor = typeof(ExpressionEqualityComparerTests_CustomType).GetConstructor(Type.EmptyTypes)!;
        var p1 = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Value))!;
        var p2 = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.OtherValue))!;
        var b1 = Expression.Bind(p1, Expression.Constant(1));
        var b2 = Expression.Bind(p2, Expression.Constant(1));

        var m1 = Expression.MemberInit(Expression.New(ctor), b1);
        var m2 = Expression.MemberInit(Expression.New(ctor), b2);
        _comparer.Equals(m1, m2).Should().BeFalse("different property bindings must compare not equal");
    }

    [Fact]
    public void EqualsMemberBinding_UnknownBindingHierarchy_ReturnsFalse()
    {
        var method = typeof(ExpressionEqualityComparer).GetMethod("EqualsMemberBinding", BindingFlags.NonPublic | BindingFlags.Instance);
        var ab = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Dyn"), System.Reflection.Emit.AssemblyBuilderAccess.Run);
        var mb = ab.DefineDynamicModule("DynMod");
        var tb = mb.DefineType("CustomMemberBinding", TypeAttributes.Public, typeof(MemberBinding));

        var baseCtor = typeof(MemberBinding).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
        var ctor = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { typeof(MemberBindingType), typeof(MemberInfo) });
        var il = ctor.GetILGenerator();
        il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
        il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
        il.Emit(System.Reflection.Emit.OpCodes.Ldarg_2);
        il.Emit(System.Reflection.Emit.OpCodes.Call, baseCtor);
        il.Emit(System.Reflection.Emit.OpCodes.Ret);

        var dynamicType = tb.CreateType();
        var dynObj = (MemberBinding)Activator.CreateInstance(dynamicType!, MemberBindingType.Assignment, typeof(Customer).GetProperty("Id")!)!;

        // When binding types match but object is an unknown external subtype
        var result = (bool)method!.Invoke(ExpressionEqualityComparer.Default, new object[] { dynObj, dynObj })!;
        result.Should().BeFalse();
    }

    [Fact]
    public void EqualsReadOnlyCollection_ReflectiveTests_NullAndCountMismatches()
    {
        var methods = typeof(ExpressionEqualityComparer).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

        // 1. Generic overload for Expression
        var exprListMethod = methods.First(m => m.Name == "EqualsReadOnlyCollection" && m.IsGenericMethod && m.GetParameters().Length == 2);
        var genericExpr = exprListMethod.MakeGenericMethod(typeof(Expression));

        var list1 = new Expression[] { Expression.Constant(1) };
        var list2 = new Expression[] { Expression.Constant(1) };
        var listDifferentCount = new Expression[] { Expression.Constant(1), Expression.Constant(2) };

        // ReferenceEquals
        ((bool)genericExpr.Invoke(ExpressionEqualityComparer.Default, new object[] { list1, list1 })!).Should().BeTrue();
        // Null checks
        ((bool)genericExpr.Invoke(ExpressionEqualityComparer.Default, new object[] { null!, list1 })!).Should().BeFalse();
        ((bool)genericExpr.Invoke(ExpressionEqualityComparer.Default, new object[] { list1, null! })!).Should().BeFalse();
        // Count mismatch
        ((bool)genericExpr.Invoke(ExpressionEqualityComparer.Default, new object[] { list1, listDifferentCount })!).Should().BeFalse();

        // 2. Overload with Func delegate
        var funcListMethod = methods.First(m => m.Name == "EqualsReadOnlyCollection" && m.IsGenericMethod && m.GetParameters().Length == 3);
        var genericFunc = funcListMethod.MakeGenericMethod(typeof(string));
        var str1 = new[] { "a" };
        var str2 = new[] { "a" };
        var strDiffCount = new[] { "a", "b" };
        Func<string, string, bool> eqFunc = (x, y) => x == y;

        ((bool)genericFunc.Invoke(null, new object[] { str1, str1, eqFunc })!).Should().BeTrue();
        ((bool)genericFunc.Invoke(null, new object[] { null!, str1, eqFunc })!).Should().BeFalse();
        ((bool)genericFunc.Invoke(null, new object[] { str1, null!, eqFunc })!).Should().BeFalse();
        ((bool)genericFunc.Invoke(null, new object[] { str1, strDiffCount, eqFunc })!).Should().BeFalse();

        // 3. MemberInfo overload
        var memberListMethod = methods.First(m => m.Name == "EqualsReadOnlyCollection" && !m.IsGenericMethod && m.GetParameters()[0].ParameterType.Name.StartsWith("ReadOnlyCollection"));
        var mem1 = new System.Collections.ObjectModel.ReadOnlyCollection<MemberInfo>(new MemberInfo[] { typeof(Customer).GetProperty("Id")! });
        var mem2 = new System.Collections.ObjectModel.ReadOnlyCollection<MemberInfo>(new MemberInfo[] { typeof(Customer).GetProperty("Id")!, typeof(Customer).GetProperty("Name")! });

        ((bool)memberListMethod.Invoke(null, new object[] { mem1, mem1 })!).Should().BeTrue();
        ((bool)memberListMethod.Invoke(null, new object[] { null!, mem1 })!).Should().BeFalse();
        ((bool)memberListMethod.Invoke(null, new object[] { mem1, null! })!).Should().BeFalse();
        ((bool)memberListMethod.Invoke(null, new object[] { mem1, mem2 })!).Should().BeFalse();
    }

    [Fact]
    public void EqualsLambda_DifferentParameterCounts_ReturnsFalse()
    {
        var equalsLambdaMethod = typeof(ExpressionEqualityComparer).GetMethod("EqualsLambda", BindingFlags.NonPublic | BindingFlags.Instance);
        var p1 = Expression.Parameter(typeof(int), "x");
        var p2 = Expression.Parameter(typeof(int), "y");
        var l1 = Expression.Lambda(Expression.Constant(true), p1);
        var l2 = Expression.Lambda(Expression.Constant(true), p1, p2);

        var result = (bool)equalsLambdaMethod!.Invoke(ExpressionEqualityComparer.Default, new object[] { l1, l2 })!;
        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_MemberInit_DifferentBindings_ReturnsFalse()
    {
        var newObj = Expression.New(typeof(ExpressionEqualityComparerTests_CustomType));
        var valProp = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.Value))!;
        var otherProp = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.OtherValue))!;
        var listProp = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.List))!;
        var addMethod = typeof(List<int>).GetMethod("Add", new[] { typeof(int) })!;

        // Different members
        var bVal = Expression.Bind(valProp, Expression.Constant(1));
        var bOther = Expression.Bind(otherProp, Expression.Constant(1));
        var init1 = Expression.MemberInit(newObj, bVal);
        var init2 = Expression.MemberInit(newObj, bOther);
        _comparer.Equals(init1, init2).Should().BeFalse();

        // Different binding types (Assignment vs ListBinding)
        var bList = Expression.ListBind(listProp, Expression.ElementInit(addMethod, Expression.Constant(5)));
        var bListAssign = Expression.Bind(listProp, Expression.Constant(new List<int>()));
        var initList1 = Expression.MemberInit(newObj, bList);
        var initList2 = Expression.MemberInit(newObj, bListAssign);
        _comparer.Equals(initList1, initList2).Should().BeFalse();
    }

    public static int DummyAdd(int a, int b) => a + b;
    public static int DummyAdd2(int a, int b) => a + b;
    public static int DummyNeg(int a) => -a;
    public static int DummyNeg2(int a) => -a;
}

public class ExpressionEqualityComparerTests_CustomCollection : System.Collections.IEnumerable
{
    public System.Collections.IEnumerator GetEnumerator() => Array.Empty<int>().GetEnumerator();
    public void Add(int x) { }
    public void Add(string x) { }
}

public class ExpressionEqualityComparerTests_CustomType
{
    public ExpressionEqualityComparerTests_CustomType() { }
    public ExpressionEqualityComparerTests_CustomType(int value) { Value = value; }
    public ExpressionEqualityComparerTests_CustomType(int value, int other) { Value = value; OtherValue = other; }

    public int Value { get; set; }
    public int OtherValue { get; set; }
    public ExpressionEqualityComparerTests_ChildType Child { get; set; } = new();
    public List<int> List { get; set; } = new();
}

public class ExpressionEqualityComparerTests_ChildType
{
    public int Value { get; set; }
}





