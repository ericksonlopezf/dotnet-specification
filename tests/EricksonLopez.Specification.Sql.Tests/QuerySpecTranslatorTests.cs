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
// Test entities
// ─────────────────────────────────────────────────────────

public struct CustomScore
{
    public int Value { get; set; }
    public bool Between(int a, int b) => true;
    public bool MatchesFullText(string query) => true;
    public bool InRange(int a, int b) => true;
    public bool InstanceDummy0() => true;
    public bool InstanceDummy1(int a) => true;
}

public sealed class Customer
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal CreditLimit { get; init; }
    public string CountryCode { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public CustomScore Score { get; init; }
}

/// <summary>
/// Tests for the <see cref="QuerySpecTranslator{T}"/> — expression to QueryModel translation.
/// </summary>
/// <remarks>
/// Implements <see cref="IDisposable"/> to clear the static <see cref="QueryPlanCache"/> before
/// and after each test. The cache is global state; without isolation, one test that caches
/// a single-criterion plan (e.g. <c>Where(A &amp;&amp; B)</c> → 1 <c>AndPredicateNode</c>) can corrupt
/// a subsequent test that expects a multi-criterion plan (e.g. <c>Where(A).Where(B)</c> → 2 nodes)
/// because both produce the same <c>ExpressionComposer.AndAll</c> cache key.
/// </remarks>
[Collection("QueryPlanCacheCollection")]
public sealed class QuerySpecTranslatorTests : IDisposable
{
    private readonly QuerySpecTranslator<Customer> _translator;

    public QuerySpecTranslatorTests()
    {
        // Clear before each test to prevent cross-test cache contamination.
        QueryPlanCache.Clear();
        _translator = new QuerySpecTranslator<Customer>("customers");
    }

    public void Dispose() => QueryPlanCache.Clear();

    [Fact]
    public void Translate_EmptySpec_ProducesEmptyModel()
    {
        var model = _translator.Translate(QuerySpec<Customer>.Empty);

        model.TableName.Should().Be("customers");
        model.Filters.Should().BeEmpty();
        model.Orders.Should().BeEmpty();
        model.Skip.Should().BeNull();
        model.Take.Should().BeNull();
    }

