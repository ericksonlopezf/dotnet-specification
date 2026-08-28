// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Specification.Dapper;
using EricksonLopez.Specification.PostgreSql;
using EricksonLopez.Specification.Sql;
using Xunit;

namespace EricksonLopez.Specification.Dapper.Tests;

/// <summary>Test entity for Dapper integration tests.</summary>
public sealed class Customer
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal CreditLimit { get; init; }
}

public sealed class QuerySpecDapperExtensionsTests
{
    private readonly QuerySpecTranslator<Customer> _translator = new("customers");
    private readonly PostgreSqlDialect _dialect = PostgreSqlDialect.Default;

    [Fact]
    public void TranslateAndRender_EmptySpec_ProducesSelectStar()
    {
        var spec = QuerySpec<Customer>.Empty;
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\"");
        query.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void TranslateAndRender_WithFilter_ProducesParameterizedWhere()
    {
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" WHERE \"is_active\" = @p1");
        query.Parameters.Should().ContainKey("p1");
        query.Parameters["p1"].Should().Be(true);
    }

    [Fact]
    public void TranslateAndRender_FullPipeline_ProducesCorrectSql()
    {
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => c.CreditLimit > 1000m)
            .OrderByDescending(c => c.CreditLimit)
            .Page(1, 25);

        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        query.Sql.Should().Be("SELECT * FROM \"customers\" WHERE \"is_active\" = @p1 AND \"credit_limit\" > @p2 ORDER BY \"credit_limit\" DESC LIMIT @_take OFFSET @_skip");
        query.Parameters.Should().ContainKey("p1");
        query.Parameters["p1"].Should().Be(true);
        query.Parameters.Should().ContainKey("p2");
        query.Parameters["p2"].Should().Be(1000m);
        query.Parameters.Should().ContainKey("_take");
        query.Parameters["_take"].Should().Be(25);
        query.Parameters.Should().ContainKey("_skip");
        query.Parameters["_skip"].Should().Be(0);
    }

    #region QueryAsync Tests

    [Fact]
    public async Task QueryAsync_NullArguments_ThrowsArgumentNullException()
    {
        var conn = new FakeDbConnection();
        var spec = QuerySpec<Customer>.Empty;

        var act1 = () => QuerySpecDapperExtensions.QueryAsync<Customer>(null!, null!, null!, null!);
        (await act1.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("connection");

        var act2 = () => QuerySpecDapperExtensions.QueryAsync<Customer>(conn, null!, null!, null!);
        (await act2.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("spec");

        var act3 = () => QuerySpecDapperExtensions.QueryAsync<Customer>(conn, spec, null!, null!);
        (await act3.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("translator");

        var act4 = () => QuerySpecDapperExtensions.QueryAsync<Customer>(conn, spec, _translator, null!);
        (await act4.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("dialect");
    }

    [Fact]
    public async Task QueryAsync_ValidSpec_ExecutesAndReturnsResults()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice", ["IsActive"] = true, ["CreditLimit"] = 5000m },
            new() { ["Id"] = 2, ["Name"] = "Bob", ["IsActive"] = true, ["CreditLimit"] = 3000m }
        };

        var conn = new FakeDbConnection { DataReaderRows = rows };
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        var tx = new FakeDbTransaction(conn);
        using var cts = new CancellationTokenSource();

        var results = await conn.QueryAsync(spec, _translator, _dialect, tx, 30, cts.Token);

        results.Should().HaveCount(2);
        results.First().Name.Should().Be("Alice");

        conn.LastCommand.Should().NotBeNull();
        conn.LastCommand!.CommandText.Should().Contain("SELECT * FROM \"customers\" WHERE \"is_active\" = @p1");
        conn.LastCommand.Transaction.Should().BeSameAs(tx);
        conn.LastCommand.CommandTimeout.Should().Be(30);
    }

    #endregion

    #region QueryFirstOrDefaultAsync Tests

    [Fact]
    public async Task QueryFirstOrDefaultAsync_NullArguments_ThrowsArgumentNullException()
    {
        var conn = new FakeDbConnection();
        var spec = QuerySpec<Customer>.Empty;

        var act1 = () => QuerySpecDapperExtensions.QueryFirstOrDefaultAsync<Customer>(null!, null!, null!, null!);
        (await act1.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("connection");

        var act2 = () => QuerySpecDapperExtensions.QueryFirstOrDefaultAsync<Customer>(conn, null!, null!, null!);
        (await act2.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("spec");

        var act3 = () => QuerySpecDapperExtensions.QueryFirstOrDefaultAsync<Customer>(conn, spec, null!, null!);
        (await act3.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("translator");

        var act4 = () => QuerySpecDapperExtensions.QueryFirstOrDefaultAsync<Customer>(conn, spec, _translator, null!);
        (await act4.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("dialect");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_SpecWithoutTake_AddsLimit1AndReturnsFirst()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice", ["IsActive"] = true, ["CreditLimit"] = 5000m }
        };

        var conn = new FakeDbConnection { DataReaderRows = rows };
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);

        var result = await conn.QueryFirstOrDefaultAsync(spec, _translator, _dialect);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alice");

        conn.LastCommand.Should().NotBeNull();
        conn.LastCommand!.CommandText.Should().Contain("LIMIT @_take");
        conn.LastCommand.Parameters["_take"].Value.Should().Be(1);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_SpecWithExplicitTake_PreservesExplicitTake()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice", ["IsActive"] = true, ["CreditLimit"] = 5000m }
        };

        var conn = new FakeDbConnection { DataReaderRows = rows };
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive).Take(5);

        var result = await conn.QueryFirstOrDefaultAsync(spec, _translator, _dialect);

        result.Should().NotBeNull();
        conn.LastCommand.Should().NotBeNull();
        conn.LastCommand!.CommandText.Should().Contain("LIMIT @_take");
        conn.LastCommand.Parameters["_take"].Value.Should().Be(5);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WithTransactionAndTimeout_PropagatesToCommand()
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Id"] = 1, ["Name"] = "Alice", ["IsActive"] = true, ["CreditLimit"] = 5000m }
        };

        var conn = new FakeDbConnection { DataReaderRows = rows };
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        var tx = new FakeDbTransaction(conn);
        using var cts = new CancellationTokenSource();

        var result = await conn.QueryFirstOrDefaultAsync(spec, _translator, _dialect, tx, 45, cts.Token);

        result.Should().NotBeNull();
        conn.LastCommand.Should().NotBeNull();
        conn.LastCommand!.Transaction.Should().BeSameAs(tx);
        conn.LastCommand.CommandTimeout.Should().Be(45);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_NoMatchingRow_ReturnsDefault()
    {
        var conn = new FakeDbConnection { DataReaderRows = [] };
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);

        var result = await conn.QueryFirstOrDefaultAsync(spec, _translator, _dialect);

        result.Should().BeNull();
    }

    #endregion

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_NullArguments_ThrowsArgumentNullException()
    {
        var conn = new FakeDbConnection();
        var spec = QuerySpec<Customer>.Empty;

        var act1 = () => QuerySpecDapperExtensions.CountAsync<Customer>(null!, null!, null!, null!);
        (await act1.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("connection");

        var act2 = () => QuerySpecDapperExtensions.CountAsync<Customer>(conn, null!, null!, null!);
        (await act2.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("spec");

        var act3 = () => QuerySpecDapperExtensions.CountAsync<Customer>(conn, spec, null!, null!);
        (await act3.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("translator");

        var act4 = () => QuerySpecDapperExtensions.CountAsync<Customer>(conn, spec, _translator, null!);
        (await act4.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("dialect");
    }

    [Fact]
    public async Task CountAsync_StripsOrderingAndPagination_AndReturnsCount()
    {
        var conn = new FakeDbConnection { ScalarResult = 42 };
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Page(2, 10);

        var tx = new FakeDbTransaction(conn);
        using var cts = new CancellationTokenSource();

        var count = await conn.CountAsync(spec, _translator, _dialect, tx, 45, cts.Token);

        count.Should().Be(42);
        conn.LastCommand.Should().NotBeNull();
        conn.LastCommand!.CommandText.Should().StartWith("SELECT COUNT(*) FROM \"customers\" WHERE \"is_active\" = @p1");
        conn.LastCommand.CommandText.Should().NotContain("ORDER BY");
        conn.LastCommand.CommandText.Should().NotContain("LIMIT");
        conn.LastCommand.CommandText.Should().NotContain("OFFSET");
        conn.LastCommand.Transaction.Should().BeSameAs(tx);
        conn.LastCommand.CommandTimeout.Should().Be(45);
    }

    #endregion

    #region AnyAsync Tests

    [Fact]
    public async Task AnyAsync_NullArguments_ThrowsArgumentNullException()
    {
        var conn = new FakeDbConnection();
        var spec = QuerySpec<Customer>.Empty;

        var act1 = () => QuerySpecDapperExtensions.AnyAsync<Customer>(null!, null!, null!, null!);
        (await act1.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("connection");

        var act2 = () => QuerySpecDapperExtensions.AnyAsync<Customer>(conn, null!, null!, null!);
        (await act2.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("spec");

        var act3 = () => QuerySpecDapperExtensions.AnyAsync<Customer>(conn, spec, null!, null!);
        (await act3.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("translator");

        var act4 = () => QuerySpecDapperExtensions.AnyAsync<Customer>(conn, spec, _translator, null!);
        (await act4.Should().ThrowAsync<ArgumentNullException>()).Which.ParamName.Should().Be("dialect");
    }

    [Fact]
    public async Task AnyAsync_WhenMatchingRowExists_ReturnsTrue()
    {
        var conn = new FakeDbConnection { ScalarResult = 1 };
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Skip(10);

        var tx = new FakeDbTransaction(conn);
        using var cts = new CancellationTokenSource();

        var exists = await conn.AnyAsync(spec, _translator, _dialect, tx, 60, cts.Token);

        exists.Should().BeTrue();
        conn.LastCommand.Should().NotBeNull();
        conn.LastCommand!.CommandText.Should().StartWith("SELECT 1 FROM \"customers\" WHERE \"is_active\" = @p1");
        conn.LastCommand.CommandText.Should().Contain("LIMIT @_take");
        conn.LastCommand.CommandText.Should().NotContain("ORDER BY");
        conn.LastCommand.CommandText.Should().NotContain("OFFSET");
        conn.LastCommand.Parameters["_take"].Value.Should().Be(1);
        conn.LastCommand.Transaction.Should().BeSameAs(tx);
        conn.LastCommand.CommandTimeout.Should().Be(60);
    }

    [Fact]
    public async Task AnyAsync_WhenNoMatchingRow_ReturnsFalse()
    {
        var conn = new FakeDbConnection { ScalarResult = null };
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);

        var exists = await conn.AnyAsync(spec, _translator, _dialect);

        exists.Should().BeFalse();
    }

    #endregion

    #region CancellationToken Propagation & Cancellation Tests

    [Fact]
    public async Task QueryAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var conn = new FakeDbConnection();
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => conn.QueryAsync(spec, _translator, _dialect, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var conn = new FakeDbConnection();
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => conn.QueryFirstOrDefaultAsync(spec, _translator, _dialect, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CountAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var conn = new FakeDbConnection();
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => conn.CountAsync(spec, _translator, _dialect, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AnyAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        var conn = new FakeDbConnection();
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => conn.AnyAsync(spec, _translator, _dialect, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}






