// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Specification;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.Tests;

public static class SecurityContext
{
    public static int CurrentTenantId { get; set; } = 1;
}

public sealed class TenantRecord
{
    public int Id { get; init; }
    public int TenantId { get; init; }
    public int? NullableScore { get; init; }
    public string Data { get; init; } = string.Empty;
}

public sealed class AdversarialRegressionTests : IDisposable
{
    public AdversarialRegressionTests()
    {
        QueryPlanCache.Clear();
        ExpressionCompilationCache.Clear();
    }

    public void Dispose()
    {
        QueryPlanCache.Clear();
        ExpressionCompilationCache.Clear();
    }

    [Fact]
    public void Regression_SEC01_QueryPlanCache_IsolatesParametersAcrossContexts()
    {
        // ARRANGE: Tenant 1 executes query with TenantId = 100
        SecurityContext.CurrentTenantId = 100;
        var translator = new QuerySpecTranslator<TenantRecord>("records");

        Expression<Func<TenantRecord, bool>> filter1 = r => r.TenantId == SecurityContext.CurrentTenantId;
        var spec1 = Spec.For(filter1).ToQuerySpec();
        var plan1 = translator.Translate(spec1);

        plan1.Parameters.Should().ContainSingle();
        ((int)plan1.Parameters[0].Value!).Should().Be(100);

        // ACT: Tenant 2 executes query with TenantId = 200
        SecurityContext.CurrentTenantId = 200;
        Expression<Func<TenantRecord, bool>> filter2 = r => r.TenantId == SecurityContext.CurrentTenantId;
        var spec2 = Spec.For(filter2).ToQuerySpec();
        var plan2 = translator.Translate(spec2);

        // ASSERT: Tenant 2 MUST receive parameter value 200 (NOT Tenant 1's 100)
        plan2.Parameters.Should().ContainSingle();
        var paramVal2 = (int)plan2.Parameters[0].Value!;
        paramVal2.Should().Be(200, "QueryPlanCache must not leak parameters across different execution contexts.");
    }

    [Fact]
    public void Regression_BUG_EXP01_ExpressionInterpreter_HandlesNullableConversions()
    {
        // ARRANGE: Candidate records with null and non-null values
        var recordNull = new TenantRecord { Id = 10, NullableScore = null };
        var recordMatch = new TenantRecord { Id = 10, NullableScore = 80 };
        var recordOther = new TenantRecord { Id = 20, NullableScore = 30 };

        // ACT & ASSERT: Must not throw InvalidCastException when evaluating Nullable expressions (SEC-01 / BUG-EXP-01 exploit)
        var specCast = Spec.For<TenantRecord>(r => (int?)r.Id == (int?)10);
        specCast.IsSatisfiedBy(recordNull).Should().BeTrue();
        specCast.IsSatisfiedBy(recordOther).Should().BeFalse();

        var specCoalesce = Spec.For<TenantRecord>(r => (r.NullableScore ?? 0) > 50);
        specCoalesce.IsSatisfiedBy(recordNull).Should().BeFalse();
        specCoalesce.IsSatisfiedBy(recordMatch).Should().BeTrue();
        specCoalesce.IsSatisfiedBy(recordOther).Should().BeFalse();
    }

    [Fact]
    public void Regression_SEC02_ExpressionInterpreter_GuardsRecursionDepth()
    {
        // ARRANGE: Create deeply nested expression tree > 512 nodes
        ParameterExpression param = Expression.Parameter(typeof(TenantRecord), "x");
        Expression current = Expression.Equal(param, Expression.Constant(null, typeof(TenantRecord)));

        for (int i = 0; i < 550; i++)
        {
            current = Expression.OrElse(current, Expression.Constant(false));
        }

        var lambda = Expression.Lambda<Func<TenantRecord, bool>>(current, param);

        // ACT & ASSERT: Should throw InvalidOperationException instead of StackOverflowException
        var act = () => ExpressionInterpreter.Evaluate(lambda, new TenantRecord());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*depth*");
    }

    [Fact]
    public void Regression_API01_Spec_Between_WithNullableTypes()
    {
        // ARRANGE & ACT: Spec.Between on Nullable<int>
        var spec = Spec.Between<TenantRecord, int>(r => r.NullableScore, 20, 80);

        // ASSERT
        var match = new TenantRecord { NullableScore = 50 };
        var outside = new TenantRecord { NullableScore = 90 };
        var nullVal = new TenantRecord { NullableScore = null };

        spec.IsSatisfiedBy(match).Should().BeTrue();
        spec.IsSatisfiedBy(outside).Should().BeFalse();
        spec.IsSatisfiedBy(nullVal).Should().BeFalse();
    }