    [Fact]
    public void Translate_SingleWhere_ProducesOnePredicate()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        var model = _translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.ColumnName.Should().Be("is_active");
        predicate.Operator.Should().Be(SqlBinaryOperator.Equal);
        predicate.ParameterName.Should().Be("p1");
        model.Parameters[0].Value.Should().Be(true);
        model.Parameters[0].Name.Should().Be("p1");
    }

    [Fact]
    public void Translate_GreaterThanComparison_ProducesCorrectOperator()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.CreditLimit > 1000m);
        var model = _translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.ColumnName.Should().Be("credit_limit");
        predicate.Operator.Should().Be(SqlBinaryOperator.GreaterThan);
        predicate.ParameterName.Should().Be("p1");
        model.Parameters[0].Value.Should().Be(1000m);
        model.Parameters[0].Name.Should().Be("p1");
    }

    [Fact]
    public void Translate_WithOrdering_ProducesOrderNode()
    {
        var spec = QuerySpec<Customer>.Empty.OrderBy(c => c.Name);
        var model = _translator.Translate(spec);

        model.Orders.Should().HaveCount(1);
        model.Orders[0].ColumnName.Should().Be("name");
        model.Orders[0].Direction.Should().Be(OrderDirection.Ascending);
    }

    [Fact]
    public void Translate_WithDescendingOrdering_ProducesDescendingDirection()
    {
        var spec = QuerySpec<Customer>.Empty.OrderByDescending(c => c.CreditLimit);
        var model = _translator.Translate(spec);

        model.Orders[0].Direction.Should().Be(OrderDirection.Descending);
    }

    [Fact]
    public void Translate_WithPagination_ProducesSkipAndTake()
    {
        var spec = QuerySpec<Customer>.Empty.Page(2, 25);
        var model = _translator.Translate(spec);

        model.Skip.Should().Be(25);
        model.Take.Should().Be(25);
    }

    [Fact]
    public void Translate_WithDistinct_SetsIsDistinct()
    {
        var spec = QuerySpec<Customer>.Empty.Distinct();
        var model = _translator.Translate(spec);

        model.IsDistinct.Should().BeTrue();
    }

    [Fact]
    public void Translate_MultipleFilters_ProducesMultiplePredicates()
    {
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => !c.IsDeleted);
        var model = _translator.Translate(spec);

        model.Filters.Should().HaveCount(2);

        var filter1 = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        filter1.ColumnName.Should().Be("is_active");
        filter1.Operator.Should().Be(SqlBinaryOperator.Equal);
        filter1.ParameterName.Should().Be("p1");
        model.Parameters[0].Value.Should().Be(true);
        model.Parameters[0].Name.Should().Be("p1");

        var filter2 = model.Filters[1].Should().BeOfType<BinaryPredicateNode>().Subject;
        filter2.ColumnName.Should().Be("is_deleted");
        filter2.Operator.Should().Be(SqlBinaryOperator.Equal);
        filter2.ParameterName.Should().Be("p2");
        model.Parameters[1].Value.Should().Be(false);
        model.Parameters[1].Name.Should().Be("p2");
    }

    [Fact]
    public void Translate_WithNullSpec_ThrowsArgumentNullException()
    {
        var act = () => _translator.Translate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Translate_MultipleOrderings_ProducesMultipleOrders()
    {
        var spec = QuerySpec<Customer>.Empty
            .OrderBy(c => c.CountryCode)
            .ThenByDescending(c => c.CreditLimit);
        var model = _translator.Translate(spec);

        model.Orders.Should().HaveCount(2);
        model.Orders[0].ColumnName.Should().Be("country_code");
        model.Orders[1].ColumnName.Should().Be("credit_limit");
        model.Orders[1].Direction.Should().Be(OrderDirection.Descending);
    }

    [Fact]
    public void Translate_AndAlso_ProducesAndPredicateNode()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive && !c.IsDeleted);
        var model = _translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        model.Filters[0].Should().BeOfType<AndPredicateNode>();
    }

    [Fact]
    public void Translate_OrElse_ProducesOrPredicateNode()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive || c.IsDeleted);
        var model = _translator.Translate(spec);

        model.Filters.Should().HaveCount(1);
        model.Filters[0].Should().BeOfType<OrPredicateNode>();
    }

    [Fact]
    public void Translate_FlippedOperator_FlipsOperatorProperly()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => 1000m < c.CreditLimit);
        var model = _translator.Translate(spec);

        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.ColumnName.Should().Be("credit_limit");
        predicate.Operator.Should().Be(SqlBinaryOperator.GreaterThan); // Flipped from LessThan
    }

    [Fact]
    public void Translate_FlippedOperatorGreaterThanOrEqual_FlipsOperatorProperly()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => 1000m >= c.CreditLimit);
        var model = _translator.Translate(spec);

        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.ColumnName.Should().Be("credit_limit");
        predicate.Operator.Should().Be(SqlBinaryOperator.LessThanOrEqual); // Flipped from GreaterThanOrEqual
    }

    [Fact]
    public void Translate_FlippedOperatorLessThanOrEqual_FlipsOperatorProperly()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => 1000m <= c.CreditLimit);
        var model = _translator.Translate(spec);

        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.ColumnName.Should().Be("credit_limit");
        predicate.Operator.Should().Be(SqlBinaryOperator.GreaterThanOrEqual); // Flipped from LessThanOrEqual
    }

    [Fact]
    public void Translate_FlippedOperatorGreaterThan_FlipsOperatorProperly()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => 1000m > c.CreditLimit);
        var model = _translator.Translate(spec);

        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.ColumnName.Should().Be("credit_limit");
        predicate.Operator.Should().Be(SqlBinaryOperator.LessThan); // Flipped from GreaterThan
    }

    [Fact]
    public void Translate_FlippedOperatorEqual_KeepsOperatorProperly()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => 1000m == c.CreditLimit);
        var model = _translator.Translate(spec);

        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.ColumnName.Should().Be("credit_limit");
        predicate.Operator.Should().Be(SqlBinaryOperator.Equal);
    }

    [Fact]
    public void Translate_LessThanOrEqual_Operator()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.CreditLimit <= 100m);
        var model = _translator.Translate(spec);
        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.Operator.Should().Be(SqlBinaryOperator.LessThanOrEqual);
    }

    [Fact]
    public void Translate_MethodCall_UnsupportedMethod_Throws()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name.ToUpper() == "TEST");
        var act = () => _translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*ToUpper*");
    }

    private static class StaticHelpers
    {
        public static string StaticField = "test";
        public static string StaticProperty { get; set; } = "test";
    }

    private class InstanceHelpers
    {
        public string InstanceField = "test";
        public string InstanceProperty { get; set; } = "test";
    }

    [Fact]
    public void Translate_ExtractClosureStaticField_GetsValue()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name == StaticHelpers.StaticField);
        var model = _translator.Translate(spec);
        model.Parameters[0].Value.Should().Be("test");
    }

    [Fact]
    public void Translate_ExtractClosureStaticProperty_GetsValue()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name == StaticHelpers.StaticProperty);
        var model = _translator.Translate(spec);
        model.Parameters[0].Value.Should().Be("test");
    }

    [Fact]
    public void Translate_ExtractClosureInstanceField_GetsValue()
    {
        var localVariable = "test";
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name == localVariable);
        var model = _translator.Translate(spec);
        model.Parameters[0].Value.Should().Be("test");
    }

    [Fact]
    public void Translate_ExtractClosureInstanceProperty_GetsValue()
    {
        var param = System.Linq.Expressions.Expression.Parameter(typeof(Customer), "c");
        var prop = System.Linq.Expressions.Expression.Property(param, "Name");
        var instanceConstant = System.Linq.Expressions.Expression.Constant(new InstanceHelpers());
        var right = System.Linq.Expressions.Expression.Property(instanceConstant, "InstanceProperty");
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Customer, bool>>(
            System.Linq.Expressions.Expression.Equal(prop, right), param);

        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var model = _translator.Translate(spec);
        model.Parameters[0].Value.Should().Be("test");
    }

    [Fact]
    public void Translate_ConvertNodeInExtraction_RemovesConvert()
    {
        // Enums or struct wrapping often adds a Convert node (e.g. object == enum)
        var spec = QuerySpec<Customer>.Empty.Where(c => (object)c.Name == (object)"test");
        var model = _translator.Translate(spec);
        model.Filters.Should().HaveCount(1);
    }

    [Fact]
    public void Translate_InvalidExpressionType_ThrowsNotSupported()
    {
        var param = System.Linq.Expressions.Expression.Parameter(typeof(Customer), "c");
        var prop = System.Linq.Expressions.Expression.Property(param, "CreditLimit");
        var constant = System.Linq.Expressions.Expression.Constant(100m);
        var body = System.Linq.Expressions.Expression.Add(prop, constant);

        var lambda = System.Linq.Expressions.Expression.Lambda<Func<Customer, bool>>(
            System.Linq.Expressions.Expression.Equal(body, constant), param);

        var spec = QuerySpec<Customer>.Empty.Where(lambda);
        var act = () => _translator.Translate(spec);
        act.Should().Throw<NotSupportedException>();
    }
}

