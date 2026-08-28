// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Specification.Oracle;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.Oracle.Tests;

public sealed class OracleDialectTests
{
    private readonly OracleDialect _dialect = OracleDialect.Default;

    [Fact]
    public void DialectProperties_ReturnExpectedValues()
    {
        _dialect.DialectName.Should().Be("Oracle");
        _dialect.ParameterPrefix.Should().Be(":");
    }

    [Theory]
    [InlineData("customers", "\"customers\"")]
    [InlineData("order_items", "\"order_items\"")]
    [InlineData("table\"name", "\"table\"\"name\"")]
    public void QuoteIdentifier_QuotesWithDoubleQuotes(string input, string expected)
    {
        _dialect.QuoteIdentifier(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void QuoteIdentifier_NullOrWhitespace_ThrowsArgumentException(string? input)
    {
        var act = () => _dialect.QuoteIdentifier(input!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Render_NullModel_ThrowsArgumentNullException()
    {
        var act = () => _dialect.Render(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Render_SelectAll_FromTable()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters = [],
            Orders = [],
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\"");
        query.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Render_SingleProjection_NoTrailingOrLeadingCommas()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Projections = ["id"],
            Filters = [],
            Orders = [],
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT \"id\" FROM \"customers\"");
    }

    [Fact]
    public void Render_SelectDistinct_WithProjections_AndAlias_WithoutAsKeyword()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            TableAlias = "c",
            IsDistinct = true,
            Projections = ["id", "name"],
            Filters = [],
            Orders = [],
            Parameters = []
        };

        var query = _dialect.Render(model);

        // In Oracle, table aliases in FROM cannot use the AS keyword
        query.Sql.Should().Be("SELECT DISTINCT \"id\", \"name\" FROM \"customers\" \"c\"");
    }

    [Fact]
    public void Render_CountQuery_ProducesSelectCount()
    {
        var model = new QueryModel
        {
            TableName = "orders",
            QueryType = SqlQueryType.Count,
            Filters = [],
            Orders = [],
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT COUNT(*) FROM \"orders\"");
    }

    [Fact]
    public void Render_ExistsQuery_ProducesSelect1()
    {
        var model = new QueryModel
        {
            TableName = "orders",
            QueryType = SqlQueryType.Exists,
            Filters = [],
            Orders = [],
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT 1 FROM \"orders\"");
    }

    [Fact]
    public void Render_BinaryPredicates_RendersColonParametersCorrectly()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters =
            [
                new BinaryPredicateNode("is_active", SqlBinaryOperator.Equal, "p1"),
                new BinaryPredicateNode("age", SqlBinaryOperator.GreaterThanOrEqual, "p2"),
                new BinaryPredicateNode("credit_limit", SqlBinaryOperator.LessThan, "p3")
            ],
            Orders = [],
            Parameters =
            [
                new SqlParameter("p1", true),
                new SqlParameter("p2", 18),
                new SqlParameter("p3", 5000m)
            ]
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" WHERE \"is_active\" = :p1 AND \"age\" >= :p2 AND \"credit_limit\" < :p3");
        query.Parameters["p1"].Should().Be(true);
        query.Parameters["p2"].Should().Be(18);
        query.Parameters["p3"].Should().Be(5000m);
    }

    [Fact]
    public void Render_IsNullAndIsNotNull_RendersNullChecksWithoutParameters()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters =
            [
                new BinaryPredicateNode("deleted_at", SqlBinaryOperator.IsNull, string.Empty),
                new BinaryPredicateNode("verified_at", SqlBinaryOperator.IsNotNull, string.Empty)
            ],
            Orders = [],
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" WHERE \"deleted_at\" IS NULL AND \"verified_at\" IS NOT NULL");
    }

    [Fact]
    public void Render_LikeOperators_RendersLikeCorrectly()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters =
            [
                new BinaryPredicateNode("email", SqlBinaryOperator.Like, "p1"),
                new BinaryPredicateNode("name", SqlBinaryOperator.LikeStartsWith, "p2"),
                new BinaryPredicateNode("code", SqlBinaryOperator.LikeEndsWith, "p3")
            ],
            Orders = [],
            Parameters =
            [
                new SqlParameter("p1", "%@gmail.com"),
                new SqlParameter("p2", "John%"),
                new SqlParameter("p3", "%XYZ")
            ]
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" WHERE \"email\" LIKE :p1 AND \"name\" LIKE :p2 AND \"code\" LIKE :p3");
    }

    [Fact]
    public void Render_InPredicate_ExpandsCollectionParameters()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters =
            [
                new InPredicateNode("status_id", "p1", Negated: false),
                new InPredicateNode("country_code", "p2", Negated: true)
            ],
            Orders = [],
            Parameters =
            [
                new SqlParameter("p1", new[] { 1, 2, 3 }),
                new SqlParameter("p2", new[] { "CA", "MX" }),
                new SqlParameter("scalarParam", "stringValue")
            ]
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" WHERE \"status_id\" IN (:p1_0, :p1_1, :p1_2) AND \"country_code\" NOT IN (:p2_0, :p2_1)");
        query.Parameters["p1_0"].Should().Be(1);
        query.Parameters["p1_1"].Should().Be(2);
        query.Parameters["p1_2"].Should().Be(3);
        query.Parameters["p2_0"].Should().Be("CA");
        query.Parameters["p2_1"].Should().Be("MX");
        query.Parameters["scalarParam"].Should().Be("stringValue");
    }

    [Fact]
    public void Render_InPredicate_SingleItem_RendersWithoutExtraCommas()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters = [new InPredicateNode("status_id", "p1", Negated: false)],
            Orders = [],
            Parameters = [new SqlParameter("p1", new[] { 42 })]
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" WHERE \"status_id\" IN (:p1_0)");
        query.Parameters["p1_0"].Should().Be(42);
    }

    [Fact]
    public void Render_EmptyInPredicate_RendersAlwaysFalseOrAlwaysTrue()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters =
            [
                new InPredicateNode("id", "p1", Negated: false),
                new InPredicateNode("id", "p2", Negated: true)
            ],
            Orders = [],
            Parameters =
            [
                new SqlParameter("p1", Array.Empty<int>()),
                new SqlParameter("p2", Array.Empty<int>())
            ]
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" WHERE 1 = 0 AND 1 = 1");
    }

    [Fact]
    public void Render_ComplexTreeWithAndOrNot()
    {
        var left = new BinaryPredicateNode("is_active", SqlBinaryOperator.Equal, "p1");
        var right = new BinaryPredicateNode("is_vip", SqlBinaryOperator.Equal, "p2");
        var orNode = new OrPredicateNode(left, right);
        var notNode = new NotPredicateNode(new BinaryPredicateNode("is_banned", SqlBinaryOperator.Equal, "p3"));
        var andNode = new AndPredicateNode(orNode, notNode);

        var model = new QueryModel
        {
            TableName = "users",
            Filters = [andNode],
            Orders = [],
            Parameters =
            [
                new SqlParameter("p1", true),
                new SqlParameter("p2", true),
                new SqlParameter("p3", true)
            ]
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"users\" WHERE ((\"is_active\" = :p1 OR \"is_vip\" = :p2) AND NOT (\"is_banned\" = :p3))");
    }

    [Fact]
    public void Render_SingleOrder_ProducesOrderByClause()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters = [],
            Orders = [new SqlOrderNode("last_name", OrderDirection.Ascending)],
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" ORDER BY \"last_name\" ASC");
    }

    [Fact]
    public void Render_MultipleOrders_ProducesCommaSeparatedOrderByClause()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters = [],
            Orders =
            [
                new SqlOrderNode("last_name", OrderDirection.Ascending),
                new SqlOrderNode("created_at", OrderDirection.Descending)
            ],
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" ORDER BY \"last_name\" ASC, \"created_at\" DESC");
    }

    [Fact]
    public void Render_Pagination_WithTakeAndSkip_WithOrders()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Orders = [new SqlOrderNode("id", OrderDirection.Ascending)],
            Filters = [],
            Skip = 20,
            Take = 10,
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" ORDER BY \"id\" ASC OFFSET :_skip ROWS FETCH NEXT :_take ROWS ONLY");
        query.Parameters["_take"].Should().Be(10);
        query.Parameters["_skip"].Should().Be(20);
    }

    [Fact]
    public void Render_Pagination_WithTakeAndSkip_WithoutOrders_EmitsDefaultOrderBy()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Orders = [],
            Filters = [],
            Skip = 20,
            Take = 10,
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" ORDER BY (SELECT NULL FROM DUAL) OFFSET :_skip ROWS FETCH NEXT :_take ROWS ONLY");
        query.Parameters["_take"].Should().Be(10);
        query.Parameters["_skip"].Should().Be(20);
    }

    [Fact]
    public void Render_Pagination_WithTakeOnly()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters = [],
            Orders = [],
            Take = 25,
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" FETCH NEXT :_take ROWS ONLY");
        query.Parameters["_take"].Should().Be(25);
    }

    [Fact]
    public void Render_Pagination_WithSkipOnly_WithOrders()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters = [],
            Orders = [new SqlOrderNode("id", OrderDirection.Ascending)],
            Skip = 50,
            Take = null,
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" ORDER BY \"id\" ASC OFFSET :_skip ROWS");
        query.Parameters["_skip"].Should().Be(50);
    }

    [Fact]
    public void Render_Pagination_WithSkipOnly_WithoutOrders_EmitsDefaultOrderBy()
    {
        var model = new QueryModel
        {
            TableName = "customers",
            Filters = [],
            Orders = [],
            Skip = 50,
            Take = null,
            Parameters = []
        };

        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" ORDER BY (SELECT NULL FROM DUAL) OFFSET :_skip ROWS");
        query.Parameters["_skip"].Should().Be(50);
    }

    [Fact]
    public void Render_BetweenPredicate_RendersCorrectly()
    {
        var model = new QueryModel
        {
            TableName = "products",
            Filters = [new BetweenPredicateNode("price", "p1", "p2")],
            Orders = [],
            Parameters = [new SqlParameter("p1", 10), new SqlParameter("p2", 50)]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"products\" WHERE \"price\" BETWEEN :p1 AND :p2");
    }

    [Fact]
    public void Render_BetweenPredicate_Negated_RendersCorrectly()
    {
        var model = new QueryModel
        {
            TableName = "products",
            Filters = [new BetweenPredicateNode("price", "p1", "p2") { Negated = true }],
            Parameters = [new SqlParameter("p1", 10), new SqlParameter("p2", 50)]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"products\" WHERE \"price\" NOT BETWEEN :p1 AND :p2");
    }

    [Fact]
    public void Render_FullTextPredicate_RendersOracleTextContains()
    {
        var model = new QueryModel
        {
            TableName = "articles",
            Filters = [new FullTextPredicateNode("body", "p1")],
            Orders = [],
            Parameters = [new SqlParameter("p1", "oracle")]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"articles\" WHERE CONTAINS(\"body\", :p1) > 0");
    }

    [Fact]
    public void Render_RangePredicate_RendersCorrectly()
    {
        var model = new QueryModel
        {
            TableName = "events",
            Filters = [new RangePredicateNode("event_date", "p1", "p2")],
            Orders = [],
            Parameters = [new SqlParameter("p1", "2025-01-01"), new SqlParameter("p2", "2025-12-31")]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"events\" WHERE (\"event_date\" >= :p1 AND \"event_date\" <= :p2)");
    }

    [Fact]
    public void Render_InPredicateNode_EmptyCollection_Negated_ProducesAlwaysTrue()
    {
        var model = new QueryModel
        {
            TableName = "products",
            Filters = [new InPredicateNode("Category", "p1") { Negated = true }],
            Parameters = [new SqlParameter("p1", Array.Empty<string>())]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"products\" WHERE 1 = 1");
    }

    [Fact]
    public void Render_InPredicateNode_MissingParameterInExpansions_ProducesAlwaysFalse()
    {
        var model = new QueryModel
        {
            TableName = "products",
            Filters = [new InPredicateNode("Category", "missing_param")],
            Parameters = []
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"products\" WHERE 1 = 0");
    }

    [Fact]
    public void Render_InPredicateNode_PresentKeyWithEmptyExpandedList_ProducesAlwaysFalse()
    {
        var model = new QueryModel
        {
            TableName = "products",
            Filters = [new InPredicateNode("Category", "p1")],
            Parameters = [new SqlParameter("p1", new List<string>())]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"products\" WHERE 1 = 0");
    }

    [Fact]
    public void Render_InPredicateNode_EmptyCollection_NonNegated_ProducesAlwaysFalse()
    {
        var model = new QueryModel
        {
            TableName = "products",
            Filters = [new InPredicateNode("Category", "p1") { Negated = false }],
            Parameters = [new SqlParameter("p1", Array.Empty<string>())]
        };

        var query = _dialect.Render(model);
        query.Sql.Should().Be("SELECT * FROM \"products\" WHERE 1 = 0");
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
    [InlineData(SqlBinaryOperator.Equal, " = :p1")]
    [InlineData(SqlBinaryOperator.NotEqual, " <> :p1")]
    [InlineData(SqlBinaryOperator.GreaterThan, " > :p1")]
    [InlineData(SqlBinaryOperator.GreaterThanOrEqual, " >= :p1")]
    [InlineData(SqlBinaryOperator.LessThan, " < :p1")]
    [InlineData(SqlBinaryOperator.LessThanOrEqual, " <= :p1")]
    [InlineData(SqlBinaryOperator.Like, " LIKE :p1")]
    [InlineData(SqlBinaryOperator.LikeStartsWith, " LIKE :p1")]
    [InlineData(SqlBinaryOperator.LikeEndsWith, " LIKE :p1")]
    [InlineData(SqlBinaryOperator.NotLike, " NOT LIKE :p1")]
    [InlineData(SqlBinaryOperator.IsNull, " IS NULL")]
    [InlineData(SqlBinaryOperator.IsNotNull, " IS NOT NULL")]
    public void Render_AllOperators_RendersCorrectSql(SqlBinaryOperator op, string expectedSuffix)
    {
        var model = new QueryModel
        {
            TableName = "T",
            Filters = [new BinaryPredicateNode("Col", op, "p1")]
        };

        var query = _dialect.Render(model);
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
            .WithMessage("*Operator '999' is not supported by Oracle dialect.*");
    }

    private sealed record CustomUnknownSqlPredicateNode : SqlPredicateNode;
}
