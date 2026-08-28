// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.Sql.Tests;

public sealed class SqlQueryTests
{
    [Fact]
    public void Empty_ReturnsExpectedValues()
    {
        var empty = SqlQuery.Empty;

        empty.Sql.Should().BeEmpty();
        empty.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var paramsDict = new Dictionary<string, object?> { ["p1"] = 42, ["p2"] = "active" };
        var query = new SqlQuery
        {
            Sql = "SELECT * FROM users WHERE id = @p1 AND status = @p2",
            Parameters = paramsDict
        };

        query.Sql.Should().Be("SELECT * FROM users WHERE id = @p1 AND status = @p2");
        query.Parameters.Should().HaveCount(2);
        query.Parameters["p1"].Should().Be(42);
        query.Parameters["p2"].Should().Be("active");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var paramsDict = new Dictionary<string, object?> { ["p1"] = 1 };
        var q1 = new SqlQuery { Sql = "SELECT 1", Parameters = paramsDict };
        var q2 = new SqlQuery { Sql = "SELECT 1", Parameters = paramsDict };

        q1.Should().Be(q2);
    }
}