/// <summary>
/// Tests for the <see cref="MsSqlDialect"/> — SQL Server T-SQL rendering.
/// </summary>
public sealed class MsSqlDialectTests : IDisposable
{
    private readonly MsSqlDialect _dialect = MsSqlDialect.Default;
    private readonly QuerySpecTranslator<Customer> _translator;

    public MsSqlDialectTests()
    {
        QueryPlanCache.Clear();
        _translator = new QuerySpecTranslator<Customer>("customers");
    }

    public void Dispose() => QueryPlanCache.Clear();

    [Fact]
    public void Render_EmptyModel_ProducesSelectStar()
    {
        var model = _translator.Translate(QuerySpec<Customer>.Empty);
        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM [customers]");
        query.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Render_QueryTypeCount_ProducesSelectCount()
    {
        var model = new QueryModel { TableName = "t", QueryType = SqlQueryType.Count };
        var query = _dialect.Render(model);
        query.Sql.Should().StartWith("SELECT COUNT(*) FROM [t]");
    }

    [Fact]
    public void Render_QueryTypeExists_ProducesSelect1()
    {
        var model = new QueryModel { TableName = "t", QueryType = SqlQueryType.Exists };
        var query = _dialect.Render(model);
        query.Sql.Should().StartWith("SELECT 1 FROM [t]");
    }

    [Fact]
    public void QuoteIdentifier_WrapsWithSquareBrackets()
    {
        _dialect.QuoteIdentifier("my_table").Should().Be("[my_table]");
    }

    [Fact]
    public void QuoteIdentifier_EscapesInternalClosingBracket()
    {
        _dialect.QuoteIdentifier("bad]name").Should().Be("[bad]]name]");
    }

    [Fact]
    public void Render_WithFilter_ProducesWhereClause()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("WHERE");
        query.Sql.Should().Contain("[is_active]");
    }

