// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Tests for <see cref="ExpressionInterpreter"/> — AOT-safe in-memory evaluation.
/// </summary>
public sealed class ExpressionInterpreterTests
{
    private struct CustomComparableStruct : IComparable
    {
        public int CompareTo(object? obj) => 0;
    }

    private struct CustomNonComparableStruct
    {
        public static bool operator >(CustomNonComparableStruct left, CustomNonComparableStruct right) => false;
        public static bool operator <(CustomNonComparableStruct left, CustomNonComparableStruct right) => false;
    }

    [Fact]
    public void Evaluate_SimpleBooleanProperty_ReturnsCorrectResult()
    {
        Expression<Func<Customer, bool>> expr = c => c.IsActive;

        ExpressionInterpreter.Evaluate(expr, new Customer { IsActive = true }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(expr, new Customer { IsActive = false }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_EqualityComparison_ReturnsCorrectResult()
    {
        Expression<Func<Customer, bool>> expr = c => c.CountryCode == "US";

        ExpressionInterpreter.Evaluate(expr, new Customer { CountryCode = "US" }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(expr, new Customer { CountryCode = "UK" }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NumericComparison_ReturnsCorrectResult()
    {
        Expression<Func<Customer, bool>> expr = c => c.CreditLimit > 1000m;

        ExpressionInterpreter.Evaluate(expr, new Customer { CreditLimit = 2000m }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(expr, new Customer { CreditLimit = 500m }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_AndAlso_ShortCircuits()
    {
        Expression<Func<Customer, bool>> expr = c => c.IsActive && c.CreditLimit > 1000m;

        ExpressionInterpreter.Evaluate(expr, new Customer { IsActive = false, CreditLimit = 0m }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_AndAlso_LeftTrueRightFalse_ReturnsFalse()
    {
        Expression<Func<Customer, bool>> expr = c => c.IsActive && c.CreditLimit > 1000m;
        ExpressionInterpreter.Evaluate(expr, new Customer { IsActive = true, CreditLimit = 100m }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NullConstantComparison_EvaluatesCorrectly()
    {
        Expression<Func<Customer, bool>> expr = c => c.CountryCode == null;
        ExpressionInterpreter.Evaluate(expr, new Customer { CountryCode = null! }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(expr, new Customer { CountryCode = "US" }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_OrElse_ShortCircuits()
    {
        Expression<Func<Customer, bool>> expr = c => c.IsActive || c.CreditLimit > 1000m;

        ExpressionInterpreter.Evaluate(expr, new Customer { IsActive = true, CreditLimit = 0m }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(expr, new Customer { IsActive = false, CreditLimit = 2000m }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(expr, new Customer { IsActive = false, CreditLimit = 0m }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NotOperator_Negates()
    {
        Expression<Func<Customer, bool>> expr = c => !c.IsActive;

        ExpressionInterpreter.Evaluate(expr, new Customer { IsActive = true }).Should().BeFalse();
        ExpressionInterpreter.Evaluate(expr, new Customer { IsActive = false }).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WithNullCandidate_ThrowsArgumentNullException()
    {
        Expression<Func<Customer, bool>> expr = c => c.IsActive;
        var act = () => ExpressionInterpreter.Evaluate(expr, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Evaluate_WithNullExpression_ThrowsArgumentNullException()
    {
        var act = () => ExpressionInterpreter.Evaluate<Customer>(null!, new Customer());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Evaluate_ConditionalExpression_EvaluatesCorrectBranch()
    {
        var param = Expression.Parameter(typeof(Customer), "c");
        var test = Expression.Property(param, nameof(Customer.IsActive));
        var ifTrue = Expression.GreaterThan(
            Expression.Property(param, nameof(Customer.CreditLimit)),
            Expression.Constant(500m));
        var ifFalse = Expression.Equal(
            Expression.Property(param, nameof(Customer.Id)),
            Expression.Constant(1));

        var cond = Expression.Condition(test, ifTrue, ifFalse);
        var lambda = Expression.Lambda<Func<Customer, bool>>(cond, param);

        ExpressionInterpreter.Evaluate(lambda, new Customer { IsActive = true, CreditLimit = 1000m, Id = 99 }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(lambda, new Customer { IsActive = true, CreditLimit = 100m, Id = 99 }).Should().BeFalse();
        ExpressionInterpreter.Evaluate(lambda, new Customer { IsActive = false, CreditLimit = 100m, Id = 1 }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(lambda, new Customer { IsActive = false, CreditLimit = 100m, Id = 2 }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Coalesce_EvaluatesNullFallback()
    {
        var param = Expression.Parameter(typeof(Customer), "c");
        var coalesce = Expression.Coalesce(
            Expression.Property(param, nameof(Customer.CountryCode)),
            Expression.Constant("DEFAULT"));
        var eq = Expression.Equal(
            coalesce,
            Expression.Constant("DEFAULT"));

        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, param);

        ExpressionInterpreter.Evaluate(lambda, new Customer { CountryCode = null! }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(lambda, new Customer { CountryCode = "US" }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_TypeIs_EvaluatesTypeCheck()
    {
        var param = Expression.Parameter(typeof(object), "obj");
        var typeIs = Expression.TypeIs(param, typeof(Customer));
        var lambda = Expression.Lambda<Func<object, bool>>(typeIs, param);

        ExpressionInterpreter.Evaluate(lambda, new Customer()).Should().BeTrue();
        ExpressionInterpreter.Evaluate(lambda, "some string").Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithUnsupportedExpression_ThrowsNotSupportedException()
    {
        var expr = Expression.Lambda<Func<Customer, bool>>(
            Expression.Block(Expression.Constant(true)),
            Expression.Parameter(typeof(Customer), "c")
        );

        var act = () => ExpressionInterpreter.Evaluate(expr, new Customer());
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Block*is not supported by the interpreted evaluator*");
    }

    [Fact]
    public void Evaluate_BinaryOperators_Comprehensive()
    {
        var p = Expression.Parameter(typeof(Customer), "c");

        // NotEqual
        var neq = Expression.Lambda<Func<Customer, bool>>(Expression.NotEqual(Expression.Constant(1), Expression.Constant(2)), p);
        ExpressionInterpreter.Evaluate(neq, new Customer()).Should().BeTrue();

        // GreaterThan
        var gt = Expression.Lambda<Func<Customer, bool>>(Expression.GreaterThan(Expression.Constant(2), Expression.Constant(1)), p);
        ExpressionInterpreter.Evaluate(gt, new Customer()).Should().BeTrue();

        var gtEqual = Expression.Lambda<Func<Customer, bool>>(Expression.GreaterThan(Expression.Constant(2), Expression.Constant(2)), p);
        ExpressionInterpreter.Evaluate(gtEqual, new Customer()).Should().BeFalse("2 > 2 is false");

        // GreaterThanOrEqual
        var gte = Expression.Lambda<Func<Customer, bool>>(Expression.GreaterThanOrEqual(Expression.Constant(2), Expression.Constant(2)), p);
        ExpressionInterpreter.Evaluate(gte, new Customer()).Should().BeTrue();

        // LessThan
        var lt = Expression.Lambda<Func<Customer, bool>>(Expression.LessThan(Expression.Constant(1), Expression.Constant(2)), p);
        ExpressionInterpreter.Evaluate(lt, new Customer()).Should().BeTrue();

        var ltEqual = Expression.Lambda<Func<Customer, bool>>(Expression.LessThan(Expression.Constant(2), Expression.Constant(2)), p);
        ExpressionInterpreter.Evaluate(ltEqual, new Customer()).Should().BeFalse("2 < 2 is false");

        // LessThanOrEqual
        var lte = Expression.Lambda<Func<Customer, bool>>(Expression.LessThanOrEqual(Expression.Constant(2), Expression.Constant(2)), p);
        ExpressionInterpreter.Evaluate(lte, new Customer()).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_UnsupportedBinaryOperator_ThrowsNotSupportedException()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var unsuppBin = Expression.And(
            Expression.Constant(true),
            Expression.Constant(false));
        var lambda = Expression.Lambda<Func<Customer, bool>>(unsuppBin, p);

        var act = () => ExpressionInterpreter.Evaluate(lambda, new Customer());
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Binary operator 'And' is not supported by the interpreted evaluator.");
    }

    [Fact]
    public void Evaluate_UnaryConvert_EvaluatesCorrectly()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var conv = Expression.Convert(Expression.Constant(1), typeof(double));
        var eq = Expression.Equal(conv, Expression.Constant(1.0));
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, p);

        ExpressionInterpreter.Evaluate(lambda, new Customer()).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_UnsupportedUnaryOperator_ThrowsNotSupportedException()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var unaryNotSupported = Expression.IsFalse(Expression.Constant(false));
        var lambda = Expression.Lambda<Func<Customer, bool>>(unaryNotSupported, p);

        var act = () => ExpressionInterpreter.Evaluate(lambda, new Customer());
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Unary operator 'IsFalse' is not supported by the interpreted evaluator.*");
    }

    [Fact]
    public void Evaluate_MethodCall_InstanceAndStatic()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var prop = Expression.Property(p, nameof(Customer.Name));
        var method = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        var call = Expression.Call(prop, method!, Expression.Constant("A"));
        var lambda = Expression.Lambda<Func<Customer, bool>>(call, p);

        ExpressionInterpreter.Evaluate(lambda, new Customer { Name = "Alice" }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(lambda, new Customer { Name = "Bob" }).Should().BeFalse();

        // Static method
        var isNullOrEmpty = typeof(string).GetMethod("IsNullOrEmpty", new[] { typeof(string) })!;
        var staticCall = Expression.Call(null, isNullOrEmpty, prop);
        var staticLambda = Expression.Lambda<Func<Customer, bool>>(staticCall, p);

        ExpressionInterpreter.Evaluate(staticLambda, new Customer { Name = null! }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(staticLambda, new Customer { Name = "Alice" }).Should().BeFalse();
    }

    private sealed class EntityWithField
    {
        public int ValueField = 42;
    }

    [Fact]
    public void Evaluate_Field_EvaluatesCorrectly()
    {
        var p = Expression.Parameter(typeof(EntityWithField), "e");
        var field = Expression.Field(p, nameof(EntityWithField.ValueField));
        var eq = Expression.Equal(field, Expression.Constant(42));
        var lambda = Expression.Lambda<Func<EntityWithField, bool>>(eq, p);

        ExpressionInterpreter.Evaluate(lambda, new EntityWithField { ValueField = 42 }).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_CompareValues_NonIComparable_ThrowsNotSupportedException()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var gt = Expression.GreaterThan(Expression.Constant(new CustomNonComparableStruct()), Expression.Constant(new CustomNonComparableStruct()));
        var lambda = Expression.Lambda<Func<Customer, bool>>(gt, p);

        var act = () => ExpressionInterpreter.Evaluate(lambda, new Customer());
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Cannot compare values of type*");
    }

    [Fact]
    public void Evaluate_NullComparisons_ThrowsNotSupportedException()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var gt = Expression.GreaterThan(Expression.Constant(null, typeof(int?)), Expression.Constant(1, typeof(int?)));
        var lambda = Expression.Lambda<Func<Customer, bool>>(gt, p);

        var act = () => ExpressionInterpreter.Evaluate(lambda, new Customer());
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Evaluate_AndAlso_ShortCircuit_WithThrowHelper()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var falseExpr = Expression.Constant(false);
        var throwExpr = Expression.Call(typeof(ExpressionInterpreterTests).GetMethod(nameof(ThrowHelper))!);
        var andAlso = Expression.AndAlso(falseExpr, throwExpr);
        var lambda = Expression.Lambda<Func<Customer, bool>>(andAlso, p);

        ExpressionInterpreter.Evaluate(lambda, new Customer()).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_OrElse_ShortCircuit_WithThrowHelper()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var trueExpr = Expression.Constant(true);
        var throwExpr = Expression.Call(typeof(ExpressionInterpreterTests).GetMethod(nameof(ThrowHelper))!);
        var orElse = Expression.OrElse(trueExpr, throwExpr);
        var lambda = Expression.Lambda<Func<Customer, bool>>(orElse, p);

        ExpressionInterpreter.Evaluate(lambda, new Customer()).Should().BeTrue();
    }

    public static bool ThrowHelper() => throw new InvalidOperationException("Should not be evaluated due to short-circuit");

    [Fact]
    public void Evaluate_ThreeValuedLogic_NullOperands()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var nullBool = Expression.Constant(null, typeof(bool?));
        var trueBool = Expression.Constant(true, typeof(bool?));
        var falseBool = Expression.Constant(false, typeof(bool?));

        // AndAlso with null and true -> null
        var andAlso = Expression.AndAlso(nullBool, trueBool);
        var eqAnd = Expression.Equal(andAlso, Expression.Constant(null, typeof(bool?)));
        var lambdaAnd = Expression.Lambda<Func<Customer, bool>>(eqAnd, p);
        ExpressionInterpreter.Evaluate(lambdaAnd, new Customer()).Should().BeTrue();

        // OrElse with null and false -> null
        var orElse = Expression.OrElse(nullBool, falseBool);
        var eqOr = Expression.Equal(orElse, Expression.Constant(null, typeof(bool?)));
        var lambdaOr = Expression.Lambda<Func<Customer, bool>>(eqOr, p);
        ExpressionInterpreter.Evaluate(lambdaOr, new Customer()).Should().BeTrue();

        // OrElse with false and null -> null
        var orElseFalseNull = Expression.OrElse(falseBool, nullBool);
        var eqOrFalseNull = Expression.Equal(orElseFalseNull, Expression.Constant(null, typeof(bool?)));
        var lambdaOrFalseNull = Expression.Lambda<Func<Customer, bool>>(eqOrFalseNull, p);
        ExpressionInterpreter.Evaluate(lambdaOrFalseNull, new Customer()).Should().BeTrue();

        // AndAlso with null and false -> false
        var andAlsoNullFalse = Expression.AndAlso(nullBool, falseBool);
        var eqAndNullFalse = Expression.Equal(andAlsoNullFalse, Expression.Constant(false, typeof(bool?)));
        var lambdaAndNullFalse = Expression.Lambda<Func<Customer, bool>>(eqAndNullFalse, p);
        ExpressionInterpreter.Evaluate(lambdaAndNullFalse, new Customer()).Should().BeTrue();

        // AndAlso with true and null -> null
        var andAlsoTrueNull = Expression.AndAlso(trueBool, nullBool);
        var eqAndTrueNull = Expression.Equal(andAlsoTrueNull, Expression.Constant(null, typeof(bool?)));
        var lambdaAndTrueNull = Expression.Lambda<Func<Customer, bool>>(eqAndTrueNull, p);
        ExpressionInterpreter.Evaluate(lambdaAndTrueNull, new Customer()).Should().BeTrue();

        // OrElse with null and true -> true
        var orElseNullTrue = Expression.OrElse(nullBool, trueBool);
        var eqOrNullTrue = Expression.Equal(orElseNullTrue, Expression.Constant(true, typeof(bool?)));
        var lambdaOrNullTrue = Expression.Lambda<Func<Customer, bool>>(eqOrNullTrue, p);
        ExpressionInterpreter.Evaluate(lambdaOrNullTrue, new Customer()).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_StaticMember_EvaluatesWithoutInstance()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var staticProp = Expression.Property(null, typeof(DateTime), nameof(DateTime.UtcNow));
        var yearProp = Expression.Property(staticProp, nameof(DateTime.Year));
        var gt = Expression.GreaterThan(yearProp, Expression.Constant(2000));
        var lambda = Expression.Lambda<Func<Customer, bool>>(gt, p);

        ExpressionInterpreter.Evaluate(lambda, new Customer()).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_TypeBinary_TypeIs_MatchingType_ReturnsTrue()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var prop = Expression.Property(p, nameof(Customer.Name));
        var typeIs = Expression.TypeIs(prop, typeof(string));
        var lambda = Expression.Lambda<Func<Customer, bool>>(typeIs, p);

        ExpressionInterpreter.Evaluate(lambda, new Customer { Name = "Alice" }).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_TypeBinary_TypeIs_NonMatchingType_ReturnsFalse()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var prop = Expression.Property(p, nameof(Customer.Name));
        var typeIs = Expression.TypeIs(prop, typeof(int));
        var lambda = Expression.Lambda<Func<Customer, bool>>(typeIs, p);

        ExpressionInterpreter.Evaluate(lambda, new Customer { Name = "Alice" }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_TypeBinary_TypeIs_NullOperand_ReturnsFalse()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var prop = Expression.Property(p, nameof(Customer.Name));
        var typeIs = Expression.TypeIs(prop, typeof(string));
        var lambda = Expression.Lambda<Func<Customer, bool>>(typeIs, p);

        ExpressionInterpreter.Evaluate(lambda, new Customer { Name = null! }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_TypeBinary_UnsupportedTypeEqual_ThrowsNotSupportedException()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var prop = Expression.Property(p, nameof(Customer.Name));
        var typeEqual = Expression.TypeEqual(prop, typeof(string));
        var lambda = Expression.Lambda<Func<Customer, bool>>(typeEqual, p);

        var act = () => ExpressionInterpreter.Evaluate(lambda, new Customer { Name = "Alice" });
        act.Should().Throw<NotSupportedException>()
            .WithMessage("TypeBinary operator 'TypeEqual' is not supported by the interpreted evaluator.");
    }

    [Fact]
    public void Evaluate_Unary_Convert_ConvertsCorrectly()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var prop = Expression.Property(p, nameof(Customer.Id));
        var convert = Expression.Convert(prop, typeof(double));
        var gt = Expression.GreaterThan(convert, Expression.Constant(5.0));
        var lambda = Expression.Lambda<Func<Customer, bool>>(gt, p);

        ExpressionInterpreter.Evaluate(lambda, new Customer { Id = 10 }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(lambda, new Customer { Id = 2 }).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_Unary_UnsupportedOperator_ThrowsNotSupportedException()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var prop = Expression.Property(p, nameof(Customer.Id));
        var negate = Expression.Negate(prop);
        var eq = Expression.Equal(negate, Expression.Constant(-10));
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, p);

        var act = () => ExpressionInterpreter.Evaluate(lambda, new Customer { Id = 10 });
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Unary operator 'Negate' is not supported by the interpreted evaluator.");
    }

    [Fact]
    public void Evaluate_UnsupportedNodeType_ThrowsNotSupportedException()
    {
        var p = Expression.Parameter(typeof(Customer), "c");
        var block = Expression.Block(Expression.Constant(true));
        var lambda = Expression.Lambda<Func<Customer, bool>>(block, p);

        var act = () => ExpressionInterpreter.Evaluate(lambda, new Customer());
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Expression node type 'Block' is not supported by the interpreted evaluator. Use ToCompiledPredicate() in JIT environments for full expression support.");
    }

    [Fact]
    public void Evaluate_AndAlso_AllBranches_EvaluatesCorrectly()
    {
        Expression<Func<Customer, bool>> trueAndTrue = c => c.IsActive && c.CreditLimit > 10;
        Expression<Func<Customer, bool>> trueAndFalse = c => c.IsActive && c.CreditLimit < 0;
        Expression<Func<Customer, bool>> falseAndTrue = c => !c.IsActive && c.CreditLimit > 10;
        Expression<Func<Customer, bool>> falseAndFalse = c => !c.IsActive && c.CreditLimit < 0;

        var customer = new Customer { IsActive = true, CreditLimit = 100m };

        ExpressionInterpreter.Evaluate(trueAndTrue, customer).Should().BeTrue();
        ExpressionInterpreter.Evaluate(trueAndFalse, customer).Should().BeFalse();
        ExpressionInterpreter.Evaluate(falseAndTrue, customer).Should().BeFalse();
        ExpressionInterpreter.Evaluate(falseAndFalse, customer).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_OrElse_AllBranches_EvaluatesCorrectly()
    {
        Expression<Func<Customer, bool>> trueOrTrue = c => c.IsActive || c.CreditLimit > 10;
        Expression<Func<Customer, bool>> trueOrFalse = c => c.IsActive || c.CreditLimit < 0;
        Expression<Func<Customer, bool>> falseOrTrue = c => !c.IsActive || c.CreditLimit > 10;
        Expression<Func<Customer, bool>> falseOrFalse = c => !c.IsActive || c.CreditLimit < 0;

        var customer = new Customer { IsActive = true, CreditLimit = 100m };

        ExpressionInterpreter.Evaluate(trueOrTrue, customer).Should().BeTrue();
        ExpressionInterpreter.Evaluate(trueOrFalse, customer).Should().BeTrue();
        ExpressionInterpreter.Evaluate(falseOrTrue, customer).Should().BeTrue();
        ExpressionInterpreter.Evaluate(falseOrFalse, customer).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WhenDepthAtMaxDepth_DoesNotThrow()
    {
        Expression expr = Expression.Constant(1);
        for (var i = 0; i < 511; i++)
        {
            expr = Expression.Convert(expr, typeof(int));
        }

        var param = Expression.Parameter(typeof(Customer), "c");
        var eq = Expression.Equal(expr, Expression.Constant(1));
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, param);

        ExpressionInterpreter.Evaluate(lambda, new Customer()).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WhenDepthExceedsMaxDepth_ThrowsInvalidOperationException()
    {
        Expression expr = Expression.Constant(1);
        for (var i = 0; i < 515; i++)
        {
            expr = Expression.Convert(expr, typeof(int));
        }

        var param = Expression.Parameter(typeof(Customer), "c");
        var eq = Expression.Equal(expr, Expression.Constant(1));
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, param);

        var act = () => ExpressionInterpreter.Evaluate(lambda, new Customer());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Expression tree exceeds maximum supported evaluation depth of 512.");
    }

    [Fact]
    public void Evaluate_ForbiddenNamespaces_ThrowsInvalidOperationException()
    {
        var param = Expression.Parameter(typeof(Customer), "c");

        // System.Diagnostics
        var diagMethod = typeof(System.Diagnostics.Process).GetMethod(nameof(System.Diagnostics.Process.GetCurrentProcess), Type.EmptyTypes)!;
        var diagCall = Expression.Call(diagMethod);
        var diagLambda = Expression.Lambda<Func<Customer, bool>>(Expression.NotEqual(diagCall, Expression.Constant(null, typeof(System.Diagnostics.Process))), param);
        var actDiag = () => ExpressionInterpreter.Evaluate(diagLambda, new Customer());
        actDiag.Should().Throw<InvalidOperationException>()
            .WithMessage("*is not permitted in interpreted specification evaluation for security reasons.");

        // System.IO
        var ioMethod = typeof(System.IO.Path).GetMethod(nameof(System.IO.Path.GetTempPath), Type.EmptyTypes)!;
        var ioCall = Expression.Call(ioMethod);
        var ioLambda = Expression.Lambda<Func<Customer, bool>>(Expression.NotEqual(ioCall, Expression.Constant(null, typeof(string))), param);
        var actIo = () => ExpressionInterpreter.Evaluate(ioLambda, new Customer());
        actIo.Should().Throw<InvalidOperationException>()
            .WithMessage("*is not permitted in interpreted specification evaluation for security reasons.");

        // System.Reflection
        var reflMethod = typeof(System.Reflection.Assembly).GetMethod(nameof(System.Reflection.Assembly.GetExecutingAssembly), Type.EmptyTypes)!;
        var reflCall = Expression.Call(reflMethod);
        var reflLambda = Expression.Lambda<Func<Customer, bool>>(Expression.NotEqual(reflCall, Expression.Constant(null, typeof(System.Reflection.Assembly))), param);
        var actRefl = () => ExpressionInterpreter.Evaluate(reflLambda, new Customer());
        actRefl.Should().Throw<InvalidOperationException>()
            .WithMessage("*is not permitted in interpreted specification evaluation for security reasons.");

        // Environment
        var envMethod = typeof(Environment).GetMethod(nameof(Environment.GetEnvironmentVariable), new[] { typeof(string) })!;
        var envCall = Expression.Call(envMethod, Expression.Constant("PATH"));
        var envLambda = Expression.Lambda<Func<Customer, bool>>(Expression.NotEqual(envCall, Expression.Constant(null, typeof(string))), param);
        var actEnv = () => ExpressionInterpreter.Evaluate(envLambda, new Customer());
        actEnv.Should().Throw<InvalidOperationException>()
            .WithMessage("*is not permitted in interpreted specification evaluation for security reasons.");
    }

    [Fact]
    public void Evaluate_ConvertNullableType_FromDifferentNumericType()
    {
        var param = Expression.Parameter(typeof(Customer), "c");
        var convertExpr = Expression.Convert(Expression.Constant(42L), typeof(int?));
        var eq = Expression.Equal(convertExpr, Expression.Constant((int?)42, typeof(int?)));
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, param);

        ExpressionInterpreter.Evaluate(lambda, new Customer()).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_CompareValues_DifferentNumericTypes()
    {
        Expression<Func<Customer, bool>> exprGte = c => c.CreditLimit >= 100;
        Expression<Func<Customer, bool>> exprLte = c => c.CreditLimit <= 100;
        Expression<Func<Customer, bool>> exprGt = c => c.CreditLimit > 100;
        Expression<Func<Customer, bool>> exprLt = c => c.CreditLimit < 100;

        ExpressionInterpreter.Evaluate(exprGte, new Customer { CreditLimit = 100m }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(exprLte, new Customer { CreditLimit = 100m }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(exprGt, new Customer { CreditLimit = 150m }).Should().BeTrue();
        ExpressionInterpreter.Evaluate(exprLt, new Customer { CreditLimit = 50m }).Should().BeTrue();
    }

    [Fact]
    public void Evaluate_CompareValues_WithNullOperand_ReturnsFalse()
    {
        // When b.Left is not ConstantExpression and rightVal is null:
#pragma warning disable CS0464
        Expression<Func<Customer, bool>> exprRightNullGt = c => ((int?)c.Id) > null;
        Expression<Func<Customer, bool>> exprRightNullGte = c => ((int?)c.Id) >= null;
        Expression<Func<Customer, bool>> exprRightNullLt = c => ((int?)c.Id) < null;
        Expression<Func<Customer, bool>> exprRightNullLte = c => ((int?)c.Id) <= null;
#pragma warning restore CS0464

        var customer = new Customer { Id = 10 };
        ExpressionInterpreter.Evaluate(exprRightNullGt, customer).Should().BeFalse();
        ExpressionInterpreter.Evaluate(exprRightNullGte, customer).Should().BeFalse();
        ExpressionInterpreter.Evaluate(exprRightNullLt, customer).Should().BeFalse();
        ExpressionInterpreter.Evaluate(exprRightNullLte, customer).Should().BeFalse();

        // When b.Left is not ConstantExpression and leftVal is null:
        Expression<Func<Customer, bool>> exprLeftNullGt = c => (c.Id == 10 ? (int?)null : 1) > 5;
        Expression<Func<Customer, bool>> exprLeftNullGte = c => (c.Id == 10 ? (int?)null : 1) >= 5;
        Expression<Func<Customer, bool>> exprLeftNullLt = c => (c.Id == 10 ? (int?)null : 1) < 5;
        Expression<Func<Customer, bool>> exprLeftNullLte = c => (c.Id == 10 ? (int?)null : 1) <= 5;

        ExpressionInterpreter.Evaluate(exprLeftNullGt, customer).Should().BeFalse();
        ExpressionInterpreter.Evaluate(exprLeftNullGte, customer).Should().BeFalse();
        ExpressionInterpreter.Evaluate(exprLeftNullLt, customer).Should().BeFalse();
        ExpressionInterpreter.Evaluate(exprLeftNullLte, customer).Should().BeFalse();
    }

    [Fact]
    public void CompareValues_Helper_DirectComparisons()
    {
        var method = typeof(ExpressionInterpreter).GetMethod("CompareValues", BindingFlags.NonPublic | BindingFlags.Static)!;

        // Decimal vs int (different types, right is converted to left type)
        var res1 = (int)method.Invoke(null, new object?[] { 10m, 5 })!;
        res1.Should().BePositive();

        var res2 = (int)method.Invoke(null, new object?[] { 10m, 10 })!;
        res2.Should().Be(0);

        var res3 = (int)method.Invoke(null, new object?[] { 10m, 15 })!;
        res3.Should().BeNegative();

        // Right is null: IComparable.CompareTo(null) returns > 0
        var resNull = (int)method.Invoke(null, new object?[] { 10, null })!;
        resNull.Should().BePositive();

        // Right is unconvertible type falls back to direct comparison
        var actInvalidCast = () => method.Invoke(null, new object?[] { 10, new object() });
        actInvalidCast.Should().Throw<TargetInvocationException>().WithInnerException<ArgumentException>();
    }

    [Fact]
    public void Evaluate_UnsupportedBinaryOperator_Modulo_ThrowsNotSupportedException()
    {
        var param = Expression.Parameter(typeof(Customer), "c");
        var bin = Expression.Modulo(Expression.Constant(10), Expression.Constant(3));
        var method = typeof(ExpressionInterpreter).GetMethod("EvaluateBinary", BindingFlags.NonPublic | BindingFlags.Static)!;
        var generic = method.MakeGenericMethod(typeof(Customer));

        var act = () =>
        {
            try
            {
                generic.Invoke(null, new object[] { bin, param, new Customer(), 0 });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };

        act.Should().Throw<NotSupportedException>()
            .WithMessage("Binary operator 'Modulo' is not supported by the interpreted evaluator.");
    }

    [Fact]
    public void Evaluate_UnsupportedBinaryOperator_WithNullOperand_ThrowsNotSupportedException()
    {
        var param = Expression.Parameter(typeof(int?), "x");
        var bin = Expression.Modulo(param, Expression.Constant((int?)3, typeof(int?)));
        var method = typeof(ExpressionInterpreter).GetMethod("EvaluateBinary", BindingFlags.NonPublic | BindingFlags.Static)!;
        var generic = method.MakeGenericMethod(typeof(int?));

        var act = () =>
        {
            try
            {
                generic.Invoke(null, new object?[] { bin, param, (int?)null, 0 });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };

        act.Should().Throw<NotSupportedException>()
            .WithMessage("Binary operator 'Modulo' is not supported by the interpreted evaluator.");
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Conditional")]
    [InlineData("TypeBinary")]
    [InlineData("MethodCall")]
    public void EvaluateNode_DepthIncrement_AtBoundary_IncrementsAndThrows(string kind)
    {
        var param = Expression.Parameter(typeof(Customer), "c");
        var evaluateNodeMethod = typeof(ExpressionInterpreter).GetMethod("EvaluateNode", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(Customer));

        Expression node = kind switch
        {
            "Member" => Expression.Property(param, nameof(Customer.Name)),
            "Conditional" => Expression.Condition(Expression.Constant(true), Expression.Constant(1), Expression.Constant(2)),
            "TypeBinary" => Expression.TypeIs(param, typeof(Customer)),
            "MethodCall" => Expression.Call(param, typeof(object).GetMethod(nameof(ToString))!),
            _ => throw new ArgumentException(kind)
        };

        var act = () =>
        {
            try
            {
                // Calling at depth 512 passes depth check on root node (512 > 512 is false),
                // but child evaluation receives depth + 1 = 513, exceeding 512 and throwing InvalidOperationException.
                // Under arithmetic mutation depth - 1, child receives depth 511 and does not throw.
                evaluateNodeMethod.Invoke(null, new object[] { node, param, new Customer { Name = "Test" }, 512 });
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Expression tree exceeds maximum supported evaluation depth of 512.");
    }
}



