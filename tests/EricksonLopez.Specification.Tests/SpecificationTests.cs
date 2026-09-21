// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Reflection;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Tests for the <see cref="Specification{T}"/> base class and domain specification behaviors.
/// </summary>
public sealed class SpecificationTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // IsSatisfiedBy — in-memory evaluation
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsSatisfiedBy_ActiveCustomer_WhenIsActive_ReturnsTrue()
    {
        var spec = new ActiveCustomerSpecification();
        var customer = new Customer { IsActive = true };

        spec.IsSatisfiedBy(customer).Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_ActiveCustomer_WhenNotActive_ReturnsFalse()
    {
        var spec = new ActiveCustomerSpecification();
        var customer = new Customer { IsActive = false };

        spec.IsSatisfiedBy(customer).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_WithNullCandidate_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();

        var act = () => spec.IsSatisfiedBy(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ToExpression — expression tree correctness
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ToExpression_ReturnsSameInstanceOnRepeatedCalls()
    {
        var spec = new ActiveCustomerSpecification();

        var expr1 = spec.ToExpression();
        var expr2 = spec.ToExpression();

        ReferenceEquals(expr1, expr2).Should().BeTrue("expression should be cached");
    }

    [Fact]
    public void ToExpression_NotNull()
    {
        var spec = new ActiveCustomerSpecification();
        spec.ToExpression().Should().NotBeNull();
    }

    [Fact]
    public void ToExpression_UnderConcurrentAccess_InitializesThreadSafelyAndExecutesBuildExpressionOnce()
    {
        var buildCount = 0;
        var spec = new CountingSpecification(() => System.Threading.Interlocked.Increment(ref buildCount));

        System.Threading.Tasks.Parallel.For(0, 50, _ =>
        {
            var expr = spec.ToExpression();
            expr.Should().NotBeNull();
        });

        buildCount.Should().Be(1, "Lazy initialization must be thread-safe and call BuildExpression exactly once");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // And composition
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void And_ActiveAndNotDeleted_SatisfiedWhenBothTrue()
    {
        var spec = new ActiveCustomerSpecification()
            .And(new NotDeletedCustomerSpecification());

        var validCustomer = new Customer { IsActive = true, IsDeleted = false };
        var activeButDeleted = new Customer { IsActive = true, IsDeleted = true };
        var notActiveNotDeleted = new Customer { IsActive = false, IsDeleted = false };

        spec.IsSatisfiedBy(validCustomer).Should().BeTrue();
        spec.IsSatisfiedBy(activeButDeleted).Should().BeFalse();
        spec.IsSatisfiedBy(notActiveNotDeleted).Should().BeFalse();
    }

    [Fact]
    public void And_ComposedExpression_HasNoInvocationNodes()
    {
        var spec = new ActiveCustomerSpecification()
            .And(new NotDeletedCustomerSpecification());

        var expr = spec.ToExpression();
        var visitor = new InvocationNodeDetector();
        visitor.Visit(expr);

        visitor.FoundInvocation.Should().BeFalse(
            "composed expressions must not contain Expression.Invoke nodes");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Or composition
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Or_ActiveOrNotDeleted_SatisfiedWhenEitherTrue()
    {
        var spec = new ActiveCustomerSpecification()
            .Or(new NotDeletedCustomerSpecification());

        var bothTrue = new Customer { IsActive = true, IsDeleted = false };
        var onlyActive = new Customer { IsActive = true, IsDeleted = true };
        var onlyNotDeleted = new Customer { IsActive = false, IsDeleted = false };
        var neitherTrue = new Customer { IsActive = false, IsDeleted = true };

        spec.IsSatisfiedBy(bothTrue).Should().BeTrue();
        spec.IsSatisfiedBy(onlyActive).Should().BeTrue();
        spec.IsSatisfiedBy(onlyNotDeleted).Should().BeTrue();
        spec.IsSatisfiedBy(neitherTrue).Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Not composition
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Not_ActiveSpec_IsSatisfiedByInactiveCustomer()
    {
        var spec = new ActiveCustomerSpecification().Not();

        spec.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeFalse();
        spec.IsSatisfiedBy(new Customer { IsActive = false }).Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Deep composition
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DeepComposition_ThreeSpecifications_CorrectResult()
    {
        var spec = new ActiveCustomerSpecification()
            .And(new NotDeletedCustomerSpecification())
            .And(new CustomerFromCountrySpecification("US"));

        var valid = new Customer { IsActive = true, IsDeleted = false, CountryCode = "US" };
        var wrongCountry = new Customer { IsActive = true, IsDeleted = false, CountryCode = "UK" };

        spec.IsSatisfiedBy(valid).Should().BeTrue();
        spec.IsSatisfiedBy(wrongCountry).Should().BeFalse();
    }

    [Fact]
    public void DeepComposition_ExpressionTreeIsFlat_NoInvocations()
    {
        var spec = new ActiveCustomerSpecification()
            .And(new NotDeletedCustomerSpecification())
            .And(new CustomerFromCountrySpecification("US"))
            .And(new CreditLimitExceedsSpecification(1000m));

        var expr = spec.ToExpression();
        var visitor = new InvocationNodeDetector();
        visitor.Visit(expr);

        visitor.FoundInvocation.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Implicit conversion to QuerySpec
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ImplicitConversion_ToQuerySpec_ContainsCriteria()
    {
        var spec = new ActiveCustomerSpecification();
        QuerySpec<Customer> querySpec = spec;

        querySpec.Criteria.Should().HaveCount(1);
    }

    [Fact]
    public void ImplicitConversion_WithNull_ThrowsArgumentNullException()
    {
        Specification<Customer> spec = null!;
        var act = () => { QuerySpec<Customer> qs = spec; };
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToQuerySpec_WithAdditionalModifiers_CombinesCorrectly()
    {
        var querySpec = new ActiveCustomerSpecification()
            .ToQuerySpec()
            .OrderBy(c => c.Name)
            .Take(20);

        querySpec.Criteria.Should().HaveCount(1);
        querySpec.OrderClauses.Should().HaveCount(1);
        querySpec.TakeCount.Should().Be(20);
    }

    [Fact]
    public void ToQuerySpec_WithNullOther_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var act = () => spec.ToQuerySpec(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void And_WithNullOther_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var act = () => spec.And(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Or_WithNullOther_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var act = () => spec.Or(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CompositeSpecification_InvalidKind_ThrowsInvalidOperationException()
    {
        var spec1 = new ActiveCustomerSpecification();
        var spec2 = new ActiveCustomerSpecification();

        var type = typeof(Specification<Customer>).Assembly.GetTypes()
            .First(t => t.Name == "CompositeSpecification`1");

        var instance = Activator.CreateInstance(
            type.MakeGenericType(typeof(Customer)),
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new object[] { spec1, spec2, (CompositionKind)99 },
            null);

        var act = () => ((Specification<Customer>)instance!).ToExpression();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ToCompiledPredicate_ReturnsWorkingDelegate()
    {
        var spec = new ActiveCustomerSpecification();
        var compiled = spec.ToCompiledPredicate();

        compiled(new Customer { IsActive = true }).Should().BeTrue();
        compiled(new Customer { IsActive = false }).Should().BeFalse();
    }
}

/// <summary>Tests for <see cref="Specification{T}.ToDebugString()"/>.</summary>
public sealed class SpecificationToDebugStringTests
{
    [Fact]
    public void ToDebugString_BooleanMember_ReturnsMemberName()
    {
        var spec = new ActiveCustomerSpecification();
        var debug = spec.ToDebugString();
        debug.Should().Contain("IsActive");
    }

    [Fact]
    public void ToDebugString_NotBooleanMember_ContainsNot()
    {
        var spec = new NotDeletedCustomerSpecification();
        var debug = spec.ToDebugString();
        debug.Should().Contain("NOT");
        debug.Should().Contain("IsDeleted");
    }

    [Fact]
    public void ToDebugString_EqualityWithClosure_ContainsMemberAndValue()
    {
        var spec = new CustomerFromCountrySpecification("US");
        var debug = spec.ToDebugString();
        debug.Should().Contain("CountryCode");
        debug.Should().Contain("US");
    }

    [Fact]
    public void ToDebugString_GreaterThan_ContainsOperatorAndValue()
    {
        var spec = new CreditLimitExceedsSpecification(1000m);
        var debug = spec.ToDebugString();
        debug.Should().Contain("CreditLimit");
        (debug.Contains(">=") || debug.Contains(">")).Should().BeTrue("expected a comparison operator in debug output");
    }

    [Fact]
    public void ToDebugString_AndComposition_ContainsAnd()
    {
        var spec = new ActiveCustomerSpecification().And(new NotDeletedCustomerSpecification());
        var debug = spec.ToDebugString();
        debug.Should().Contain("AND");
        debug.Should().Contain("IsActive");
        debug.Should().Contain("IsDeleted");
    }

    [Fact]
    public void ToDebugString_OrComposition_ContainsOr()
    {
        var spec = new ActiveCustomerSpecification().Or(new NotDeletedCustomerSpecification());
        var debug = spec.ToDebugString();
        debug.Should().Contain("OR");
    }

    [Fact]
    public void ToDebugString_NullExpression_ThrowsArgumentNullException()
    {
        var act = () => ExpressionDebugFormatter.Format<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDebugString_InlineSpec_ContainsCorrectMember()
    {
        var spec = Spec.For<Customer>(c => c.CreditLimit > 500m);
        var debug = spec.ToDebugString();
        debug.Should().Contain("CreditLimit");
    }
}

/// <summary>Tests for <see cref="Spec.All{T}"/> and <see cref="Spec.Any{T}"/> combinators.</summary>
public sealed class SpecAllAnyTests
{
    [Fact]
    public void All_EmptyArray_ReturnsTrueSpec()
    {
        var spec = Spec.All<Customer>();
        var customer = new Customer { IsActive = false }; // Any customer
        spec.IsSatisfiedBy(customer).Should().BeTrue("empty All() is the neutral AND element — always true");
    }

    [Fact]
    public void All_SingleSpec_ReturnsSameLogic()
    {
        var spec = Spec.All(new ActiveCustomerSpecification());
        spec.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { IsActive = false }).Should().BeFalse();
    }

    [Fact]
    public void All_MultipleSpecs_RequiresAll()
    {
        var spec = Spec.All(
            new ActiveCustomerSpecification(),
            new NotDeletedCustomerSpecification());

        spec.IsSatisfiedBy(new Customer { IsActive = true, IsDeleted = false }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { IsActive = false, IsDeleted = false }).Should().BeFalse();
        spec.IsSatisfiedBy(new Customer { IsActive = true, IsDeleted = true }).Should().BeFalse();
    }

    [Fact]
    public void All_ThrowsOnNullArray()
    {
        var act = () => Spec.All<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Any_EmptyArray_ReturnsFalseSpec()
    {
        var spec = Spec.Any<Customer>();
        var customer = new Customer { IsActive = true }; // Even active customers
        spec.IsSatisfiedBy(customer).Should().BeFalse("empty Any() is the neutral OR element — always false");
    }

    [Fact]
    public void Any_SingleSpec_ReturnsSameLogic()
    {
        var spec = Spec.Any(new ActiveCustomerSpecification());
        spec.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { IsActive = false }).Should().BeFalse();
    }

    [Fact]
    public void Any_MultipleSpecs_RequiresAtLeastOne()
    {
        var spec = Spec.Any(
            new ActiveCustomerSpecification(),
            new NotDeletedCustomerSpecification());

        spec.IsSatisfiedBy(new Customer { IsActive = true, IsDeleted = true }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { IsActive = false, IsDeleted = false }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { IsActive = false, IsDeleted = true }).Should().BeFalse();
    }

    [Fact]
    public void Any_ThrowsOnNullArray()
    {
        var act = () => Spec.Any<Customer>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Operators (&, |, !, &&, ||) and named methods
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OperatorAnd_NullLeft_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var act = () => { var _ = (Specification<Customer>)null! & spec; };
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("left");
    }

    [Fact]
    public void OperatorAnd_NullRight_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var act = () => { var _ = spec & (Specification<Customer>)null!; };
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("right");
    }

    [Fact]
    public void BitwiseAnd_NullArguments_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var actLeft = () => Specification<Customer>.BitwiseAnd(null!, spec);
        actLeft.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("left");

        var actRight = () => Specification<Customer>.BitwiseAnd(spec, null!);
        actRight.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("right");
    }

    [Fact]
    public void BitwiseAnd_CombinesWithLogicalAnd()
    {
        var spec1 = new ActiveCustomerSpecification();
        var spec2 = new NotDeletedCustomerSpecification();
        var combined = Specification<Customer>.BitwiseAnd(spec1, spec2);

        combined.IsSatisfiedBy(new Customer { IsActive = true, IsDeleted = false }).Should().BeTrue();
        combined.IsSatisfiedBy(new Customer { IsActive = true, IsDeleted = true }).Should().BeFalse();
        combined.IsSatisfiedBy(new Customer { IsActive = false, IsDeleted = false }).Should().BeFalse();
    }

    [Fact]
    public void OperatorOr_NullLeft_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var act = () => { var _ = (Specification<Customer>)null! | spec; };
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("left");
    }

    [Fact]
    public void OperatorOr_NullRight_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var act = () => { var _ = spec | (Specification<Customer>)null!; };
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("right");
    }

    [Fact]
    public void BitwiseOr_NullArguments_ThrowsArgumentNullException()
    {
        var spec = new ActiveCustomerSpecification();
        var actLeft = () => Specification<Customer>.BitwiseOr(null!, spec);
        actLeft.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("left");

        var actRight = () => Specification<Customer>.BitwiseOr(spec, null!);
        actRight.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("right");
    }

    [Fact]
    public void BitwiseOr_CombinesWithLogicalOr()
    {
        var spec1 = new ActiveCustomerSpecification();
        var spec2 = new NotDeletedCustomerSpecification();
        var combined = Specification<Customer>.BitwiseOr(spec1, spec2);

        combined.IsSatisfiedBy(new Customer { IsActive = true, IsDeleted = true }).Should().BeTrue();
        combined.IsSatisfiedBy(new Customer { IsActive = false, IsDeleted = false }).Should().BeTrue();
        combined.IsSatisfiedBy(new Customer { IsActive = false, IsDeleted = true }).Should().BeFalse();
    }

    [Fact]
    public void OperatorNot_NullSpecification_ThrowsArgumentNullException()
    {
        var act = () => { var _ = !(Specification<Customer>)null!; };
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("specification");
    }

    [Fact]
    public void LogicalNot_NullSpecification_ThrowsArgumentNullException()
    {
        var act = () => Specification<Customer>.LogicalNot(null!);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("specification");
    }

    [Fact]
    public void LogicalNot_NegatesSpecification()
    {
        var spec = new ActiveCustomerSpecification();
        var negated = Specification<Customer>.LogicalNot(spec);

        negated.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeFalse();
        negated.IsSatisfiedBy(new Customer { IsActive = false }).Should().BeTrue();
    }

    [Fact]
    public void ConditionalAnd_ShortCircuitOperator_EvaluatesBothOperands()
    {
        var spec1 = new ActiveCustomerSpecification();
        var spec2 = new NotDeletedCustomerSpecification();
        var combined = spec1 && spec2;

        combined.IsSatisfiedBy(new Customer { IsActive = true, IsDeleted = true }).Should().BeFalse();
        combined.IsSatisfiedBy(new Customer { IsActive = true, IsDeleted = false }).Should().BeTrue();
    }

    [Fact]
    public void ConditionalOr_ShortCircuitOperator_EvaluatesBothOperands()
    {
        var spec1 = new ActiveCustomerSpecification();
        var spec2 = new NotDeletedCustomerSpecification();
        var combined = spec1 || spec2;

        combined.IsSatisfiedBy(new Customer { IsActive = false, IsDeleted = false }).Should().BeTrue();
        combined.IsSatisfiedBy(new Customer { IsActive = false, IsDeleted = true }).Should().BeFalse();
    }
}

/// <summary>Tests for automatic ExpressionSimplifier integration in CompositeSpecification.</summary>
public sealed class ExpressionSimplifierIntegrationTests
{
    [Fact]
    public void TrueAnd_RealSpec_SimplifiesToRealSpec()
    {
        var realSpec = new ActiveCustomerSpecification();
        var composed = Spec.True<Customer>().And(realSpec);

        // The composed expression should be simplified — no constant "true" left in tree
        var expr = composed.ToExpression();
        // After simplification, the body should NOT be a BinaryExpression with a constant left node
        var body = expr.Body;
        if (body is System.Linq.Expressions.BinaryExpression bin)
        {
            bin.Left.Should().NotBeOfType<System.Linq.Expressions.ConstantExpression>(
                "Spec.True<T>().And(real) should simplify the constant away");
        }
    }

    [Fact]
    public void TrueAnd_RealSpec_EvaluatesCorrectly()
    {
        var spec = Spec.True<Customer>().And(new ActiveCustomerSpecification());
        spec.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { IsActive = false }).Should().BeFalse();
    }

    [Fact]
    public void RealSpec_AndFalse_AlwaysFalse()
    {
        var spec = new ActiveCustomerSpecification().And(Spec.False<Customer>());
        spec.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeFalse(
            "X AND false = false always");
    }

    [Fact]
    public void FalseOr_RealSpec_SimplifiesToRealSpec()
    {
        var spec = Spec.False<Customer>().Or(new ActiveCustomerSpecification());
        spec.IsSatisfiedBy(new Customer { IsActive = true }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { IsActive = false }).Should().BeFalse();
    }

    [Fact]
    public void TwoRealSpecs_NoSimplification_EvaluatesCorrectly()
    {
        // Non-constant composition should work normally
        var spec = new ActiveCustomerSpecification().And(new NotDeletedCustomerSpecification());
        spec.IsSatisfiedBy(new Customer { IsActive = true, IsDeleted = false }).Should().BeTrue();
        spec.IsSatisfiedBy(new Customer { IsActive = false, IsDeleted = false }).Should().BeFalse();
    }

    [Fact]
    public void CompositeSpecification_WithUnknownCompositionKind_ThrowsInvalidOperationException()
    {
        var spec = new CompositeSpecification<Customer>(
            new ActiveCustomerSpecification(),
            new NotDeletedCustomerSpecification(),
            (CompositionKind)999);

        var act = () => spec.ToExpression();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unknown composition kind: 999");
    }
}

/// <summary>Helper visitor to detect InvocationExpression nodes in expression trees.</summary>
internal sealed class InvocationNodeDetector : System.Linq.Expressions.ExpressionVisitor
{
    public bool FoundInvocation { get; private set; }

    protected override System.Linq.Expressions.Expression VisitInvocation(
        System.Linq.Expressions.InvocationExpression node)
    {
        FoundInvocation = true;
        return base.VisitInvocation(node);
    }
}


