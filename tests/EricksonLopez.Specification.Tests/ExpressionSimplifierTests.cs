// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Tests for <see cref="ExpressionSimplifier"/> — constant folding and Boolean simplification.
/// </summary>
public sealed class ExpressionSimplifierTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // AND simplification
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Simplify_TrueAndA_ReturnsA()
    {
        Expression<Func<int, bool>> expr = x => true && x > 0;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(5).Should().BeTrue();
        simplified.Compile()(-1).Should().BeFalse();
    }

    [Fact]
    public void Simplify_AAndTrue_ReturnsA()
    {
        Expression<Func<int, bool>> expr = x => x > 0 && true;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(5).Should().BeTrue();
        simplified.Compile()(-1).Should().BeFalse();
    }

    [Fact]
    public void Simplify_FalseAndA_ReturnsFalse()
    {
        Expression<Func<int, bool>> expr = x => false && x > 0;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(5).Should().BeFalse("short-circuited to false");
        simplified.Compile()(0).Should().BeFalse();
    }

    [Fact]
    public void Simplify_AAndFalse_ReturnsFalse()
    {
        Expression<Func<int, bool>> expr = x => x > 0 && false;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(5).Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // OR simplification
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Simplify_TrueOrA_ReturnsTrue()
    {
        Expression<Func<int, bool>> expr = x => true || x > 0;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(-1000).Should().BeTrue("always true");
    }

    [Fact]
    public void Simplify_AOrTrue_ReturnsTrue()
    {
        Expression<Func<int, bool>> expr = x => x > 0 || true;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(-1000).Should().BeTrue();
    }

    [Fact]
    public void Simplify_FalseOrA_ReturnsA()
    {
        Expression<Func<int, bool>> expr = x => false || x > 0;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(5).Should().BeTrue();
        simplified.Compile()(-1).Should().BeFalse();
    }

    [Fact]
    public void Simplify_AOrFalse_ReturnsA()
    {
        Expression<Func<int, bool>> expr = x => x > 0 || false;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(5).Should().BeTrue();
        simplified.Compile()(-1).Should().BeFalse();
    }

    [Fact]
    public void Simplify_AstOrFalseExplicit_ReturnsLeft()
    {
        var param = Expression.Parameter(typeof(int), "x");
        var gt = Expression.GreaterThan(param, Expression.Constant(0));
        var orElse = Expression.OrElse(gt, Expression.Constant(false));
        var lambda = Expression.Lambda<Func<int, bool>>(orElse, param);

        var simplified = ExpressionSimplifier.Simplify(lambda);
        simplified.Body.Should().Be(gt);
    }

    [Fact]
    public void Simplify_OrElseNoConstants_ReturnsSameOrRebuilt()
    {
        Expression<Func<Customer, bool>> expr = c => c.CreditLimit > 100m || c.IsActive;
        var simplified = ExpressionSimplifier.Simplify(expr);
        simplified.Compile()(new Customer { CreditLimit = 200m, IsActive = false }).Should().BeTrue();
        simplified.Compile()(new Customer { CreditLimit = 50m, IsActive = false }).Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NOT simplification
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Simplify_NotTrue_ReturnsFalse()
    {
        Expression<Func<int, bool>> expr = x => !true;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(0).Should().BeFalse();
    }

    [Fact]
    public void Simplify_AstNotTrue_ReturnsFalseConstant()
    {
        var param = Expression.Parameter(typeof(int), "x");
        var notTrue = Expression.Not(Expression.Constant(true));
        var lambda = Expression.Lambda<Func<int, bool>>(notTrue, param);
        var simplified = ExpressionSimplifier.Simplify(lambda);

        simplified.Body.Should().BeOfType<ConstantExpression>()
            .Which.Value.Should().Be(false);
    }

    [Fact]
    public void Simplify_AstNotFalse_ReturnsTrueConstant()
    {
        var param = Expression.Parameter(typeof(int), "x");
        var notFalse = Expression.Not(Expression.Constant(false));
        var lambda = Expression.Lambda<Func<int, bool>>(notFalse, param);
        var simplified = ExpressionSimplifier.Simplify(lambda);

        simplified.Body.Should().BeOfType<ConstantExpression>()
            .Which.Value.Should().Be(true);
    }

    [Fact]
    public void Simplify_NotFalse_ReturnsTrue()
    {
        Expression<Func<int, bool>> expr = x => !false;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(0).Should().BeTrue();
    }

    [Fact]
    public void Simplify_NotNotA_ReturnsA()
    {
        Expression<Func<bool, bool>> expr = x => !!x;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(true).Should().BeTrue();
        simplified.Compile()(false).Should().BeFalse();
    }

    [Fact]
    public void Simplify_NotNotNot_SimplifiesToNot()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var x = Expression.Equal(Expression.Property(p, "Id"), Expression.Constant(1));
        var not1 = Expression.Not(x);
        var not2 = Expression.Not(not1);
        var not3 = Expression.Not(not2);

        var lambda = Expression.Lambda<Func<Customer, bool>>(not3, p);
        var simplified = ExpressionSimplifier.Simplify(lambda);

        simplified.Body.NodeType.Should().Be(ExpressionType.Not);
        ((UnaryExpression)simplified.Body).Operand.Should().BeSameAs(x);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Rebuilding AST nodes
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Simplify_NoConstants_ReturnsSameStructure()
    {
        Expression<Func<Customer, bool>> expr = c => c.IsActive && c.CreditLimit > 1000m;
        var simplified = ExpressionSimplifier.Simplify(expr);

        simplified.Compile()(new Customer { IsActive = true, CreditLimit = 2000m }).Should().BeTrue();
        simplified.Compile()(new Customer { IsActive = false, CreditLimit = 2000m }).Should().BeFalse();
    }

    [Fact]
    public void Simplify_NestedSimplifiedBinary_RebuildsParentBinary()
    {
        Expression<Func<Customer, bool>> expr = c => (c.CreditLimit > 18m && true) == (c.IsActive && true);
        var simplified = ExpressionSimplifier.Simplify(expr);
        simplified.Compile()(new Customer { CreditLimit = 20m, IsActive = true }).Should().BeTrue();
    }

    [Fact]
    public void Simplify_UnaryNonNot_VisitsBase()
    {
        Expression<Func<Customer, bool>> expr = c => -c.CreditLimit < 0m;
        var simplified = ExpressionSimplifier.Simplify(expr);
        simplified.Compile()(new Customer { CreditLimit = 10m }).Should().BeTrue();
    }

    [Fact]
    public void Simplify_UnaryOperandChanged_RebuildsNot()
    {
        Expression<Func<Customer, bool>> expr = c => !(c.CreditLimit > 18m && true);
        var simplified = ExpressionSimplifier.Simplify(expr);
        simplified.Compile()(new Customer { CreditLimit = 20m }).Should().BeFalse();
        simplified.Compile()(new Customer { CreditLimit = 10m }).Should().BeTrue();
    }

    [Fact]
    public void Simplify_UnaryOperandUnchanged_ReturnsSameInstance()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var not = Expression.Not(Expression.Property(p, nameof(Customer.IsActive)));
        var lambda = Expression.Lambda<Func<Customer, bool>>(not, p);
        var simplified = ExpressionSimplifier.Simplify(lambda);

        simplified.Body.Should().BeSameAs(not, "when inner operand is unchanged, UnaryExpression instance should be preserved");
    }

    [Fact]
    public void Simplify_RebuildsBinary_WhenOperandsChange()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var left = Expression.Equal(Expression.Property(p, "Id"), Expression.Constant(1));
        var right = Expression.Not(Expression.Not(Expression.Constant(true)));
        var and = Expression.AndAlso(left, right);

        var lambda = Expression.Lambda<Func<Customer, bool>>(and, p);
        var simplified = ExpressionSimplifier.Simplify(lambda);

        simplified.Body.Should().Be(left);
    }

    [Fact]
    public void Simplify_RebuildsBinary_WhenChildrenChangeButNoTrivialSimplification()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var left = Expression.Not(Expression.Not(Expression.Equal(Expression.Property(p, "Id"), Expression.Constant(1))));
        var right = Expression.Equal(Expression.Property(p, "Id"), Expression.Constant(2));
        var and = Expression.AndAlso(left, right);

        var lambda = Expression.Lambda<Func<Customer, bool>>(and, p);
        var simplified = ExpressionSimplifier.Simplify(lambda);

        simplified.Body.NodeType.Should().Be(ExpressionType.AndAlso);
        simplified.Body.Should().NotBeSameAs(and);
    }

    [Fact]
    public void Simplify_AllBooleanCombinations_SimplifyCorrectly()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var x = Expression.Equal(Expression.Property(p, nameof(Customer.Id)), Expression.Constant(1));

        // false OR x -> x
        var falseOrX = Expression.Lambda<Func<Customer, bool>>(Expression.OrElse(Expression.Constant(false), x), p);
        ExpressionSimplifier.Simplify(falseOrX).Body.Should().BeSameAs(x);

        // x OR false -> x
        var xOrFalse = Expression.Lambda<Func<Customer, bool>>(Expression.OrElse(x, Expression.Constant(false)), p);
        ExpressionSimplifier.Simplify(xOrFalse).Body.Should().BeSameAs(x);

        // true OR x -> true
        var trueOrX = Expression.Lambda<Func<Customer, bool>>(Expression.OrElse(Expression.Constant(true), x), p);
        ((ConstantExpression)ExpressionSimplifier.Simplify(trueOrX).Body).Value.Should().Be(true);

        // x OR true -> true
        var xOrTrue = Expression.Lambda<Func<Customer, bool>>(Expression.OrElse(x, Expression.Constant(true)), p);
        ((ConstantExpression)ExpressionSimplifier.Simplify(xOrTrue).Body).Value.Should().Be(true);

        // true AND x -> x
        var trueAndX = Expression.Lambda<Func<Customer, bool>>(Expression.AndAlso(Expression.Constant(true), x), p);
        ExpressionSimplifier.Simplify(trueAndX).Body.Should().BeSameAs(x);

        // x AND true -> x
        var xAndTrue = Expression.Lambda<Func<Customer, bool>>(Expression.AndAlso(x, Expression.Constant(true)), p);
        ExpressionSimplifier.Simplify(xAndTrue).Body.Should().BeSameAs(x);

        // false AND x -> false
        var falseAndX = Expression.Lambda<Func<Customer, bool>>(Expression.AndAlso(Expression.Constant(false), x), p);
        ((ConstantExpression)ExpressionSimplifier.Simplify(falseAndX).Body).Value.Should().Be(false);

        // x AND false -> false
        var xAndFalse = Expression.Lambda<Func<Customer, bool>>(Expression.AndAlso(x, Expression.Constant(false)), p);
        ((ConstantExpression)ExpressionSimplifier.Simplify(xAndFalse).Body).Value.Should().Be(false);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Null guards and Reflection testing
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Simplify_WithNull_ThrowsArgumentNullException()
    {
        var act = () => ExpressionSimplifier.Simplify<int>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Simplify_VisitBinary_Null_ThrowsArgumentNullException()
    {
        var method = typeof(ExpressionSimplifier).GetMethod("VisitBinary", BindingFlags.NonPublic | BindingFlags.Instance);
        var act = () =>
        {
            try
            {
                method!.Invoke(ExpressionSimplifier.Default, new object[] { null! });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Simplify_VisitUnary_Null_ThrowsArgumentNullException()
    {
        var method = typeof(ExpressionSimplifier).GetMethod("VisitUnary", BindingFlags.NonPublic | BindingFlags.Instance);
        var act = () =>
        {
            try
            {
                method!.Invoke(ExpressionSimplifier.Default, new object[] { null! });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        act.Should().Throw<ArgumentNullException>();
    }
}