    [Fact]
    public void Regression_API01_Spec_Between_InvalidBounds_ThrowsArgumentException()
    {
        // ACT & ASSERT: lower > upper should throw ArgumentException
        var act = () => Spec.Between<TenantRecord, int>(r => r.Id, 100, 10);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be greater than*");
    }

    [Fact]
    public void Regression_API02_Specification_BooleanOperators()
    {
        var specA = Spec.For<TenantRecord>(r => r.Id > 5);
        var specB = Spec.For<TenantRecord>(r => r.Id < 20);

        // Operator &
        var andSpec = specA & specB;
        andSpec.IsSatisfiedBy(new TenantRecord { Id = 10 }).Should().BeTrue();
        andSpec.IsSatisfiedBy(new TenantRecord { Id = 25 }).Should().BeFalse();

        // Operator |
        var orSpec = specA | Spec.For<TenantRecord>(r => r.Id == 0);
        orSpec.IsSatisfiedBy(new TenantRecord { Id = 0 }).Should().BeTrue();
        orSpec.IsSatisfiedBy(new TenantRecord { Id = 10 }).Should().BeTrue();
        orSpec.IsSatisfiedBy(new TenantRecord { Id = 2 }).Should().BeFalse();

        // Operator !
        var notSpec = !specA;
        notSpec.IsSatisfiedBy(new TenantRecord { Id = 2 }).Should().BeTrue();
        notSpec.IsSatisfiedBy(new TenantRecord { Id = 10 }).Should().BeFalse();
    }

    [Fact]
    public void Regression_EXP10_AndSpecification_ShortCircuitsWhenLeftIsFalse()
    {
        var falseSpec = Spec.For<TenantRecord>(r => false);
        var explosiveSpec = new ExplosiveSpec<TenantRecord>();

        var composite = falseSpec.And(explosiveSpec);

        // Should NOT evaluate explosiveSpec
        bool result = composite.IsSatisfiedBy(new TenantRecord());
        result.Should().BeFalse();
        explosiveSpec.WasEvaluated.Should().BeFalse();
    }

    [Fact]
    public void Regression_SEC03_ExpressionInterpreter_BlocksUnsafeMethods()
    {
        var method = typeof(Environment).GetMethod(nameof(Environment.GetEnvironmentVariable), [typeof(string)])!;
        var param = Expression.Parameter(typeof(TenantRecord), "r");
        var call = Expression.Call(method, Expression.Constant("PATH"));
        var notNull = Expression.NotEqual(call, Expression.Constant(null, typeof(string)));
        var lambda = Expression.Lambda<Func<TenantRecord, bool>>(notNull, param);

        var act = () => ExpressionInterpreter.Evaluate(lambda, new TenantRecord());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not permitted*security*");
    }

    [Fact]
    public void Regression_MUT02_QuerySpec_Skip_Negative_ThrowsArgumentOutOfRangeException()
    {
        var spec = QuerySpec<TenantRecord>.Empty;
        var act = () => spec.Skip(-1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Regression_MUT03_QuerySpec_Take_ZeroOrNegative_ThrowsArgumentOutOfRangeException(int invalidTake)
    {
        var spec = QuerySpec<TenantRecord>.Empty;
        var act = () => spec.Take(invalidTake);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Regression_MUT04_ExpressionInterpreter_NegationOfNullOrFalse()
    {
        // !false => true
        var specFalse = Spec.For<TenantRecord>(r => !false);
        specFalse.IsSatisfiedBy(new TenantRecord()).Should().BeTrue();

        // !(null == null) => false
        var specNullNeg = Spec.For<TenantRecord>(r => !(r.NullableScore == null));
        specNullNeg.IsSatisfiedBy(new TenantRecord { NullableScore = null }).Should().BeFalse();
        specNullNeg.IsSatisfiedBy(new TenantRecord { NullableScore = 42 }).Should().BeTrue();
    }

    private sealed class ExplosiveSpec<
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties |
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] T> : Specification<T>
    {
        public bool WasEvaluated { get; private set; }

        protected override Expression<Func<T, bool>> BuildExpression() => x => true;

        public new bool IsSatisfiedBy(T entity)
        {
            WasEvaluated = true;
            throw new InvalidOperationException("ExplosiveSpec was evaluated!");
        }
    }
}