    [Fact]
    public void Render_TakeOnly_ProducesTopN_NotOffsetFetch()
    {
        var spec = QuerySpec<Customer>.Empty.Take(10);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("TOP (@_take)");
        query.Sql.Should().NotContain("OFFSET");
        query.Parameters["_take"].Should().Be(10);
    }

    [Fact]
    public void Render_SkipAndTake_ProducesOffsetFetch()
    {
        var spec = QuerySpec<Customer>.Empty.OrderBy(c => c.Id).Page(2, 10);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("OFFSET @_skip ROWS");
        query.Sql.Should().Contain("FETCH NEXT @_take ROWS ONLY");
        query.Sql.Should().NotContain("TOP");
    }

    [Fact]
    public void Render_SkipWithoutOrderBy_AddsOrderBySelectNull()
    {
        var spec = QuerySpec<Customer>.Empty.Page(2, 10);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("ORDER BY (SELECT NULL)");
        query.Sql.Should().Contain("OFFSET @_skip ROWS");
    }

    [Fact]
    public void Render_WithOrdering_ProducesOrderByWithBrackets()
    {
        var spec = QuerySpec<Customer>.Empty.OrderBy(c => c.Name);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("ORDER BY [name] ASC");
    }

    [Fact]
    public void ListContains_MsSqlRendering_ProducesInClause()
    {
        var ids = new List<int> { 5, 10, 15 };
        var spec = QuerySpec<Customer>.Empty.Where(c => ids.Contains(c.Id));
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("[id] IN (@p1_0, @p1_1, @p1_2)");
        query.Parameters.Should().ContainKey("p1_0");
        query.Parameters["p1_0"].Should().Be(5);
    }

    [Fact]
    public void Render_EmptyCollectionInPredicate_ProducesAlwaysFalse()
    {
        var ids = new List<int>();
        var spec = QuerySpec<Customer>.Empty.Where(c => ids.Contains(c.Id));
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("1 = 0");
    }

