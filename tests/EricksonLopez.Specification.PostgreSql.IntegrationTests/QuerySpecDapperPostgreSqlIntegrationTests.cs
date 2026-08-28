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

[Collection("PostgreSqlDatabase")]
[Trait("Category", "Integration")]
public sealed class QuerySpecDapperPostgreSqlIntegrationTests
{
    private readonly PostgreSqlFixture _fixture;
    private readonly QuerySpecTranslator<Customer> _translator = new("Customers");
    private readonly PostgreSqlDialect _dialect = PostgreSqlDialect.Default;

    public QuerySpecDapperPostgreSqlIntegrationTests(PostgreSqlFixture fixture) => _fixture = fixture;

    private async Task<NpgsqlConnection?> GetConnectionAsync()
    {
        return await _fixture.CreateConnectionAsync();
    }

    [Fact]
    public async Task QueryAsync_ExecutesAndReturnsResults()
    {
        // Arrange
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.TotalPurchases);

        await using var connection = await GetConnectionAsync();
        if (connection is null) return;

        // Act
        var results = await connection.QueryAsync(spec, _translator, _dialect);
        var list = results.ToList();

        // Assert
        list.Should().HaveCount(4);
        list[0].Name.Should().Be("Eve");
        list[1].Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ReturnsFirstResult()
    {
        // Arrange
        var spec = QuerySpec<Customer>.Empty
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.TotalPurchases);

        await using var connection = await GetConnectionAsync();
        if (connection is null) return;

        // Act
        var result = await connection.QueryFirstOrDefaultAsync(spec, _translator, _dialect);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();
        result.Name.Should().Be("Eve");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_ReturnsDefaultWhenNoMatch()
    {
        // Arrange
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name == "NonExistent");

        await using var connection = await GetConnectionAsync();
        if (connection is null) return;

        // Act
        var result = await connection.QueryFirstOrDefaultAsync(spec, _translator, _dialect);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);

        await using var connection = await GetConnectionAsync();
        if (connection is null) return;

        // Act
        var count = await connection.CountAsync(spec, _translator, _dialect);

        // Assert
        count.Should().Be(4);
    }

    [Fact]
    public async Task AnyAsync_ReturnsTrueWhenMatchesExist()
    {
        // Arrange
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);

        await using var connection = await GetConnectionAsync();
        if (connection is null) return;

        // Act
        var exists = await connection.AnyAsync(spec, _translator, _dialect);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_ReturnsFalseWhenNoMatches()
    {
        // Arrange
        var spec = QuerySpec<Customer>.Empty.Where(c => c.Name == "NonExistent");

        await using var connection = await GetConnectionAsync();
        if (connection is null) return;

        // Act
        var exists = await connection.AnyAsync(spec, _translator, _dialect);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task QueryAsync_WithTransaction_ExecutesSuccessfully()
    {
        // Arrange
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);

        await using var connection = await GetConnectionAsync();
        if (connection is null) return;

        using var transaction = await connection.BeginTransactionAsync();

        // Act
        var results = await connection.QueryAsync(spec, _translator, _dialect, transaction);

        // Assert
        results.Should().HaveCount(4);
        await transaction.CommitAsync();
    }
}
