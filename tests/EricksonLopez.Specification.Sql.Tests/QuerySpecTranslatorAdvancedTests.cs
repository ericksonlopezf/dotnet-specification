// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Specification.MsSql;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.Sql.Tests;

// ─────────────────────────────────────────────────────────
// Advanced and Edge Cases tests for QuerySpecTranslator
// ─────────────────────────────────────────────────────────

[Collection("QueryPlanCacheCollection")]
public class QuerySpecTranslatorAdvancedTests : IDisposable
{
    public QuerySpecTranslatorAdvancedTests() => QueryPlanCache.Clear();
    public void Dispose() => QueryPlanCache.Clear();

    [Fact]
    public void Constructor_ThrowsOnNullTableName()
    {
        var act = () => new QuerySpecTranslator<Customer>("   ");
        act.Should().Throw<ArgumentException>();
    }

    private class CustomResolver : IColumnNameResolver { public string Resolve(string propertyName) => "X"; }

    [Fact]
    public void Constructor_UsesCustomResolver()
    {
        var translator = new QuerySpecTranslator<Customer>("t", new CustomResolver());
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name == "A");
        var model = translator.Translate(spec);
        model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject.ColumnName.Should().Be("X");
    }

    [Fact]
    public void Translate_BothNullColumns_Throws()
    {
        var translator = new QuerySpecTranslator<Customer>("t");
        var spec = QuerySpec<Customer>.Empty.Where(c => 1 == 1);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void StringMethod_OnNonMember_Throws()
    {
        var translator = new QuerySpecTranslator<Customer>("t");
        var spec = QuerySpec<Customer>.Empty.Where(c => "foo".Contains(c.Name));
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Translate_CachingBehavior_SingleCriteria_ReusesCachedPlanOnlyWhenNoPaginationOrOrdering()
    {
        QueryPlanCache.Clear();
        var translator = new QuerySpecTranslator<Customer>("customers");
        var baseSpec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);

        // Single-criteria pure filter: cache hit on second call
        var plan1 = translator.Translate(baseSpec);
        var plan2 = translator.Translate(baseSpec);
        ReferenceEquals(plan1, plan2).Should().BeTrue("Pure single-criteria query should return identical cached plan instance");

        // Spec with Skip should NOT return cached plan
        var specSkip = baseSpec.Skip(10);
        var planSkip = translator.Translate(specSkip);
        ReferenceEquals(plan1, planSkip).Should().BeFalse();
        planSkip.Skip.Should().Be(10);

        // Spec with Take should NOT return cached plan
        var specTake = baseSpec.Take(5);
        var planTake = translator.Translate(specTake);
        ReferenceEquals(plan1, planTake).Should().BeFalse();
        planTake.Take.Should().Be(5);

        // Spec with Order should NOT return cached plan
        var specOrder = baseSpec.OrderBy(c => c.Name);
        var planOrder = translator.Translate(specOrder);
        ReferenceEquals(plan1, planOrder).Should().BeFalse();
        planOrder.Orders.Should().NotBeEmpty();

        // Multi-criteria pure-filter specs ARE now cached via composed AndAll key (ADR-021 fix).
        // The composed cache key is structurally unique, so the second translation returns the cached plan.
        var multiSpec = baseSpec.Where(c => c.CreditLimit > 100m);
        var multiPlan1 = translator.Translate(multiSpec);
        var multiPlan2 = translator.Translate(multiSpec);
        ReferenceEquals(multiPlan1, multiPlan2).Should().BeTrue(
            "Multi-criteria pure-filter specs are cached via ExpressionComposer.AndAll key (ADR-021)");

        // Multi-criteria spec with pagination is NOT cached
        var multiSpecPaged = multiSpec.Take(10);
        var multiPagedPlan = translator.Translate(multiSpecPaged);
        ReferenceEquals(multiPlan1, multiPagedPlan).Should().BeFalse(
            "Multi-criteria specs with pagination must not use the cached pure-filter plan");
        multiPagedPlan.Take.Should().Be(10);
    }

    [Fact]
    public void Translate_BetweenMethod_ProducesBetweenPredicateNode()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(c => c.CreditLimit.Between(100m, 500m));
        var model = translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        model.Filters[0].Should().BeOfType<BetweenPredicateNode>();

        var between = (BetweenPredicateNode)model.Filters[0];
        between.ColumnName.Should().Be("credit_limit");
        between.LowerParameterName.Should().Be("p1");
        between.UpperParameterName.Should().Be("p2");

        model.Parameters.Should().HaveCount(2);
        model.Parameters[0].Name.Should().Be("p1");
        model.Parameters[0].Value.Should().Be(100m);
        model.Parameters[1].Name.Should().Be("p2");
        model.Parameters[1].Value.Should().Be(500m);

        var sqlServerSql = MsSqlDialect.Default.Render(model).Sql;
        sqlServerSql.Should().Contain("[credit_limit] BETWEEN @p1 AND @p2");
    }

    [Fact]
    public void Translate_MatchesFullText_ProducesFullTextPredicateNode()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name.MatchesFullText("alice"));
        var model = translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        var fullText = model.Filters[0].Should().BeOfType<FullTextPredicateNode>().Subject;
        fullText.ColumnName.Should().Be("name");
        fullText.ParameterName.Should().Be("p1");

        model.Parameters.Should().HaveCount(1);
        model.Parameters[0].Name.Should().Be("p1");
        model.Parameters[0].Value.Should().Be("alice");

        var sqlServerSql = MsSqlDialect.Default.Render(model).Sql;
        sqlServerSql.Should().Contain("CONTAINS([name], @p1)");
    }

    [Fact]
    public void Translate_InRange_ProducesRangePredicateNode()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(c => c.CreditLimit.InRange(10m, 50m));
        var model = translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        var range = model.Filters[0].Should().BeOfType<RangePredicateNode>().Subject;
        range.ColumnName.Should().Be("credit_limit");
        range.LowerParameterName.Should().Be("p1");
        range.UpperParameterName.Should().Be("p2");

        model.Parameters.Should().HaveCount(2);
        model.Parameters[0].Name.Should().Be("p1");
        model.Parameters[0].Value.Should().Be(10m);
        model.Parameters[1].Name.Should().Be("p2");
        model.Parameters[1].Value.Should().Be(50m);

        var sqlServerSql = MsSqlDialect.Default.Render(model).Sql;
        sqlServerSql.Should().Contain("[credit_limit] >= @p1 AND [credit_limit] <= @p2");
    }

    [Fact]
    public void Translate_WithCursor_ProducesCursorPredicateNodeAndLimit()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.Id)
            .SeekAfter(c => c.Id, 100, 20);

        var model = translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        model.Filters[0].Should().BeOfType<BinaryPredicateNode>();

        var cursorNode = (BinaryPredicateNode)model.Filters[0];
        cursorNode.ColumnName.Should().Be("id");
        cursorNode.Operator.Should().Be(SqlBinaryOperator.GreaterThan);
        model.Take.Should().Be(20);
        model.Parameters.Should().ContainSingle(p => p.Name == cursorNode.ParameterName && Equals(p.Value, 100));

        var sqlServerSql = MsSqlDialect.Default.Render(model).Sql;
        sqlServerSql.Should().Be("SELECT TOP (@_take) * FROM [customers] WHERE [id] > @p1 ORDER BY [id] ASC");
    }

    [Fact]
    public void Translate_UnsupportedBinaryOperator_ThrowsNotSupportedException()
    {
        var mapMethod = typeof(QuerySpecTranslator<Customer>).GetMethod("MapOperator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var act = () => mapMethod.Invoke(null, new object[] { System.Linq.Expressions.ExpressionType.Add });
        act.Should().Throw<System.Reflection.TargetInvocationException>()
            .WithInnerException<NotSupportedException>()
            .WithMessage("*Operator 'Add' cannot be mapped to a SQL operator.*");
    }

    [Fact]
    public void Translate_UnsupportedMethodCall_ThrowsNotSupportedException()
    {
        var method = typeof(string).GetMethod(nameof(string.Equals), [typeof(string)])!;
        var p = System.Linq.Expressions.Expression.Parameter(typeof(Customer), "c");
        var nameProp = System.Linq.Expressions.Expression.Property(p, nameof(Customer.Name));
        var call = System.Linq.Expressions.Expression.Call(nameProp, method, System.Linq.Expressions.Expression.Constant("ALICE"));
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Customer, bool>>(call, p);

        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*is not supported in SQL translation*");
    }

    [Fact]
    public void Translate_BinaryComparisonWithMethodCallOnColumn_ThrowsNotSupportedException()
    {
        var method = typeof(string).GetMethod(nameof(string.ToUpper), Type.EmptyTypes)!;
        var p = System.Linq.Expressions.Expression.Parameter(typeof(Customer), "c");
        var nameProp = System.Linq.Expressions.Expression.Property(p, nameof(Customer.Name));
        var call = System.Linq.Expressions.Expression.Call(nameProp, method);
        var eq = System.Linq.Expressions.Expression.Equal(call, System.Linq.Expressions.Expression.Constant("ALICE"));
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Customer, bool>>(eq, p);

        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*Cannot determine column name from expression*");
    }

    [Fact]
    public void Translate_StringMethodOnNonMember_ThrowsNotSupportedException()
    {
        var method = typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;
        var p = System.Linq.Expressions.Expression.Parameter(typeof(Customer), "c");
        var constStr = System.Linq.Expressions.Expression.Constant("constant_value");
        var arg = System.Linq.Expressions.Expression.Constant("prefix");
        var call = System.Linq.Expressions.Expression.Call(constStr, method, arg);
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Customer, bool>>(call, p);

        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*String method 'StartsWith' on non-member not supported.*");
    }

    [Fact]
    public void Translate_ContainsWithNonMemberItem_ThrowsNotSupportedException()
    {
        var ids = new List<int> { 1, 2, 3 };
        var p = System.Linq.Expressions.Expression.Parameter(typeof(Customer), "c");
        var constantItem = System.Linq.Expressions.Expression.Constant(42);
        var listExpr = System.Linq.Expressions.Expression.Constant(ids);
        var containsMethod = typeof(List<int>).GetMethod(nameof(List<int>.Contains), [typeof(int)])!;
        var call = System.Linq.Expressions.Expression.Call(listExpr, containsMethod, constantItem);
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Customer, bool>>(call, p);

        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*Contains() translation requires a direct member access*");
    }

    [Fact]
    public void Translate_BetweenWithInvalidTarget_ThrowsNotSupportedException()
    {
        var method = typeof(BetweenExtensions).GetMethods().First(m => m.Name == "Between" && m.GetParameters().Length == 3).MakeGenericMethod(typeof(int));
        var call = System.Linq.Expressions.Expression.Call(null, method, System.Linq.Expressions.Expression.Constant(5), System.Linq.Expressions.Expression.Constant(1), System.Linq.Expressions.Expression.Constant(10));
        var p = System.Linq.Expressions.Expression.Parameter(typeof(Customer), "c");
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Customer, bool>>(call, p);

        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*Cannot determine column name from Between() target*");
    }

    [Fact]
    public void Translate_FullTextWithInvalidTarget_ThrowsNotSupportedException()
    {
        var method = typeof(FullTextExtensions).GetMethod("MatchesFullText", new[] { typeof(string), typeof(string) })!;
        var call = System.Linq.Expressions.Expression.Call(null, method, System.Linq.Expressions.Expression.Constant("constant"), System.Linq.Expressions.Expression.Constant("search"));
        var p = System.Linq.Expressions.Expression.Parameter(typeof(Customer), "c");
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Customer, bool>>(call, p);

        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*Cannot determine column name from FullText target*");
    }

    [Fact]
    public void Translate_RangeWithInvalidTarget_ThrowsNotSupportedException()
    {
        var method = typeof(RangeExtensions).GetMethods().First(m => m.Name == "InRange" && m.GetParameters().Length == 3).MakeGenericMethod(typeof(int));
        var call = System.Linq.Expressions.Expression.Call(null, method, System.Linq.Expressions.Expression.Constant(5), System.Linq.Expressions.Expression.Constant(1), System.Linq.Expressions.Expression.Constant(10));
        var p = System.Linq.Expressions.Expression.Parameter(typeof(Customer), "c");
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Customer, bool>>(call, p);

        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*Cannot determine column name from Range target*");
    }

    [Fact]
    public void Translate_UnsupportedExpressionNode_ThrowsNotSupportedException()
    {
        var p = System.Linq.Expressions.Expression.Parameter(typeof(Customer), "c");
        var block = System.Linq.Expressions.Expression.Block(System.Linq.Expressions.Expression.Constant(true));
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Customer, bool>>(block, p);

        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>()
            .WithMessage($"Expression node '{ExpressionType.Block}' ({block.GetType().Name}) is not supported in SQL translation. Expression: {block}");
    }

    [Fact]
    public void Translate_WithCursor_SeekBefore_ProducesLessThan()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.Id)
            .SeekBefore(c => c.Id, 100, 20);

        var model = translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        model.Filters[0].Should().BeOfType<BinaryPredicateNode>();

        var cursorNode = (BinaryPredicateNode)model.Filters[0];
        cursorNode.ColumnName.Should().Be("id");
        cursorNode.Operator.Should().Be(SqlBinaryOperator.LessThan);
        model.Take.Should().Be(20);
        model.Parameters.Should().ContainSingle(p => p.Name == cursorNode.ParameterName && Equals(p.Value, 100));

        var sqlServerSql = MsSqlDialect.Default.Render(model).Sql;
        sqlServerSql.Should().Be("SELECT TOP (@_take) * FROM [customers] WHERE [id] < @p1 ORDER BY [id] ASC");
    }

    [Fact]
    public void Translate_WithCursor_InvalidKeySelector_ThrowsNotSupportedException()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.Id)
            .SeekAfter(c => 123, 100, 20);

        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*Cannot determine column name from cursor key selector*");
    }

    [Fact]
    public void Translate_ContainedByRange_ProducesRangePredicateNode()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(c => RangeExtensions.ContainedByRange(c.CreditLimit, 10m, 50m));
        var model = translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        model.Filters[0].Should().BeOfType<RangePredicateNode>();
    }

    [Fact]
    public void Translate_InvalidOrderSelector_ThrowsNotSupportedException()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.OrderBy(c => 123);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Cannot determine column name from order selector.");
    }

    [Fact]
    public void Translate_InvalidOrderSelector_UnaryNonConvert_ThrowsNotSupportedException()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var p = Expression.Parameter(typeof(Customer), "c");
        var idProp = Expression.Property(p, nameof(Customer.Id));
        var negate = Expression.Negate(idProp);
        var lambda = Expression.Lambda<Func<Customer, object?>>(Expression.Convert(negate, typeof(object)), p);
        var spec = QuerySpec<Customer>.Empty.OrderBy(lambda);
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Cannot determine column name from order selector.");
    }

    [Fact]
    public void Translate_ContainsOnNonMemberItem_ThrowsNotSupportedException()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var ids = new[] { 1, 2, 3 };
        var spec = QuerySpec<Customer>.Empty.Where(c => ids.Contains(10));
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Contains() translation requires a direct member access as the item argument.*");
    }

    [Fact]
    public void Translate_StaticFieldAndProperty_ExtractedCorrectly()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec1 = QuerySpec<Customer>.Empty.Where(c => c.Id == TestStaticHolder.StaticIntField);
        var model1 = translator.Translate(spec1);
        model1.Parameters[0].Value.Should().Be(42);

        var spec2 = QuerySpec<Customer>.Empty.Where(c => c.Id == TestStaticHolder.StaticIntProp);
        var model2 = translator.Translate(spec2);
        model2.Parameters[0].Value.Should().Be(99);
    }

    [Fact]
    public void Translate_ConvertChecked_ExtractedCorrectly()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        int val = 50;
        var p = Expression.Parameter(typeof(Customer), "c");
        var idProp = Expression.Property(p, nameof(Customer.Id));
        var valConstant = Expression.Constant(val);
        var convertChecked = Expression.ConvertChecked(valConstant, typeof(int));
        var eq = Expression.Equal(idProp, convertChecked);
        var lambda = Expression.Lambda<Func<Customer, bool>>(eq, p);

        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var model = translator.Translate(spec);
        model.Parameters[0].Value.Should().Be(50);
    }

    [Fact]
    public void Translate_StringMethodOnNonMember_Literal_ThrowsNotSupportedException()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(c => "literal".Contains("a"));
        var act = () => translator.Translate(spec);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("String method 'Contains' on non-member not supported.");
    }

    [Fact]
    public void Translate_StringMethodUnsupported_ThrowsNotSupportedException()
    {
        var method = typeof(QuerySpecTranslator<Customer>).GetMethod("TranslateStringMethod", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var p = Expression.Parameter(typeof(Customer), "c");
        var nameProp = Expression.Property(p, nameof(Customer.Name));
        var trimMethod = typeof(string).GetMethod(nameof(string.Trim), Type.EmptyTypes)!;
        var call = Expression.Call(nameProp, trimMethod);

        var parameters = new List<SqlParameter>();
        int counter = 0;
        var act = () =>
        {
            try
            {
                object[] args = new object[] { call, parameters, counter };
                method!.Invoke(new QuerySpecTranslator<Customer>("customers"), args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        act.Should().Throw<NotSupportedException>()
            .WithMessage("String method 'Trim' is not supported.");
    }

    [Fact]
    public void Translate_BetweenAndRangeAndFullText_InvalidArguments_ThrowsNotSupportedException()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");

        // Between with missing args
        var translateBetweenMethod = typeof(QuerySpecTranslator<Customer>).GetMethod("TranslateBetween", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dummyMethod = typeof(TestStaticHolder).GetMethod(nameof(TestStaticHolder.DummyMethod))!;
        var emptyCall = Expression.Call(null, dummyMethod);

        var parameters = new List<SqlParameter>();
        int counter = 0;
        var actBetween = () =>
        {
            try
            {
                object[] args = new object[] { emptyCall, parameters, counter };
                translateBetweenMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actBetween.Should().Throw<NotSupportedException>()
            .WithMessage("Between() call could not be parsed:*");

        var p = Expression.Parameter(typeof(Customer), "c");
        var idProp = Expression.Property(p, nameof(Customer.Id));
        var scoreProp = Expression.Property(p, nameof(Customer.Score));

        // Between with 1 static arg
        var dummyMethod1 = typeof(TestStaticHolder).GetMethod(nameof(TestStaticHolder.DummyMethod1))!;
        var call1 = Expression.Call(null, dummyMethod1, Expression.Constant(1));
        var actBetween1 = () =>
        {
            try
            {
                object[] args = new object[] { call1, parameters, counter };
                translateBetweenMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actBetween1.Should().Throw<NotSupportedException>().WithMessage("Between() call could not be parsed:*");

        // Between with 2 static args
        var dummyMethod2 = typeof(TestStaticHolder).GetMethod(nameof(TestStaticHolder.DummyMethod2))!;
        var call2 = Expression.Call(null, dummyMethod2, idProp, Expression.Constant(10));
        var actBetween2 = () =>
        {
            try
            {
                object[] args = new object[] { call2, parameters, counter };
                translateBetweenMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actBetween2.Should().Throw<NotSupportedException>().WithMessage("Between() call could not be parsed:*");

        // Between with instance 0 args
        var dummyInstanceMethod0 = typeof(CustomScore).GetMethod(nameof(CustomScore.InstanceDummy0))!;
        var callInst0 = Expression.Call(scoreProp, dummyInstanceMethod0);
        var actBetweenInst0 = () =>
        {
            try
            {
                object[] args = new object[] { callInst0, parameters, counter };
                translateBetweenMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actBetweenInst0.Should().Throw<NotSupportedException>().WithMessage("Between() call could not be parsed:*");

        // Between with instance 1 arg
        var dummyInstanceMethod1 = typeof(CustomScore).GetMethod(nameof(CustomScore.InstanceDummy1))!;
        var callInst1 = Expression.Call(scoreProp, dummyInstanceMethod1, Expression.Constant(1));
        var actBetweenInst1 = () =>
        {
            try
            {
                object[] args = new object[] { callInst1, parameters, counter };
                translateBetweenMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actBetweenInst1.Should().Throw<NotSupportedException>().WithMessage("Between() call could not be parsed:*");

        // FullText with missing args
        var translateFullTextMethod = typeof(QuerySpecTranslator<Customer>).GetMethod("TranslateFullText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var actFullText = () =>
        {
            try
            {
                object[] args = new object[] { emptyCall, parameters, counter };
                translateFullTextMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actFullText.Should().Throw<NotSupportedException>()
            .WithMessage("MatchesFullText() call could not be parsed:*");

        // FullText with 1 static arg
        var actFullText1 = () =>
        {
            try
            {
                object[] args = new object[] { call1, parameters, counter };
                translateFullTextMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actFullText1.Should().Throw<NotSupportedException>().WithMessage("MatchesFullText() call could not be parsed:*");

        // FullText with instance 0 args
        var actFullTextInst0 = () =>
        {
            try
            {
                object[] args = new object[] { callInst0, parameters, counter };
                translateFullTextMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actFullTextInst0.Should().Throw<NotSupportedException>().WithMessage("MatchesFullText() call could not be parsed:*");

        // Range with missing args
        var translateRangeMethod = typeof(QuerySpecTranslator<Customer>).GetMethod("TranslateRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var actRange = () =>
        {
            try
            {
                object[] args = new object[] { emptyCall, parameters, counter };
                translateRangeMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actRange.Should().Throw<NotSupportedException>()
            .WithMessage("Range call could not be parsed:*");

        // Range with 1 static arg
        var actRange1 = () =>
        {
            try
            {
                object[] args = new object[] { call1, parameters, counter };
                translateRangeMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actRange1.Should().Throw<NotSupportedException>().WithMessage("Range call could not be parsed:*");

        // Range with 2 static args
        var actRange2 = () =>
        {
            try
            {
                object[] args = new object[] { call2, parameters, counter };
                translateRangeMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actRange2.Should().Throw<NotSupportedException>().WithMessage("Range call could not be parsed:*");

        // Range with instance 0 args
        var actRangeInst0 = () =>
        {
            try
            {
                object[] args = new object[] { callInst0, parameters, counter };
                translateRangeMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actRangeInst0.Should().Throw<NotSupportedException>().WithMessage("Range call could not be parsed:*");

        // Range with instance 1 arg
        var actRangeInst1 = () =>
        {
            try
            {
                object[] args = new object[] { callInst1, parameters, counter };
                translateRangeMethod!.Invoke(translator, args);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                throw ex.InnerException!;
            }
        };
        actRangeInst1.Should().Throw<NotSupportedException>().WithMessage("Range call could not be parsed:*");
    }

    [Fact]
    public void Translate_BetweenAndRangeAndFullText_TargetNotResolvable_ThrowsNotSupportedException()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var p = Expression.Parameter(typeof(Customer), "c");

        // Target is constant (123) which does not resolve to a column name
        var betweenMethod = typeof(BetweenExtensions).GetMethods().First(m => m.Name == nameof(BetweenExtensions.Between)).MakeGenericMethod(typeof(int));
        var callBetween = Expression.Call(null, betweenMethod, Expression.Constant(123), Expression.Constant(10), Expression.Constant(20));
        var specBetween = QuerySpec<Customer>.Empty.Where(Expression.Lambda<Func<Customer, bool>>(callBetween, p));
        var actBetween = () => translator.Translate(specBetween);
        actBetween.Should().Throw<NotSupportedException>()
            .WithMessage("Cannot determine column name from Between() target:*");

        var rangeMethod = typeof(RangeExtensions).GetMethods().First(m => m.Name == nameof(RangeExtensions.ContainedByRange)).MakeGenericMethod(typeof(int));
        var callRange = Expression.Call(null, rangeMethod, Expression.Constant(123), Expression.Constant(10), Expression.Constant(20));
        var specRange = QuerySpec<Customer>.Empty.Where(Expression.Lambda<Func<Customer, bool>>(callRange, p));
        var actRange = () => translator.Translate(specRange);
        actRange.Should().Throw<NotSupportedException>()
            .WithMessage("Cannot determine column name from Range target:*");

        var fullTextMethod = typeof(FullTextExtensions).GetMethods().First(m => m.Name == nameof(FullTextExtensions.MatchesFullText));
        var callFullText = Expression.Call(null, fullTextMethod, Expression.Constant("literal"), Expression.Constant("query"));
        var specFullText = QuerySpec<Customer>.Empty.Where(Expression.Lambda<Func<Customer, bool>>(callFullText, p));
        var actFullText = () => translator.Translate(specFullText);
        actFullText.Should().Throw<NotSupportedException>()
            .WithMessage("Cannot determine column name from FullText target:*");
    }

    [Fact]
    public void Translate_IsComparisonOperator_ReturnsFalseForNonComparison()
    {
        var method = typeof(QuerySpecTranslator<Customer>).GetMethod("IsComparisonOperator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        ((bool)method!.Invoke(null, new object[] { ExpressionType.Add })!).Should().BeFalse();
        ((bool)method!.Invoke(null, new object[] { ExpressionType.Multiply })!).Should().BeFalse();
    }

    [Fact]
    public void Translate_InstanceBetween_ProducesBetweenPredicateNode()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Score.Between(10, 20));
        var model = translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        var between = model.Filters[0].Should().BeOfType<BetweenPredicateNode>().Subject;
        between.ColumnName.Should().Be("score");
        between.LowerParameterName.Should().Be("p1");
        between.UpperParameterName.Should().Be("p2");

        model.Parameters.Should().HaveCount(2);
        model.Parameters[0].Name.Should().Be("p1");
        model.Parameters[0].Value.Should().Be(10);
        model.Parameters[1].Name.Should().Be("p2");
        model.Parameters[1].Value.Should().Be(20);
    }

    [Fact]
    public void Translate_InstanceMatchesFullText_ProducesFullTextPredicateNode()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Score.MatchesFullText("sample"));
        var model = translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        var fullText = model.Filters[0].Should().BeOfType<FullTextPredicateNode>().Subject;
        fullText.ColumnName.Should().Be("score");
        fullText.ParameterName.Should().Be("p1");

        model.Parameters.Should().HaveCount(1);
        model.Parameters[0].Name.Should().Be("p1");
        model.Parameters[0].Value.Should().Be("sample");
    }

    [Fact]
    public void Translate_InstanceInRange_ProducesRangePredicateNode()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Score.InRange(10, 20));
        var model = translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        var range = model.Filters[0].Should().BeOfType<RangePredicateNode>().Subject;
        range.ColumnName.Should().Be("score");
        range.LowerParameterName.Should().Be("p1");
        range.UpperParameterName.Should().Be("p2");

        model.Parameters.Should().HaveCount(2);
        model.Parameters[0].Name.Should().Be("p1");
        model.Parameters[0].Value.Should().Be(10);
        model.Parameters[1].Name.Should().Be("p2");
        model.Parameters[1].Value.Should().Be(20);
    }

    [Fact]
    public void Translate_CollectionContainsInstanceMethod_ProducesInPredicateNode()
    {
        var translator = new QuerySpecTranslator<Customer>("customers");
        ICollection<int> list = new List<int> { 1, 2, 3 };
        var spec = QuerySpec<Customer>.Empty.Where(c => list.Contains(c.Id));
        var model = translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        var inNode = model.Filters[0].Should().BeOfType<InPredicateNode>().Subject;
        inNode.ColumnName.Should().Be("id");
    }

    [Fact]
    public void Translate_ExtractConstantValue_AdditionalCases()
    {
        var method = typeof(QuerySpecTranslator<Customer>).GetMethod("ExtractConstantValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        // Instance property on closure object
        var holder = new TestHolderClass { PropVal = 77, FieldVal = 88 };
        var specProp = QuerySpec<Customer>.Empty.Where(c => c.Id == holder.PropVal);
        var translator = new QuerySpecTranslator<Customer>("customers");
        var modelProp = translator.Translate(specProp);
        modelProp.Parameters[0].Value.Should().Be(77);

        // Instance field on closure object
        var specField = QuerySpec<Customer>.Empty.Where(c => c.Id == holder.FieldVal);
        var modelField = translator.Translate(specField);
        modelField.Parameters[0].Value.Should().Be(88);

        // Unary non-convert
        var unaryNegate = Expression.Negate(Expression.Constant(5));
        method.Invoke(null, new object[] { unaryNegate }).Should().BeNull();

        // Field on null instance expression
        var fieldOnNull = Expression.Field(Expression.Constant(null, typeof(TestHolderClass)), nameof(TestHolderClass.FieldVal));
        method.Invoke(null, new object[] { fieldOnNull }).Should().BeNull();

        // Property on null instance expression
        var propOnNull = Expression.Property(Expression.Constant(null, typeof(TestHolderClass)), nameof(TestHolderClass.PropVal));
        method.Invoke(null, new object[] { propOnNull }).Should().BeNull();

        // Write-only property
        var writeOnlyProp = Expression.Property(Expression.Constant(new TestWriteOnlyClass()), nameof(TestWriteOnlyClass.WriteOnlyProp));
        method.Invoke(null, new object[] { writeOnlyProp }).Should().BeNull();
    }

    [Fact]
    public void Translate_RecordsActivity()
    {
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "EricksonLopez.Specification",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(activityListener);

        Activity? capturedActivity = null;
        activityListener.ActivityStarted = a => capturedActivity = a;

        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Id == 1);
        translator.Translate(spec);

        capturedActivity.Should().NotBeNull();
        capturedActivity!.OperationName.Should().Be("QuerySpecTranslator.Translate");
    }

    [Fact]
    public void Translate_RecordsMetrics_OnCacheMissAndCacheHit()
    {
        long translationCount = 0;
        double durationRecorded = 0;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "EricksonLopez.Specification")
                listener.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "specification.sql.translations")
                translationCount += measurement;
        });
        meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "specification.sql.translation.duration")
                durationRecorded += measurement;
        });
        meterListener.Start();

        var translator = new QuerySpecTranslator<Customer>("customers");
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Id == 1);

        // Cache miss
        var before1 = translationCount;
        var durationBefore1 = durationRecorded;
        translator.Translate(spec);
        (translationCount - before1).Should().Be(1);
        (durationRecorded - durationBefore1).Should().BeGreaterThan(0);

        // Cache hit
        var before2 = translationCount;
        var durationBefore2 = durationRecorded;
        translator.Translate(spec);
        (translationCount - before2).Should().Be(1);
        (durationRecorded - durationBefore2).Should().BeGreaterThan(0);
    }
}

public class TestWriteOnlyClass
{
    public int WriteOnlyProp { set { } }
}

public class TestHolderClass
{
    public int FieldVal;
    public int PropVal { get; set; }
}

public static class TestStaticHolder
{
    public static int StaticIntField = 42;
    public static int StaticIntProp => 99;
    public static void DummyMethod() { }
    public static void DummyMethod1(int a) { }
    public static void DummyMethod2(int a, int b) { }
}