    [Fact]
    public void Render_AllParametersNamedNotInterpolated()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.CreditLimit > 9999.99m);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().NotContain("9999.99", "values must be parameterized");
        query.Parameters.Should().ContainValue(9999.99m);
    }

    [Fact]
    public void Render_MultipleFilters_ProducesWhereClauseWithAnd()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive).Where(c => c.Name != null);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("WHERE [is_active] = @p1 AND [name] IS NOT NULL");
    }

    [Fact]
    public void Render_MultipleOrders_ProducesOrderByClauseWithComma()
    {
        var spec = QuerySpec<Customer>.Empty.OrderBy(c => c.Name).ThenByDescending(c => c.CreditLimit);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("ORDER BY [name] ASC, [credit_limit] DESC");
    }

    [Fact]
    public void Render_WithProjections_ProducesSelectColumns()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Projections = ["Id", "Name"]
        };
        var query = _dialect.Render(model);

        query.Sql.Should().StartWith("SELECT [Id], [Name] FROM");
    }

    [Fact]
    public void Render_TakeWithoutSkip_ProducesTop()
    {
        var spec = QuerySpec<Customer>.Empty.Take(10);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("SELECT TOP (@_take) * FROM");
        query.Sql.Should().NotContain("OFFSET");
        query.Parameters["_take"].Should().Be(10);
    }

    [Fact]
    public void Render_SkipWithoutTake_ProducesOffsetFetch()
    {
        var spec = QuerySpec<Customer>.Empty.Skip(5);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("OFFSET @_skip ROWS");
        query.Sql.Should().NotContain("FETCH NEXT");
        query.Parameters["_skip"].Should().Be(5);
    }

    [Fact]
    public void Render_InPredicateEmpty_ProducesFalse()
    {
        var emptyList = new List<string>();
        var spec = QuerySpec<Customer>.Empty.Where(c => emptyList.Contains(c.Name));
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("WHERE 1 = 0");
    }

    [Fact]
    public void Render_NotInPredicateEmpty_ProducesTrue()
    {
        var emptyList = new List<string>();
        var spec = QuerySpec<Customer>.Empty.Where(c => !emptyList.Contains(c.Name));
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("NOT (1 = 0)");
    }

    [Fact]
    public void Translate_UnsupportedExpression_ThrowsNotSupportedException()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => Math.Abs(c.Id) == 1);
        var act = () => _translator.Translate(spec);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Translate_ComparisonWithoutColumn_ThrowsNotSupportedException()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => 1 == 1);
        var act = () => _translator.Translate(spec);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Translate_IsNullComparison_TranslatesToIsNullOperator()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name == null);
        var model = _translator.Translate(spec);

        var filter = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        filter.Operator.Should().Be(SqlBinaryOperator.IsNull);
        filter.ParameterName.Should().BeEmpty();
    }

    [Fact]
    public void Translate_IsNotNullComparison_TranslatesToIsNotNullOperator()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name != null);
        var model = _translator.Translate(spec);

        var filter = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        filter.Operator.Should().Be(SqlBinaryOperator.IsNotNull);
        filter.ParameterName.Should().BeEmpty();
    }

    [Fact]
    public void Translate_LessThanOrEqualComparison_Reversed()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => 1000m <= c.CreditLimit);
        var model = _translator.Translate(spec);

        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.ColumnName.Should().Be("credit_limit");
        predicate.Operator.Should().Be(SqlBinaryOperator.GreaterThanOrEqual);
        predicate.ParameterName.Should().Be("p1");
        model.Parameters[0].Value.Should().Be(1000m);
    }

    [Fact]
    public void Translate_StringMethodOnNonMember_ThrowsNotSupportedException()
    {
        var someString = "test";
        var spec = QuerySpec<Customer>.Empty.Where(c => someString.Contains(c.Name));
        var act = () => _translator.Translate(spec);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Translate_UnsupportedStringMethod_ThrowsNotSupportedException()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name.Equals("test"));
        var act = () => _translator.Translate(spec);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Translate_ContainsOnNonMember_ThrowsNotSupportedException()
    {
        var ids = new[] { 1, 2, 3 };
        var spec = QuerySpec<Customer>.Empty.Where(c => ids.Contains(1));
        var act = () => _translator.Translate(spec);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Translate_OrderSelectorWithoutColumn_ThrowsNotSupportedException()
    {
        var spec = QuerySpec<Customer>.Empty.OrderBy(c => 1);
        var act = () => _translator.Translate(spec);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Translate_StaticProperty_EvaluatesCorrectly()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name == DateTime.Now.ToString());
        var model = _translator.Translate(spec);
        model.Filters.Should().HaveCount(1);
    }

    [Fact]
    public void Translate_BinaryComparisonWithoutColumn_ThrowsNotSupportedException()
    {
        var one = 1;
        var two = 2;
        var spec = QuerySpec<Customer>.Empty.Where(c => one == two);
        var act = () => _translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*Cannot determine column name*");
    }

    [Fact]
    public void Translate_UnsupportedMethodCall_ThrowsNotSupportedException()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name.ToUpper() == "A");
        var act = () => _translator.Translate(spec);
        act.Should().Throw<NotSupportedException>().WithMessage("*ToUpper*");
    }

    [Fact]
    public void DialectName_IsSqlServer()
    {
        _dialect.DialectName.Should().Be("SQL Server");
    }

    [Fact]
    public void ParameterPrefix_IsAtSign()
    {
        _dialect.ParameterPrefix.Should().Be("@");
    }

    [Fact]
    public void QuoteIdentifier_NullOrWhitespace_ThrowsArgumentException()
    {
        var act = () => _dialect.QuoteIdentifier(" ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Render_NullModel_ThrowsArgumentNullException()
    {
        var act = () => _dialect.Render(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Render_WithTableAlias_ProducesAsAlias()
    {
        var model = new QueryModel
        {
            TableName = "Products",
            TableAlias = "p"
        };
        var result = _dialect.Render(model);
        result.Sql.Should().Contain("FROM [Products] AS [p]");
    }

    [Fact]
    public void Render_AllOperators_ProduceCorrectSql()
    {
        var spec = QuerySpec<Customer>.Empty
            .Where(p => p.CreditLimit < 100m)
            .Where(p => p.CreditLimit <= 100m)
            .Where(p => p.CreditLimit > 10m)
            .Where(p => p.CreditLimit >= 10m)
            .Where(p => p.CreditLimit != 50m)
            .Where(p => p.Name.StartsWith("W"))
            .Where(p => p.Name.EndsWith("Z"))
            .Where(p => p.Name.Contains("Mid"));

        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("[credit_limit] < @p1");
        result.Sql.Should().Contain("[credit_limit] <= @p2");
        result.Sql.Should().Contain("[credit_limit] > @p3");
        result.Sql.Should().Contain("[credit_limit] >= @p4");
        result.Sql.Should().Contain("[credit_limit] <> @p5");
        result.Sql.Should().Contain("[name] LIKE @p6");
        result.Sql.Should().Contain("[name] LIKE @p7");
        result.Sql.Should().Contain("[name] LIKE @p8");

        result.Parameters["p6"].Should().Be("W%");
        result.Parameters["p7"].Should().Be("%Z");
        result.Parameters["p8"].Should().Be("%Mid%");
    }

    [Fact]
    public void Render_RawPredicateNode_ProducesRawSql()
    {
        var model = new QueryModel
        {
            TableName = "T",
            Filters = [new RawPredicateNode("1 = 1")]
        };
        var result = _dialect.Render(model);
        result.Sql.Should().Contain("1 = 1");
    }

    [Fact]
    public void Render_AndPredicateNode_ProducesAndClause()
    {
        var model = new QueryModel
        {
            TableName = "T",
            Filters = [new AndPredicateNode(
                new BinaryPredicateNode("A", SqlBinaryOperator.Equal, "p1"),
                new BinaryPredicateNode("B", SqlBinaryOperator.Equal, "p2")
            )]
        };
        var result = _dialect.Render(model);
        result.Sql.Should().Contain("([A] = @p1 AND [B] = @p2)");
    }

    [Fact]
    public void Render_OrPredicateNode_ProducesOrClause()
    {
        var model = new QueryModel
        {
            TableName = "T",
            Filters = [new OrPredicateNode(
                new BinaryPredicateNode("A", SqlBinaryOperator.Equal, "p1"),
                new BinaryPredicateNode("B", SqlBinaryOperator.Equal, "p2")
            )]
        };
        var result = _dialect.Render(model);
        result.Sql.Should().Contain("([A] = @p1 OR [B] = @p2)");
    }

    [Fact]
    public void Render_NotPredicateNode_ProducesNotClause()
    {
        var model = new QueryModel
        {
            TableName = "T",
            Filters = [new NotPredicateNode(
                new BinaryPredicateNode("A", SqlBinaryOperator.Equal, "p1")
            )]
        };
        var result = _dialect.Render(model);
        result.Sql.Should().Contain("NOT ([A] = @p1)");
    }

    [Fact]
    public void Render_InPredicateNode_Negated_ProducesNotIn()
    {
        var model = new QueryModel
        {
            TableName = "T",
            Filters = [new InPredicateNode("A", "p1") { Negated = true }],
            Parameters = [new SqlParameter("p1", new int[] { 1, 2 })]
        };
        var result = _dialect.Render(model);
        result.Sql.Should().Contain("[A] NOT IN (@p1_0, @p1_1)");
    }

    [Fact]
    public void Render_InPredicateNode_NegatedEmpty_ProducesAlwaysTrue()
    {
        var model = new QueryModel
        {
            TableName = "T",
            Filters = [new InPredicateNode("A", "p1") { Negated = true }],
        };
        var result = _dialect.Render(model);
        result.Sql.Should().Contain("1 = 1");
    }
}

/// <summary>
/// Tests for <see cref="SnakeCaseColumnNameResolver"/>.
/// </summary>
public sealed class SnakeCaseColumnNameResolverTests
{
    private readonly SnakeCaseColumnNameResolver _resolver = SnakeCaseColumnNameResolver.Default;

    [Theory]
    [InlineData("IsActive", "is_active")]
    [InlineData("CreditLimit", "credit_limit")]
    [InlineData("CountryCode", "country_code")]
    [InlineData("CustomerId", "customer_id")]
    [InlineData("Id", "id")]
    [InlineData("Name", "name")]
    [InlineData("CreatedAt", "created_at")]
    public void Resolve_PascalCase_ProducesSnakeCase(string input, string expected)
    {
        _resolver.Resolve(input).Should().Be(expected);
    }

    [Fact]
    public void Resolve_WithNullOrWhitespace_ThrowsArgumentException()
    {
        var act = () => _resolver.Resolve(null!);
        act.Should().Throw<ArgumentException>();
    }
}

// ─────────────────────────────────────────────────────────
/// <summary>
/// Tests verifying that string LIKE predicates produce the correct SQL LIKE patterns.
/// </summary>
public sealed class LikePredicateTests : IDisposable
{
    private readonly QuerySpecTranslator<Customer> _translator;
    private readonly MsSqlDialect _dialect = MsSqlDialect.Default;

    public LikePredicateTests()
    {
        QueryPlanCache.Clear();
        _translator = new QuerySpecTranslator<Customer>("customers");
    }

    public void Dispose() => QueryPlanCache.Clear();

    [Fact]
    public void StartsWith_ProducesArgPercentPattern_NotPercentArgPercent()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name.StartsWith("Acme"));
        var model = _translator.Translate(spec);

        model.Parameters.Should().HaveCount(1);
        model.Parameters[0].Name.Should().Be("p1");
        model.Parameters[0].Value.Should().Be("Acme%",
            because: "StartsWith should use 'arg%' pattern, not '%arg%'");

        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.Operator.Should().Be(SqlBinaryOperator.LikeStartsWith);
        predicate.ParameterName.Should().Be("p1");
    }

    [Fact]
    public void EndsWith_ProducesPercentArgPattern_NotPercentArgPattern()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name.EndsWith(".com"));
        var model = _translator.Translate(spec);

        model.Parameters.Should().HaveCount(1);
        model.Parameters[0].Name.Should().Be("p1");
        model.Parameters[0].Value.Should().Be("%.com",
            because: "EndsWith should use '%arg' pattern, not '%arg%'");

        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.Operator.Should().Be(SqlBinaryOperator.LikeEndsWith);
        predicate.ParameterName.Should().Be("p1");
    }

    [Fact]
    public void Contains_ProducesPercentArgPercentPattern()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name.Contains("corp"));
        var model = _translator.Translate(spec);

        model.Parameters.Should().HaveCount(1);
        model.Parameters[0].Name.Should().Be("p1");
        model.Parameters[0].Value.Should().Be("%corp%",
            because: "Contains should use '%arg%' wrapping pattern");

        var predicate = model.Filters[0].Should().BeOfType<BinaryPredicateNode>().Subject;
        predicate.Operator.Should().Be(SqlBinaryOperator.Like);
        predicate.ParameterName.Should().Be("p1");
    }

    [Fact]
    public void StartsWith_MsSqlRendering_ProducesLikeKeyword()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name.StartsWith("Acme"));
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("LIKE @p1");
        query.Parameters["p1"].Should().Be("Acme%");
    }

    [Fact]
    public void EndsWith_MsSqlRendering_ProducesCorrectSql()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name.EndsWith(".io"));
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("LIKE @p1");
        query.Parameters["p1"].Should().Be("%.io");
    }

    [Fact]
    public void AllThreeMethods_ProduceDifferentParameterValues()
    {
        var containsSpec = QuerySpec<Customer>.Empty.Where(c => c.Name.Contains("x"));
        var startsSpec = QuerySpec<Customer>.Empty.Where(c => c.Name.StartsWith("x"));
        var endsSpec = QuerySpec<Customer>.Empty.Where(c => c.Name.EndsWith("x"));

        var containsModel = _translator.Translate(containsSpec);
        var startsModel = _translator.Translate(startsSpec);
        var endsModel = _translator.Translate(endsSpec);

        containsModel.Parameters[0].Value.Should().Be("%x%");
        startsModel.Parameters[0].Value.Should().Be("x%");
        endsModel.Parameters[0].Value.Should().Be("%x");
    }
}
