// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Specification.PostgreSql;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.PostgreSql.Tests;

/// <summary>
/// Domain model used in PostgreSQL dialect tests.
/// </summary>
public sealed class Product
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal Price { get; init; }
    public string Category { get; init; } = string.Empty;
}

/// <summary>
/// Unit tests for <see cref="PostgreSqlDialect"/>.
/// </summary>
public sealed class PostgreSqlDialectTests
{
    private readonly PostgreSqlDialect _dialect = PostgreSqlDialect.Default;
    // Use VerbatimColumnNameResolver so column names match C# property names exactly
    private readonly QuerySpecTranslator<Product> _translator =
        new("Products", VerbatimColumnNameResolver.Default);

    [Fact]
    public void DialectName_IsPostgreSql()
    {
        _dialect.DialectName.Should().Be("PostgreSQL");
    }

    [Fact]
    public void ParameterPrefix_IsAtSign()
    {
        _dialect.ParameterPrefix.Should().Be("@");
    }

    [Fact]
    public void QuoteIdentifier_WrapsInDoubleQuotes()
    {
        _dialect.QuoteIdentifier("Name").Should().Be("\"Name\"");
    }

    [Fact]
    public void QuoteIdentifier_EscapesInternalDoubleQuotes()
    {
        _dialect.QuoteIdentifier("weird\"column").Should().Be("\"weird\"\"column\"");
    }

    [Fact]
    public void Render_EmptyModel_ProducesSelectStar()
    {
        var model = _translator.Translate(QuerySpec<Product>.Empty);
        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"Products\"");
        query.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Render_QueryTypeCount_ProducesSelectCount()
    {
        var model = new QueryModel { TableName = "t", QueryType = SqlQueryType.Count };
        var query = _dialect.Render(model);
        query.Sql.Should().StartWith("SELECT COUNT(*) FROM \"t\"");
    }

    [Fact]
    public void Render_QueryTypeExists_ProducesSelect1()
    {
        var model = new QueryModel { TableName = "t", QueryType = SqlQueryType.Exists };
        var query = _dialect.Render(model);
        query.Sql.Should().StartWith("SELECT 1 FROM \"t\"");
    }

    [Fact]
    public void Render_SimpleWhereSpec_ProducesWhereClause()
    {
        var spec = QuerySpec<Product>.Empty.Where(p => p.IsActive);
        var model = _translator.Translate(spec);

        var result = _dialect.Render(model);

        result.Sql.Should().Contain("WHERE \"IsActive\" = @p1");
        result.Parameters.Should().ContainKey("p1").WhoseValue.Should().Be(true);
    }

    [Fact]
    public void Render_WithPagination_ProducesLimitOffset()
    {
        var spec = QuerySpec<Product>.Empty
            .Where(p => p.IsActive)
            .Skip(10)
            .Take(20);

        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("LIMIT @_take");
        result.Sql.Should().Contain("OFFSET @_skip");
        result.Parameters.Should().ContainKey("_take").WhoseValue.Should().Be(20);
        result.Parameters.Should().ContainKey("_skip").WhoseValue.Should().Be(10);
    }

    [Fact]
    public void Render_WithTakeOnly_ProducesLimitWithoutOffset()
    {
        var spec = QuerySpec<Product>.Empty.Take(5);
        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("LIMIT @_take");
        result.Sql.Should().NotContain("OFFSET");
        result.Parameters.Should().ContainKey("_take").WhoseValue.Should().Be(5);
        result.Parameters.Should().NotContainKey("_skip");
    }

    [Fact]
    public void Render_WithSkipOnly_ProducesOffsetWithoutLimit()
    {
        var spec = QuerySpec<Product>.Empty.Skip(15);
        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("OFFSET @_skip");
        result.Sql.Should().NotContain("LIMIT");
        result.Parameters.Should().ContainKey("_skip").WhoseValue.Should().Be(15);
        result.Parameters.Should().NotContainKey("_take");
    }

    [Fact]
    public void Render_WithOrdering_ProducesOrderBy()
    {
        var spec = QuerySpec<Product>.Empty
            .OrderBy(p => p.Price)
            .ThenByDescending(p => p.Name);

        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("ORDER BY \"Price\" ASC, \"Name\" DESC");
    }

    [Fact]
    public void Render_MultipleFilters_ProducesWhereClauseWithAnd()
    {
        var spec = QuerySpec<Product>.Empty.Where(p => p.IsActive).Where(p => p.Price > 10m);
        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("WHERE \"IsActive\" = @p1 AND \"Price\" > @p2");
    }

