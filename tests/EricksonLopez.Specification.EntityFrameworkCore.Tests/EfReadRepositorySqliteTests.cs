// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Specification.EntityFrameworkCore.Tests;

using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Specification;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Relational integration tests for <see cref="EfReadRepository{TDbContext, TEntity}"/> using an in-memory SQLite database.
/// Validates genuine relational SQL query generation, constraints, ordering, and projections.
/// </summary>
public sealed class EfReadRepositorySqliteTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly TestSpecDbContext _dbContext;
    private readonly EfReadRepository<TestSpecDbContext, Product> _repository;

    public EfReadRepositorySqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestSpecDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestSpecDbContext(options);
        _repository = new EfReadRepository<TestSpecDbContext, Product>(_dbContext);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.Database.EnsureCreatedAsync();

        _dbContext.Products.AddRange(
            new Product { Id = 1, Name = "Laptop", Price = 1200m, IsActive = true },
            new Product { Id = 2, Name = "Mouse", Price = 25m, IsActive = true },
            new Product { Id = 3, Name = "Keyboard", Price = 75m, IsActive = false },
            new Product { Id = 4, Name = "Monitor", Price = 300m, IsActive = true }
        );
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task ListAsync_RelationalFiltersAndOrders_ReturnsMatchingEntities()
    {
        // Arrange
        var spec = new QuerySpec<Product>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name);

        // Act
        var results = await _repository.ListAsync(spec);

        // Assert
        results.Should().HaveCount(3);
        results.Select(p => p.Name).Should().ContainInOrder("Laptop", "Monitor", "Mouse");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_RelationalPredicate_ReturnsFirstMatch()
    {
        // Arrange
        var spec = new QuerySpec<Product>()
            .Where(p => p.Name == "Mouse");

        // Act
        var result = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Mouse");
    }

    [Fact]
    public async Task SingleOrDefaultAsync_RelationalPredicate_ReturnsSingleOrThrowsOnMultiple()
    {
        // Arrange
        var specExact = new QuerySpec<Product>().Where(p => p.Name == "Laptop");
        var specNone = new QuerySpec<Product>().Where(p => p.Name == "NonExistent");
        var specMultiple = new QuerySpec<Product>().Where(p => p.IsActive);

        // Act & Assert
        var exact = await _repository.SingleOrDefaultAsync(specExact);
        exact.Should().NotBeNull();
        exact!.Name.Should().Be("Laptop");

        var none = await _repository.SingleOrDefaultAsync(specNone);
        none.Should().BeNull();

        var actMultiple = () => _repository.SingleOrDefaultAsync(specMultiple);
        await actMultiple.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CountAsync_RelationalQuery_ReturnsAccurateCount()
    {
        // Arrange
        var spec = new QuerySpec<Product>().Where(p => p.IsActive);

        // Act
        var count = await _repository.CountAsync(spec);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task AnyAsync_RelationalQuery_ReturnsTrue()
    {
        // Arrange
        var spec = new QuerySpec<Product>().Where(p => p.IsActive);

        // Act
        var any = await _repository.AnyAsync(spec);

        // Assert
        any.Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_ProjectedSqliteQuery_ReturnsExpectedDtos()
    {
        // Arrange
        var spec = new QuerySpec<Product, ProductDto>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Page(1, 2)
            .Select(p => new ProductDto(p.Id, p.Name));

        // Act
        var results = await _repository.ListAsync(spec);

        // Assert
        results.Should().HaveCount(2);
        results[0].Name.Should().Be("Laptop");
        results[1].Name.Should().Be("Monitor");
    }

    [Fact]
    public async Task Queries_ExecuteWithAsNoTracking_EntitiesAreDetached()
    {
        // Arrange
        var spec = new QuerySpec<Product>().Where(p => p.Id == 1);

        // Act
        var entity = await _repository.FirstOrDefaultAsync(spec);

        // Assert
        entity.Should().NotBeNull();
        _dbContext.Entry(entity!).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task ListAsync_WithTransaction_ReadsWithinActiveTransaction()
    {
        // Arrange
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();
        var spec = new QuerySpec<Product>().Where(p => p.IsActive);

        // Act
        var results = await _repository.ListAsync(spec);

        // Assert
        results.Should().HaveCount(3);
        await transaction.CommitAsync();
    }

    [Fact]
    public async Task EfReadRepository_Sqlite_RespectsCancellationToken()
    {
        // Arrange
        var spec = new QuerySpec<Product>().Where(p => p.IsActive);
        var projSpec = new QuerySpec<Product, ProductDto> { Selector = p => new ProductDto(p.Id, p.Name) };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var actFirst = () => _repository.FirstOrDefaultAsync(spec, cts.Token);
        await actFirst.Should().ThrowAsync<OperationCanceledException>();

        var actList = () => _repository.ListAsync(spec, cts.Token);
        await actList.Should().ThrowAsync<OperationCanceledException>();

        var actListProj = () => _repository.ListAsync(projSpec, cts.Token);
        await actListProj.Should().ThrowAsync<OperationCanceledException>();

        var actCount = () => _repository.CountAsync(spec, cts.Token);
        await actCount.Should().ThrowAsync<OperationCanceledException>();

        var actAny = () => _repository.AnyAsync(spec, cts.Token);
        await actAny.Should().ThrowAsync<OperationCanceledException>();

        var actSingle = () => _repository.SingleOrDefaultAsync(spec, cts.Token);
        await actSingle.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void EfSpecificationEvaluator_TagWith_GeneratesSqlTagCommentInSqlite()
    {
        var spec = new QuerySpec<Product> { Tag = "DiagnosticTraceTag-Sqlite" }.Where(p => p.IsActive);
        var query = EfSpecificationEvaluator.GetQuery(_dbContext.Products, spec);
        query.ToQueryString().Should().Contain("-- DiagnosticTraceTag-Sqlite");

        var whitespaceSpec = new QuerySpec<Product> { Tag = "   " }.Where(p => p.IsActive);
        var whitespaceQuery = EfSpecificationEvaluator.GetQuery(_dbContext.Products, whitespaceSpec);
        whitespaceQuery.ToQueryString().Should().NotContain("--");

        var emptySpec = new QuerySpec<Product> { Tag = string.Empty }.Where(p => p.IsActive);
        var emptyQuery = EfSpecificationEvaluator.GetQuery(_dbContext.Products, emptySpec);
        emptyQuery.ToQueryString().Should().NotContain("--");

        var projSpec = new QuerySpec<Product, string> { Tag = "DiagnosticTraceTag-ProjectedSqlite", Selector = p => p.Name }
            .Where(p => p.IsActive);
        var projQuery = EfSpecificationEvaluator.GetQuery(_dbContext.Products, projSpec);
        projQuery.ToQueryString().Should().Contain("-- DiagnosticTraceTag-ProjectedSqlite");

        var projWhitespaceSpec = new QuerySpec<Product, string> { Tag = "   ", Selector = p => p.Name }
            .Where(p => p.IsActive);
        var projWhitespaceQuery = EfSpecificationEvaluator.GetQuery(_dbContext.Products, projWhitespaceSpec);
        projWhitespaceQuery.ToQueryString().Should().NotContain("--");

        var projEmptySpec = new QuerySpec<Product, string> { Tag = string.Empty, Selector = p => p.Name }
            .Where(p => p.IsActive);
        var projEmptyQuery = EfSpecificationEvaluator.GetQuery(_dbContext.Products, projEmptySpec);
        projEmptyQuery.ToQueryString().Should().NotContain("--");
    }
}







