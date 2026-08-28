// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Tests for <see cref="ExpressionComposer"/> — the core expression composition engine.
/// </summary>
public sealed class ExpressionComposerTests
{
    // ──────────────────────────────────────────────────────────────────────────
    // And composition
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void And_TwoPredicates_EvaluatesBothCorrectly()
    {
        Expression<Func<int, bool>> left = x => x > 0;
        Expression<Func<int, bool>> right = x => x < 100;

        var combined = ExpressionComposer.And(left, right);

        combined.Compile()(50).Should().BeTrue();
        combined.Compile()(-1).Should().BeFalse();
        combined.Compile()(100).Should().BeFalse();
    }

    [Fact]
    public void And_ResultHasNoInvocationExpression()
    {
        Expression<Func<Customer, bool>> left = c => c.IsActive;
        Expression<Func<Customer, bool>> right = c => !c.IsDeleted;

        var combined = ExpressionComposer.And(left, right);
        var detector = new InvocationNodeDetector();
        detector.Visit(combined);

        detector.FoundInvocation.Should().BeFalse();
    }

    [Fact]
    public void And_ResultUsesSharedParameter()
    {
        Expression<Func<Customer, bool>> left = c => c.IsActive;
        Expression<Func<Customer, bool>> right = c => !c.IsDeleted;

        var combined = ExpressionComposer.And(left, right);

        // The combined expression should have exactly one parameter
        combined.Parameters.Should().HaveCount(1);
    }

    [Fact]
    public void And_WithNullLeft_ThrowsArgumentNullException()
    {
        Expression<Func<int, bool>> right = x => x > 0;
        var act = () => ExpressionComposer.And(null!, right);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void And_WithNullRight_ThrowsArgumentNullException()
    {
        Expression<Func<int, bool>> left = x => x > 0;
        var act = () => ExpressionComposer.And(left, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Or composition
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Or_TwoPredicates_EvaluatesCorrectly()
    {
        Expression<Func<int, bool>> left = x => x < 0;
        Expression<Func<int, bool>> right = x => x > 100;

        var combined = ExpressionComposer.Or(left, right);

        combined.Compile()(-1).Should().BeTrue();
        combined.Compile()(50).Should().BeFalse();
        combined.Compile()(200).Should().BeTrue();
    }

    [Fact]
    public void Or_ResultHasNoInvocationExpression()
    {
        Expression<Func<Customer, bool>> left = c => c.IsActive;
        Expression<Func<Customer, bool>> right = c => c.CreditLimit > 1000m;

        var combined = ExpressionComposer.Or(left, right);
        var detector = new InvocationNodeDetector();
        detector.Visit(combined);

        detector.FoundInvocation.Should().BeFalse();
    }

    [Fact]
    public void Or_WithNullLeft_ThrowsArgumentNullException()
    {
        Expression<Func<int, bool>> right = x => x > 0;
        var act = () => ExpressionComposer.Or(null!, right);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Or_WithNullRight_ThrowsArgumentNullException()
    {
        Expression<Func<int, bool>> left = x => x > 0;
        var act = () => ExpressionComposer.Or(left, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Not
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Not_NegatesCorrectly()
    {
        Expression<Func<int, bool>> predicate = x => x > 0;
        var negated = ExpressionComposer.Not(predicate);

        negated.Compile()(1).Should().BeFalse();
        negated.Compile()(0).Should().BeTrue();
        negated.Compile()(-1).Should().BeTrue();
    }

    [Fact]
    public void Not_WithNullPredicate_ThrowsArgumentNullException()
    {
        var act = () => ExpressionComposer.Not<int>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // AndAll
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AndAll_MultiplePredicates_AllMustBeTrue()
    {
        var predicates = new Expression<Func<int, bool>>[]
        {
            x => x > 0,
            x => x < 100,
            x => x % 2 == 0
        };

        var combined = ExpressionComposer.AndAll<int>(predicates.AsSpan());

        combined.Compile()(2).Should().BeTrue();
        combined.Compile()(1).Should().BeFalse("odd number");
        combined.Compile()(-2).Should().BeFalse("negative");
        combined.Compile()(200).Should().BeFalse("too large");
    }

    [Fact]
    public void AndAll_SinglePredicate_ReturnsSinglePredicate()
    {
        Expression<Func<int, bool>> predicate = x => x > 0;
        var combined = ExpressionComposer.AndAll<int>(new[] { predicate }.AsSpan());

        combined.Should().BeSameAs(predicate);
    }

    [Fact]
    public void AndAll_EmptySpan_ThrowsArgumentException()
    {
        var act = () => ExpressionComposer.AndAll<int>(ReadOnlySpan<Expression<Func<int, bool>>>.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one predicate is required.*")
            .WithParameterName("predicates");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // OrAny
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OrAny_SinglePredicate_ReturnsSinglePredicate()
    {
        Expression<Func<int, bool>> predicate = x => x > 0;
        var combined = ExpressionComposer.OrAny<int>(new[] { predicate }.AsSpan());

        combined.Should().BeSameAs(predicate);
    }

    [Fact]
    public void OrAny_EmptySpan_ThrowsArgumentException()
    {
        var act = () => ExpressionComposer.OrAny<int>(ReadOnlySpan<Expression<Func<int, bool>>>.Empty);
        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one predicate is required.*")
            .WithParameterName("predicates");
    }

    [Fact]
    public void OrAny_MultiplePredicates_AnyMustBeTrue()
    {
        var predicates = new Expression<Func<int, bool>>[]
        {
            x => x < 0,
            x => x > 1000,
            x => x == 42
        };

        var combined = ExpressionComposer.OrAny<int>(predicates.AsSpan());

        combined.Compile()(-5).Should().BeTrue();
        combined.Compile()(42).Should().BeTrue();
        combined.Compile()(9999).Should().BeTrue();
        combined.Compile()(7).Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Nested composition correctness
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void NestedComposition_AndOfOrs_CorrectResult()
    {
        Expression<Func<int, bool>> isNegative = x => x < 0;
        Expression<Func<int, bool>> isHuge = x => x > 1000;
        Expression<Func<int, bool>> isSmall = x => x < 10;
        Expression<Func<int, bool>> isEven = x => x % 2 == 0;

        // (negative OR huge) AND (small OR even)
        var combined = ExpressionComposer.And(
            ExpressionComposer.Or(isNegative, isHuge),
            ExpressionComposer.Or(isSmall, isEven));

        // -2: negative (yes), small (yes) → true
        combined.Compile()(-2).Should().BeTrue();
        // -5: negative (yes), odd (no), not small (>10? no it's -5 <10) → true
        combined.Compile()(-5).Should().BeTrue();
        // 2000: huge (yes), even (yes) → true
        combined.Compile()(2000).Should().BeTrue();
        // 500: not negative, not huge (< 1000? wait 500 < 1000 so not huge), not small → false unless even
        // 500 < 1000 so not huge, not negative → left side false → false
        combined.Compile()(500).Should().BeFalse();
    }
}