    [Fact]
    public void Render_WithProjections_ProducesSelectColumns()
    {
        var model = new QueryModel
        {
            TableName = "Products",
            Projections = ["Id", "Name"]
        };
        var result = _dialect.Render(model);

        result.Sql.Should().StartWith("SELECT \"Id\", \"Name\" FROM");
    }

    [Fact]
    public void Render_WithDistinct_ProducesDistinctKeyword()
    {
        var model = new QueryModel
        {
            TableName = "Products",
            IsDistinct = true
        };
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("SELECT DISTINCT");
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

        result.Sql.Should().Contain("FROM \"Products\" AS \"p\"");
    }

    [Fact]
    public void QuoteIdentifier_NullOrWhitespace_ThrowsArgumentException()
    {
        var act = () => _dialect.QuoteIdentifier(" ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Render_WithInPredicate_ProducesExpandedInClause()
    {
        var categories = new[] { "Electronics", "Books", "Clothing" };
        var model = new QueryModel
        {
            TableName = "Products",
            Filters = [new InPredicateNode("Category", "p1")],
            Parameters = [new SqlParameter("p1", categories)]
        };

        var result = _dialect.Render(model);

        result.Sql.Should().Contain("\"Category\" = ANY(@p1)");
        result.Parameters.Should().ContainKey("p1").WhoseValue.Should().BeEquivalentTo(categories);
    }

    [Fact]
    public void Render_EmptyCollectionInPredicate_ProducesAlwaysFalse()
    {
        var empty = Array.Empty<string>();
        var spec = QuerySpec<Product>.Empty.Where(p => empty.Contains(p.Category));

        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("\"Category\" = ANY(@p1)");
    }

    [Fact]
    public void Render_AllOperators_ProduceCorrectSql()
    {
        var spec = QuerySpec<Product>.Empty
            .Where(p => p.Price < 100m)
            .Where(p => p.Price <= 100m)
            .Where(p => p.Price >= 10m)
            .Where(p => p.Price != 50m);

        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("\"Price\" < @p1");
        result.Sql.Should().Contain("\"Price\" <= @p2");
        result.Sql.Should().Contain("\"Price\" >= @p3");
        result.Sql.Should().Contain("\"Price\" <> @p4");
    }

    [Fact]
    public void Render_EmptyCollectionNotInPredicate_ProducesAlwaysTrue()
    {
        var empty = Array.Empty<string>();
        var spec = QuerySpec<Product>.Empty.Where(p => !empty.Contains(p.Category));

        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("NOT (\"Category\" = ANY(@p1))");
    }

    [Fact]
    public void Render_WithLikePredicate_ProducesLikeNotIlike()
    {
        var spec = QuerySpec<Product>.Empty.Where(p => p.Name.StartsWith("Widget"));

        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().Contain("LIKE");
        result.Sql.Should().NotContain("ILIKE");
    }

    [Fact]
    public void Render_ComplexSpec_ProducesFullQuery()
    {
        var categories = new[] { "A", "B" };
        var spec = QuerySpec<Product>.Empty
            .Where(p => p.IsActive)
            .Where(p => p.Price > 10m)
            .Where(p => categories.Contains(p.Category))
            .OrderByDescending(p => p.Price)
            .Skip(0)
            .Take(50);

        var model = _translator.Translate(spec);
        var result = _dialect.Render(model);

        result.Sql.Should().StartWith("SELECT *");
        result.Sql.Should().Contain("FROM \"Products\"");
        result.Sql.Should().Contain("WHERE");
        result.Sql.Should().Contain("ORDER BY");
        result.Sql.Should().Contain("LIMIT");
        result.Sql.Should().Contain("OFFSET");
    }

    [Fact]
    public void Render_NullModel_ThrowsArgumentNullException()
    {
        var act = () => _dialect.Render(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Default_Singleton_IsNotNull()
    {
        PostgreSqlDialect.Default.Should().NotBeNull();
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
        result.Sql.Should().Contain("(\"A\" = @p1 AND \"B\" = @p2)");
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
        result.Sql.Should().Contain("(\"A\" = @p1 OR \"B\" = @p2)");
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
        result.Sql.Should().Contain("NOT (\"A\" = @p1)");
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
        result.Sql.Should().Contain("NOT (\"A\" = ANY(@p1))");
    }

    [Fact]
    public void Render_InPredicateNode_NegatedEmpty_ProducesAlwaysTrue()
    {
        var model = new QueryModel
        {
            TableName = "T",
            Filters = [new InPredicateNode("A", "p1") { Negated = true }],
            // no parameters provided for p1
        };
        var query = _dialect.Render(model);

        query.Sql.Should().Contain("NOT (\"A\" = ANY(@p1))");
    }

    [Fact]
    public void Render_BetweenPredicate_NegatedAndNonNegated_RendersCorrectly()
    {
        var model = new QueryModel
        {
            TableName = "products",
            Filters = [
                new BetweenPredicateNode("price", "p1", "p2") { Negated = false },
                new BetweenPredicateNode("weight", "p3", "p4") { Negated = true }
            ],
            Parameters = [
                new SqlParameter("p1", 10), new SqlParameter("p2", 50),
                new SqlParameter("p3", 1), new SqlParameter("p4", 5)
            ]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Contain("\"price\" BETWEEN @p1 AND @p2");
        query.Sql.Should().Contain("\"weight\" NOT BETWEEN @p3 AND @p4");
    }

    [Fact]
    public void Render_FullTextPredicate_RendersCorrectly()
    {
        var model = new QueryModel
        {
            TableName = "articles",
            Filters = [new FullTextPredicateNode("body", "p1", "english")],
            Parameters = [new SqlParameter("p1", "postgres")]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"articles\" WHERE to_tsvector('english', \"body\") @@ plainto_tsquery('english', @p1)");
    }

    [Fact]
    public void Render_RangePredicate_RendersCorrectly()
    {
        var model = new QueryModel
        {
            TableName = "events",
            Filters = [new RangePredicateNode("age", "p1", "p2")],
            Parameters = [new SqlParameter("p1", 18), new SqlParameter("p2", 65)]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"events\" WHERE \"age\" <@ int4range(@p1, @p2, '[]')");
    }

    [Fact]
    public void Render_RawPredicateNode_RendersDirectSql()
    {
        var model = new QueryModel
        {
            TableName = "products",
            Filters = [new RawPredicateNode("EXISTS (SELECT 1 FROM Other)")]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"products\" WHERE EXISTS (SELECT 1 FROM Other)");
    }

    [Fact]
    public void Render_UnknownPredicateNode_ThrowsNotSupportedException()
    {
        var model = new QueryModel
        {
            TableName = "products",
            Filters = [new CustomUnknownSqlPredicateNode()]
        };

        var act = () => _dialect.Render(model);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Unknown predicate node type: CustomUnknownSqlPredicateNode*");
    }

    [Theory]
    [InlineData(SqlBinaryOperator.Equal, false, " = @p1")]
    [InlineData(SqlBinaryOperator.NotEqual, false, " <> @p1")]
    [InlineData(SqlBinaryOperator.GreaterThan, false, " > @p1")]
    [InlineData(SqlBinaryOperator.GreaterThanOrEqual, false, " >= @p1")]
    [InlineData(SqlBinaryOperator.LessThan, false, " < @p1")]
    [InlineData(SqlBinaryOperator.LessThanOrEqual, false, " <= @p1")]
    [InlineData(SqlBinaryOperator.Like, false, " LIKE @p1")]
    [InlineData(SqlBinaryOperator.Like, true, " ILIKE @p1")]
    [InlineData(SqlBinaryOperator.LikeStartsWith, false, " LIKE @p1")]
    [InlineData(SqlBinaryOperator.LikeStartsWith, true, " ILIKE @p1")]
    [InlineData(SqlBinaryOperator.LikeEndsWith, false, " LIKE @p1")]
    [InlineData(SqlBinaryOperator.LikeEndsWith, true, " ILIKE @p1")]
    [InlineData(SqlBinaryOperator.NotLike, false, " NOT LIKE @p1")]
    [InlineData(SqlBinaryOperator.NotLike, true, " NOT ILIKE @p1")]
    [InlineData(SqlBinaryOperator.IsNull, false, " IS NULL")]
    [InlineData(SqlBinaryOperator.IsNotNull, false, " IS NOT NULL")]
    public void Render_AllOperators_RendersCorrectSql(SqlBinaryOperator op, bool caseInsensitive, string expectedSuffix)
    {
        var dialect = new PostgreSqlDialect(useCaseInsensitiveLike: caseInsensitive);
        var model = new QueryModel
        {
            TableName = "T",
            Filters = [new BinaryPredicateNode("Col", op, "p1")]
        };

        var query = dialect.Render(model);
        query.Sql.Should().Be($"SELECT * FROM \"T\" WHERE \"Col\"{expectedSuffix}");
    }

    [Fact]
    public void Render_UnsupportedOperator_ThrowsNotSupportedException()
    {
        var model = new QueryModel
        {
            TableName = "T",
            Filters = [new BinaryPredicateNode("Col", (SqlBinaryOperator)999, "p1")]
        };

        var act = () => _dialect.Render(model);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Operator '999' is not supported by PostgreSQL dialect.*");
    }

    private sealed record CustomUnknownSqlPredicateNode : SqlPredicateNode;
}



