// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.Specification.Dapper;
using EricksonLopez.Specification.PostgreSql;
using EricksonLopez.Specification.Sql;
using Npgsql;
using Xunit;

namespace EricksonLopez.Specification.PostgreSql.IntegrationTests;

/// <summary>
/// Test domain entity for integration tests.
/// </summary>
public sealed class Customer
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal TotalPurchases { get; init; }
    public string Region { get; init; } = string.Empty;
    public string? Email { get; init; }
}

/// <summary>
/// Integration tests for <see cref="PostgreSqlDialect"/> executing against a real PostgreSQL database.
/// </summary>
[Collection("PostgreSqlDatabase")]
[Trait("Category", "Integration")]
public sealed class PostgreSqlIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;
    private readonly QuerySpecTranslator<Customer> _translator = new("Customers");
    private readonly PostgreSqlDialect _dialect = PostgreSqlDialect.Default;

    public PostgreSqlIntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    private async Task<NpgsqlConnection?> GetConnectionAsync()
    {
        return await _fixture.CreateConnectionAsync();
    }

    [SkippableFact]
    public async Task Translate_SimpleFilter_ReturnsMatchingRows()
    {
        await using var connection = await GetConnectionAsync();
        Skip.If(connection is null, "PostgreSQL database is not available.");

        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        var results = await connection.QueryAsync(spec, _translator, _dialect);

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(c => c.IsActive.Should().BeTrue());
    }

    [SkippableFact]
    public async Task Translate_LikeStartsWith_ReturnsMatchingRows()
    {
        await using var connection = await GetConnectionAsync();
        Skip.If(connection is null, "PostgreSQL database is not available.");

        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name.StartsWith("Al"));
        var results = await connection.QueryAsync(spec, _translator, _dialect);

        results.Should().AllSatisfy(c => c.Name.Should().StartWith("Al"));
    }

    [SkippableFact]
    public async Task Translate_WithPagination_ReturnsLimitedRows()
    {
        await using var connection = await GetConnectionAsync();
        Skip.If(connection is null, "PostgreSQL database is not available.");

        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Take(2);

        var results = (await connection.QueryAsync(spec, _translator, _dialect)).ToList();

        results.Should().HaveCount(2);
    }

    [SkippableFact]
    public async Task Translate_InPredicate_UsesAnyOperator()
    {
        await using var connection = await GetConnectionAsync();
        Skip.If(connection is null, "PostgreSQL database is not available.");

        var regions = new[] { "US", "EU" };
        var spec = QuerySpec<Customer>.Empty.Where(c => regions.Contains(c.Region));

        var model = _translator.Translate(spec);
        var query = _dialect.Render(model);

        var results = (await connection.QueryAsync<Customer>(query.Sql, new { p1 = regions })).ToList();

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(c => regions.Should().Contain(c.Region));
    }

    [SkippableFact]
    public async Task Translate_EmptyCollectionInPredicate_ReturnsNoRows()
    {
        await using var connection = await GetConnectionAsync();
        Skip.If(connection is null, "PostgreSQL database is not available.");

        var empty = Array.Empty<string>();
        var spec = QuerySpec<Customer>.Empty.Where(c => empty.Contains(c.Region));

        var results = await connection.QueryAsync(spec, _translator, _dialect);

        results.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task Translate_NullCheck_ReturnsRowsWithNullEmail()
    {
        await using var connection = await GetConnectionAsync();
        Skip.If(connection is null, "PostgreSQL database is not available.");

        var spec = QuerySpec<Customer>.Empty.Where(c => c.Email == null);
        var results = await connection.QueryAsync(spec, _translator, _dialect);

        results.Should().AllSatisfy(c => c.Email.Should().BeNull());
    }

    [SkippableFact]
    public async Task Translate_AndOrComposition_ReturnsCorrectRows()
    {
        await using var connection = await GetConnectionAsync();
        Skip.If(connection is null, "PostgreSQL database is not available.");

        // Active customers in US with purchases > 1000
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .Where(c => c.Region == "US")
            .Where(c => c.TotalPurchases > 1000m);

        var results = await connection.QueryAsync(spec, _translator, _dialect);

        results.Should().AllSatisfy(c =>
        {
            c.IsActive.Should().BeTrue();
            c.Region.Should().Be("US");
            c.TotalPurchases.Should().BeGreaterThan(1000m);
        });
    }

    [SkippableFact]
    public async Task Translate_OrderingMultiple_ReturnsOrderedRows()
    {
        await using var connection = await GetConnectionAsync();
        Skip.If(connection is null, "PostgreSQL database is not available.");

        var spec = QuerySpec<Customer>.Empty
            .OrderByDescending(c => c.TotalPurchases)
            .ThenBy(c => c.Name);

        var results = (await connection.QueryAsync(spec, _translator, _dialect)).ToList();

        results.Should().BeInDescendingOrder(c => c.TotalPurchases);
    }
}
