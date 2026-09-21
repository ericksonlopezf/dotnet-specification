// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Tests for <see cref="ExpressionHasher"/> — structural expression hashing and hash visitors.
/// </summary>
public sealed class ExpressionHasherTests
{
    [Fact]
    public void ComputeHash_SameStructure_ProducesSameHash()
    {
        Expression<Func<Customer, bool>> expr1 = c => c.IsActive;
        Expression<Func<Customer, bool>> expr2 = c => c.IsActive;

        var hash1 = ExpressionHasher.ComputeHash(expr1);
        var hash2 = ExpressionHasher.ComputeHash(expr2);

        hash1.Should().Be(hash2, "structurally identical expressions must produce the same hash");
    }

    [Fact]
    public void ComputeHash_DifferentStructure_ProducesDifferentHash()
    {
        Expression<Func<Customer, bool>> expr1 = c => c.IsActive;
        Expression<Func<Customer, bool>> expr2 = c => c.IsDeleted;

        var hash1 = ExpressionHasher.ComputeHash(expr1);
        var hash2 = ExpressionHasher.ComputeHash(expr2);

        hash1.Should().NotBe(hash2, "structurally different expressions should produce different hashes");
    }

    [Fact]
    public void ComputeHash_SameExpressionInstance_ProducesSameHash()
    {
        Expression<Func<Customer, bool>> expr = c => c.IsActive;

        var hash1 = ExpressionHasher.ComputeHash(expr);
        var hash2 = ExpressionHasher.ComputeHash(expr);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeHash_ComposedExpression_IsDeterministic()
    {
        var specA = new ActiveCustomerSpecification();
        var specB = new NotDeletedCustomerSpecification();

        var expr1 = ExpressionComposer.And(specA.ToExpression(), specB.ToExpression());
        var expr2 = ExpressionComposer.And(specA.ToExpression(), specB.ToExpression());

        var hash1 = ExpressionHasher.ComputeHash(expr1);
        var hash2 = ExpressionHasher.ComputeHash(expr2);

        hash1.Should().Be(hash2, "same composition structure must hash identically");
    }

    [Fact]
    public void ComputeHash_DifferentParameterType_ProducesDifferentHash()
    {
        Expression<Func<Customer, bool>> expr1 = c => true;
        Expression<Func<string, bool>> expr2 = s => true;

        var hash1 = ExpressionHasher.ComputeHash(expr1);
        var hash2 = ExpressionHasher.ComputeHash(expr2);

        hash1.Should().NotBe(hash2, "parameters of different types should produce different hashes");
    }

    [Fact]
    public void ComputeHash_WithNullExpression_ThrowsArgumentNullException()
    {
        var act = () => ExpressionHasher.ComputeHash(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HashVisitor_VisitConstant_NullValue()
    {
        var expr = Expression.Constant(null, typeof(string));
        var hash = ExpressionHasher.ComputeHash(expr);
        hash.Should().NotBe(0);
    }

    [Fact]
    public void HashVisitor_VisitMethodCall()
    {
        var method = typeof(string).GetMethod("ToUpper", Type.EmptyTypes);
        var expr = Expression.Call(Expression.Constant("a"), method!);
        var hash = ExpressionHasher.ComputeHash(expr);
        hash.Should().NotBe(0);
    }

    [Fact]
    public void HashVisitor_VisitBinary_WithMethod()
    {
        var method = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
        var expr = Expression.Add(Expression.Constant("a"), Expression.Constant("b"), method);
        var hash = ExpressionHasher.ComputeHash(expr);
        hash.Should().NotBe(0);
    }

    [Fact]
    public void HashVisitor_VisitUnary_WithMethod()
    {
        var method = typeof(ExpressionHasherTests).GetMethod(nameof(DummyUnaryMethod));
        var expr = Expression.Not(Expression.Constant(false), method);
        var hash = ExpressionHasher.ComputeHash(expr);
        hash.Should().NotBe(0);
    }

    public static bool DummyUnaryMethod(bool b) => !b;
    public static bool DummyUnaryMethodB(bool b) => b;
    public static string DummyBinaryMethodA(string a, string b) => a + b;
    public static string DummyBinaryMethodB(string a, string b) => b + a;

    [Fact]
    public void HashVisitor_VisitBinary_DifferentCustomMethods_ProduceDifferentHashes()
    {
        var methodA = typeof(ExpressionHasherTests).GetMethod(nameof(DummyBinaryMethodA));
        var methodB = typeof(ExpressionHasherTests).GetMethod(nameof(DummyBinaryMethodB));

        var expr1 = Expression.Add(Expression.Constant("a"), Expression.Constant("b"), methodA);
        var expr2 = Expression.Add(Expression.Constant("a"), Expression.Constant("b"), methodB);

        ExpressionHasher.ComputeHash(expr1).Should().NotBe(ExpressionHasher.ComputeHash(expr2),
            "binary expressions with different custom operator methods must hash differently");
    }

    [Fact]
    public void HashVisitor_VisitUnary_DifferentCustomMethods_ProduceDifferentHashes()
    {
        var methodA = typeof(ExpressionHasherTests).GetMethod(nameof(DummyUnaryMethod));
        var methodB = typeof(ExpressionHasherTests).GetMethod(nameof(DummyUnaryMethodB));

        var expr1 = Expression.Not(Expression.Constant(false), methodA);
        var expr2 = Expression.Not(Expression.Constant(false), methodB);

        ExpressionHasher.ComputeHash(expr1).Should().NotBe(ExpressionHasher.ComputeHash(expr2),
            "unary expressions with different custom operator methods must hash differently");
    }

    [Fact]
    public void HashVisitor_VisitNull_DoesNotThrow()
    {
        var visitorType = typeof(ExpressionHasher).GetNestedType("HashVisitor", BindingFlags.NonPublic);
        var visitor = Activator.CreateInstance(visitorType!);
        var visitMethod = visitorType!.GetMethod("Visit", new[] { typeof(Expression) });

        var act = () => visitMethod!.Invoke(visitor, new object[] { null! });
        act.Should().NotThrow();
    }

    [Fact]
    public void HashVisitor_MutantKillers_DifferentSubNodesProduceDifferentHashes()
    {
        var p = Expression.Parameter(typeof(Customer), "c");

        // VisitConstant: 1 != 2
        ExpressionHasher.ComputeHash(Expression.Constant(1)).Should().NotBe(ExpressionHasher.ComputeHash(Expression.Constant(2)));

        // VisitMember: c.Id != c.Name
        ExpressionHasher.ComputeHash(Expression.Property(p, "Id")).Should().NotBe(ExpressionHasher.ComputeHash(Expression.Property(p, "Name")));

        // VisitParameter != VisitConstant
        ExpressionHasher.ComputeHash(p).Should().NotBe(ExpressionHasher.ComputeHash(Expression.Constant(null, typeof(Customer))));

        // VisitBinary: AndAlso != OrElse
        var and = Expression.AndAlso(Expression.Constant(true), Expression.Constant(false));
        var or = Expression.OrElse(Expression.Constant(true), Expression.Constant(false));
        ExpressionHasher.ComputeHash(and).Should().NotBe(ExpressionHasher.ComputeHash(or));

        // VisitMethodCall: ToUpper != ToLower
        var toUpper = typeof(string).GetMethod("ToUpper", Type.EmptyTypes)!;
        var toLower = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
        var prop = Expression.Property(p, "Name");
        var callUpper = Expression.Call(prop, toUpper);
        var callLower = Expression.Call(prop, toLower);
        ExpressionHasher.ComputeHash(callUpper).Should().NotBe(ExpressionHasher.ComputeHash(callLower));
    }

    [Fact]
    public void HashVisitor_VisitBinary_LeftAndConversion_Matter()
    {
        var left1 = Expression.Constant(1);
        var left2 = Expression.Constant(2);
        var right = Expression.Constant(3);

        var bin1 = Expression.Add(left1, right);
        var bin2 = Expression.Add(left2, right);
        ExpressionHasher.ComputeHash(bin1).Should().NotBe(ExpressionHasher.ComputeHash(bin2));

        var unary1 = Expression.Not(left1);
        var unary2 = Expression.Not(left2);
        ExpressionHasher.ComputeHash(unary1).Should().NotBe(ExpressionHasher.ComputeHash(unary2));
    }

    [Fact]
    public void ComputeHash_DifferentParameterTypes_ProducesDifferentHash()
    {
        Expression<Func<Customer, bool>> expr1 = c => true;
        Expression<Func<Order, bool>> expr2 = o => true;

        var hash1 = ExpressionHasher.ComputeHash(expr1);
        var hash2 = ExpressionHasher.ComputeHash(expr2);

        hash1.Should().NotBe(hash2, "expressions with different parameter types must produce different hashes");
    }

    public static int StaticProp1 => 42;
    public static int StaticProp2 => 99;
    public static string? StaticNullProp => null;
    public static int StaticField1 = 42;
    public static int StaticField2 = 99;
    public static string? StaticNullField = null;

    [Fact]
    public void ComputeHash_StaticProperty_DifferentValues_ProducesDifferentHash()
    {
        Expression<Func<int>> expr1 = () => StaticProp1;
        Expression<Func<int>> expr2 = () => StaticProp2;

        var hash1 = ExpressionHasher.ComputeHash(expr1);
        var hash2 = ExpressionHasher.ComputeHash(expr2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeHash_StaticProperty_NullValue_ComputesHash()
    {
        Expression<Func<string?>> expr = () => StaticNullProp;
        var hash = ExpressionHasher.ComputeHash(expr);
        hash.Should().NotBe(0);
    }

    [Fact]
    public void ComputeHash_StaticField_DifferentValues_ProducesDifferentHash()
    {
        Expression<Func<int>> field1 = () => StaticField1;
        Expression<Func<int>> field2 = () => StaticField2;

        var hash1 = ExpressionHasher.ComputeHash(field1);
        var hash2 = ExpressionHasher.ComputeHash(field2);

        hash1.Should().NotBe(hash2);
    }

    public static int MutableStaticField = 10;
    public static int MutableStaticProp { get; set; } = 100;
    public static string? MutableStaticNullProp { get; set; } = null;

    [Fact]
    public void ComputeHash_StaticField_SameMemberDifferentValue_ProducesDifferentHash()
    {
        Expression<Func<int>> expr = () => MutableStaticField;

        MutableStaticField = 10;
        var hash1 = ExpressionHasher.ComputeHash(expr);

        MutableStaticField = 20;
        var hash2 = ExpressionHasher.ComputeHash(expr);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeHash_StaticProperty_SameMemberDifferentValue_ProducesDifferentHash()
    {
        Expression<Func<int>> expr = () => MutableStaticProp;

        MutableStaticProp = 100;
        var hash1 = ExpressionHasher.ComputeHash(expr);

        MutableStaticProp = 200;
        var hash2 = ExpressionHasher.ComputeHash(expr);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeHash_StaticProperty_NullVsNonNull_ProducesDifferentHash()
    {
        Expression<Func<string?>> expr = () => MutableStaticNullProp;

        MutableStaticNullProp = null;
        var hash1 = ExpressionHasher.ComputeHash(expr);

        MutableStaticNullProp = "non-null";
        var hash2 = ExpressionHasher.ComputeHash(expr);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeHash_StaticField_NullValue_ComputesHash()
    {
        Expression<Func<string?>> field = () => StaticNullField;
        var hash = ExpressionHasher.ComputeHash(field);
        hash.Should().NotBe(0);
    }

    [Fact]
    public void ComputeHash_WhenDepthAtMaxDepth_DoesNotThrow()
    {
        Expression expr = Expression.Constant(1);
        for (var i = 0; i < 511; i++)
        {
            expr = Expression.Negate(expr);
        }

        var hash = ExpressionHasher.ComputeHash(expr);
        hash.Should().NotBe(0);
    }

    [Fact]
    public void ComputeHash_WhenDepthExceedsMaxDepth_ThrowsInvalidOperationException()
    {
        Expression expr = Expression.Constant(1);
        for (var i = 0; i < 515; i++)
        {
            expr = Expression.Negate(expr);
        }

        var act = () => ExpressionHasher.ComputeHash(expr);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Expression tree exceeds maximum supported hashing depth of 512.");
    }

    [Fact]
    public void ComputeHash_WideTreeWithManySiblings_DecrementsDepthProperly()
    {
        // 600 constants inside a NewArrayInit expression:
        // Root is depth 1, each element is depth 2.
        // If _depth-- in finally block were mutated to ';' or '_depth++',
        // depth would accumulate across siblings and exceed 512, throwing InvalidOperationException.
        var elements = Enumerable.Range(0, 600).Select(i => (Expression)Expression.Constant(i)).ToArray();
        var arr = Expression.NewArrayInit(typeof(int), elements);

        var hash = ExpressionHasher.ComputeHash(arr);
        hash.Should().NotBe(0);
    }

    [Fact]
    public void GetStaticMemberValue_InstancePropertyAndNullGetter_ReturnsNull()
    {
        var visitorType = typeof(ExpressionHasher).GetNestedType("HashVisitor", BindingFlags.NonPublic)!;
        var method = visitorType.GetMethod("GetStaticMemberValue", BindingFlags.NonPublic | BindingFlags.Static)!;

        // Instance property should return null
        var instanceProp = typeof(Customer).GetProperty(nameof(Customer.Name))!;
        var resultInstance = method.Invoke(null, new object[] { instanceProp });
        resultInstance.Should().BeNull();

        // Write-only property with null GetMethod should return null
        var writeOnlyProp = typeof(ExpressionEqualityComparerTests_CustomType).GetProperty(nameof(ExpressionEqualityComparerTests_CustomType.WriteOnlyValue))!;
        var resultWriteOnly = method.Invoke(null, new object[] { writeOnlyProp });
        resultWriteOnly.Should().BeNull();

        // Non-property, non-field member should return null
        var dummyMethod = typeof(ExpressionHasherTests).GetMethod(nameof(DummyUnaryMethod))!;
        var resultMethod = method.Invoke(null, new object[] { dummyMethod });
        resultMethod.Should().BeNull();
    }
}


