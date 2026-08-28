// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Specification.EntityFrameworkCore.Tests;

using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Specification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public sealed record ProductDto(int Id, string Name);

public sealed class TestSpecDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();

    public TestSpecDbContext(DbContextOptions<TestSpecDbContext> options)
        : base(options)
    {
    }
}

public class EfReadRepositoryTests
{
    private static TestSpecDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestSpecDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new TestSpecDbContext(options);
        context.Products.AddRange(
            new Product { Id = 1, Name = "Laptop", Price = 1200m, IsActive = true },
            new Product { Id = 2, Name = "Mouse", Price = 25m, IsActive = true },
            new Product { Id = 3, Name = "Keyboard", Price = 75m, IsActive = false },
            new Product { Id = 4, Name = "Monitor", Price = 300m, IsActive = true }
        );
        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task ListAsync_WhenFilteredAndOrdered_ReturnsMatchingProducts()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repository = new EfReadRepository<TestSpecDbContext, Product>(db);

        var spec = new QuerySpec<Product>()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.Price);

        // Act
        var results = await repository.ListAsync(spec);

        // Assert
        results.Should().HaveCount(3);
        results.Select(p => p.Name).Should().ContainInOrder("Laptop", "Monitor", "Mouse");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirstMatch()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repository = new EfReadRepository<TestSpecDbContext, Product>(db);

        var spec = new QuerySpec<Product>()
            .Where(p => p.Price < 50m);

        // Act
        var result = await repository.FirstOrDefaultAsync(spec);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Mouse");
    }

    [Fact]
    public async Task CountAsync_WhenMatchingEntitiesExist_ReturnsAccurateCount()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repository = new EfReadRepository<TestSpecDbContext, Product>(db);

        var spec = new QuerySpec<Product>().Where(p => p.IsActive);

        // Act
        var count = await repository.CountAsync(spec);

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task AnyAsync_WhenMatchingEntitiesExist_ReturnsTrue()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repository = new EfReadRepository<TestSpecDbContext, Product>(db);

        var spec = new QuerySpec<Product>().Where(p => p.IsActive);

        // Act
        var any = await repository.AnyAsync(spec);

        // Assert
        any.Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_Projected_ReturnsDtos()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repository = new EfReadRepository<TestSpecDbContext, Product>(db);

        var spec = new QuerySpec<Product, ProductDto>
        {
            Selector = p => new ProductDto(p.Id, p.Name)
        }.Where(p => p.IsActive);

        // Act
        var results = await repository.ListAsync(spec);

        // Assert
        results.Should().HaveCount(3);
        results[0].Should().BeOfType<ProductDto>();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_ReturnsSingleMatchOrNull()
    {
        // Arrange
        await using var db = CreateDbContext();
        var repository = new EfReadRepository<TestSpecDbContext, Product>(db);

        var specExact = new QuerySpec<Product>().Where(p => p.Name == "Laptop");
        var specNone = new QuerySpec<Product>().Where(p => p.Name == "NonExistent");

        // Act & Assert
        var exact = await repository.SingleOrDefaultAsync(specExact);
        exact.Should().NotBeNull();
        exact!.Name.Should().Be("Laptop");

        var none = await repository.SingleOrDefaultAsync(specNone);
        none.Should().BeNull();

        var specMultiple = new QuerySpec<Product>().Where(p => p.IsActive);
        var actMultiple = () => repository.SingleOrDefaultAsync(specMultiple);
        await actMultiple.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void EfReadRepository_Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        var act1 = () => new EfReadRepository<TestSpecDbContext, Product>(null!);
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => new EfReadRepository<Product>(null!);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task EfReadRepository_NullSpec_ThrowsArgumentNullException()
    {
        await using var db = CreateDbContext();
        var repository = new EfReadRepository<TestSpecDbContext, Product>(db);

        var actFirst = () => repository.FirstOrDefaultAsync(null!);
        await actFirst.Should().ThrowAsync<ArgumentNullException>();

        var actList = () => repository.ListAsync((QuerySpec<Product>)null!);
        await actList.Should().ThrowAsync<ArgumentNullException>();

        var actListProj = () => repository.ListAsync((QuerySpec<Product, ProductDto>)null!);
        await actListProj.Should().ThrowAsync<ArgumentNullException>();

        var actCount = () => repository.CountAsync(null!);
        await actCount.Should().ThrowAsync<ArgumentNullException>();

        var actAny = () => repository.AnyAsync(null!);
        await actAny.Should().ThrowAsync<ArgumentNullException>();

        var actSingle = () => repository.SingleOrDefaultAsync(null!);
        await actSingle.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EfReadRepository_DefaultDbContextSubclass_Works()
    {
        await using var db = CreateDbContext();
        var repo = new EfReadRepository<Product>(db);

        var spec = new QuerySpec<Product>().Where(p => p.Name == "Mouse");
        var result = await repo.FirstOrDefaultAsync(spec);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Mouse");
    }

    [Fact]
    public void EfSpecificationEvaluator_GetQuery_AppliesSpecAndValidatesNull()
    {
        var list = new List<Product>
        {
            new() { Id = 1, Name = "A", IsActive = true, Price = 10m },
            new() { Id = 2, Name = "B", IsActive = false, Price = 20m }
        }.AsQueryable();

        var spec = new QuerySpec<Product>().Where(p => p.IsActive);
        var query = EfSpecificationEvaluator.GetQuery(list, spec);
        query.ToList().Should().HaveCount(1);

        var projSpec = new QuerySpec<Product, string> { Selector = p => p.Name }.Where(p => p.IsActive);
        var projQuery = EfSpecificationEvaluator.GetQuery(list, projSpec);
        projQuery.ToList().Should().ContainSingle().Which.Should().Be("A");

        var act1 = () => EfSpecificationEvaluator.GetQuery<Product>(null!, spec);
        act1.Should().Throw<ArgumentNullException>().WithParameterName("source");

        var act2 = () => EfSpecificationEvaluator.GetQuery<Product>(list, null!);
        act2.Should().Throw<ArgumentNullException>().WithParameterName("specification");

        var act3 = () => EfSpecificationEvaluator.GetQuery<Product, string>(null!, projSpec);
        act3.Should().Throw<ArgumentNullException>().WithParameterName("source");

        var act4 = () => EfSpecificationEvaluator.GetQuery<Product, string>(list, null!);
        act4.Should().Throw<ArgumentNullException>().WithParameterName("specification");

        // Interface implementation through default singleton
        ISpecificationEvaluator evaluator = EfSpecificationEvaluator.Default;
        evaluator.Should().NotBeNull();
        var ifaceQuery = evaluator.GetQuery(list, spec);
        ifaceQuery.ToList().Should().HaveCount(1);
        var ifaceProj = evaluator.GetQuery(list, projSpec);
        ifaceProj.ToList().Should().ContainSingle().Which.Should().Be("A");
    }

    [Fact]
    public void AddSpecificationEntityFramework_Overloads_RegisterCorrectly()
    {
        // Generic AddSpecificationEntityFramework<TDbContext> overload
        var services0 = new ServiceCollection();
        var options0 = new DbContextOptionsBuilder<TestSpecDbContext>()
            .UseInMemoryDatabase("DI_Test0")
            .Options;
        services0.AddScoped(_ => new TestSpecDbContext(options0));
        services0.AddSpecificationEntityFramework<TestSpecDbContext>();
        using var provider0 = services0.BuildServiceProvider();
        var repo0 = provider0.GetService<IReadRepository<Product>>();
        repo0.Should().NotBeNull();
        repo0.Should().BeOfType<EfReadRepository<Product>>();
        var eval0 = provider0.GetService<ISpecificationEvaluator>();
        eval0.Should().NotBeNull();
        eval0.Should().BeSameAs(EfSpecificationEvaluator.Default);

        // Parameterless AddSpecificationEntityFramework overload
        var services1 = new ServiceCollection();
        var options1 = new DbContextOptionsBuilder<TestSpecDbContext>()
            .UseInMemoryDatabase("DI_Test1")
            .Options;
        services1.AddScoped<DbContext>(_ => new TestSpecDbContext(options1));
        services1.AddSpecificationEntityFramework();
        using var provider1 = services1.BuildServiceProvider();
        var repo1 = provider1.GetService<IReadRepository<Product>>();
        repo1.Should().NotBeNull();
        repo1.Should().BeOfType<EfReadRepository<Product>>();
        var eval1 = provider1.GetService<ISpecificationEvaluator>();
        eval1.Should().NotBeNull();
        eval1.Should().BeSameAs(EfSpecificationEvaluator.Default);

        // AddEfReadRepository<TDbContext, TEntity> overload
        var services2 = new ServiceCollection();
        var options2 = new DbContextOptionsBuilder<TestSpecDbContext>()
            .UseInMemoryDatabase("DI_Test2")
            .Options;
        services2.AddScoped(_ => new TestSpecDbContext(options2));
        services2.AddEfReadRepository<TestSpecDbContext, Product>();
        using var provider2 = services2.BuildServiceProvider();
        var repo2 = provider2.GetService<IReadRepository<Product>>();
        repo2.Should().NotBeNull();
        repo2.Should().BeOfType<EfReadRepository<TestSpecDbContext, Product>>();

        // Null service collection guards
        IServiceCollection nullServices = null!;
        var actNull1 = () => nullServices.AddSpecificationEntityFramework<TestSpecDbContext>();
        actNull1.Should().Throw<ArgumentNullException>();

        var actNull2 = () => nullServices.AddSpecificationEntityFramework();
        actNull2.Should().Throw<ArgumentNullException>();

        var actNull3 = () => nullServices.AddEfReadRepository<TestSpecDbContext, Product>();
        actNull3.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task EfReadRepository_RespectsCancellationToken()
    {
        await using var db = CreateDbContext();
        var repo = new EfReadRepository<TestSpecDbContext, Product>(db);
        var spec = new QuerySpec<Product>().Where(p => p.IsActive);
        var projSpec = new QuerySpec<Product, ProductDto> { Selector = p => new ProductDto(p.Id, p.Name) };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var actFirst = () => repo.FirstOrDefaultAsync(spec, cts.Token);
        await actFirst.Should().ThrowAsync<OperationCanceledException>();

        var actList = () => repo.ListAsync(spec, cts.Token);
        await actList.Should().ThrowAsync<OperationCanceledException>();

        var actListProj = () => repo.ListAsync(projSpec, cts.Token);
        await actListProj.Should().ThrowAsync<OperationCanceledException>();

        var actCount = () => repo.CountAsync(spec, cts.Token);
        await actCount.Should().ThrowAsync<OperationCanceledException>();

        var actAny = () => repo.AnyAsync(spec, cts.Token);
        await actAny.Should().ThrowAsync<OperationCanceledException>();

        var actSingle = () => repo.SingleOrDefaultAsync(spec, cts.Token);
        await actSingle.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task EfReadRepository_QueriesUseAsNoTracking()
    {
        await using var db = CreateDbContext();
        var repo = new EfReadRepository<TestSpecDbContext, Product>(db);

        var spec = new QuerySpec<Product>().Where(p => p.Id == 1);
        var entity = await repo.FirstOrDefaultAsync(spec);
        entity.Should().NotBeNull();

        // With AsNoTracking(), the entity returned is detached from the DbContext ChangeTracker
        db.Entry(entity!).State.Should().Be(EntityState.Detached);

        var list = await repo.ListAsync(spec);
        list.Should().ContainSingle();
        db.Entry(list[0]).State.Should().Be(EntityState.Detached);

        var single = await repo.SingleOrDefaultAsync(spec);
        single.Should().NotBeNull();
        db.Entry(single!).State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task ListAsync_ProjectedWithFilterOrderAndPagination_ReturnsExpectedDtos()
    {
        await using var db = CreateDbContext();
        var repo = new EfReadRepository<TestSpecDbContext, Product>(db);

        var spec = new QuerySpec<Product, ProductDto>()
            .Where(p => p.Price > 50m)
            .OrderByDescending(p => p.Price)
            .Page(1, 2)
            .Select(p => new ProductDto(p.Id, p.Name));

        var results = await repo.ListAsync(spec);

        results.Should().HaveCount(2);
        results[0].Name.Should().Be("Laptop");
        results[1].Name.Should().Be("Monitor");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsEntity()
    {
        await using var db = CreateDbContext();
        var repo = new EfReadRepository<TestSpecDbContext, Product>(db);

        var product = await repo.GetByIdAsync(1);

        product.Should().NotBeNull();
        product!.Id.Should().Be(1);
        product.Name.Should().Be("Laptop");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        await using var db = CreateDbContext();
        var repo = new EfReadRepository<TestSpecDbContext, Product>(db);

        var product = await repo.GetByIdAsync(999);

        product.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithNullId_ThrowsArgumentNullException()
    {
        await using var db = CreateDbContext();
        var repo = new EfReadRepository<TestSpecDbContext, Product>(db);

        var act = () => repo.GetByIdAsync<string>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_ThrowsWhenCancelled()
    {
        await using var db = CreateDbContext();
        var repo = new EfReadRepository<TestSpecDbContext, Product>(db);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => repo.GetByIdAsync(1, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Evaluator_WithTag_AppliesTagWithToQuery()
    {
        await using var db = CreateDbContext();
        var spec = new QuerySpec<Product>()
            .Where(p => p.IsActive)
            .TagWith("DiagnosticTraceTag-12345");

        var query = EfSpecificationEvaluator.GetQuery(db.Products, spec);
        var result = await query.ToListAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Evaluator_ProjectedWithTag_AppliesTagWithToQuery()
    {
        await using var db = CreateDbContext();
        var spec = new QuerySpec<Product, string>()
            .Where(p => p.IsActive)
            .TagWith("DiagnosticTraceTag-Projected")
            .Select(p => p.Name);

        var query = EfSpecificationEvaluator.GetQuery(db.Products, spec);
        var result = await query.ToListAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task CustomEvaluator_CanBeInjectedIntoRepository()
    {
        await using var db = CreateDbContext();
        var customEvaluator = new CustomTestEvaluator();
        var repo = new EfReadRepository<TestSpecDbContext, Product>(db, customEvaluator);

        var spec = new QuerySpec<Product>().Where(p => p.IsActive);
        var results = await repo.ListAsync(spec);

        customEvaluator.GetQueryCalled.Should().BeTrue();
        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task QuerySpecEfCoreExtensions_ApplyWithFlags_ConfiguresQueryable()
    {
        await using var db = CreateDbContext();
        var spec = new QuerySpec<Product>().Where(p => p.IsActive);

        var queryBoth = db.Products.Apply(spec, asSplitQuery: true, ignoreAutoIncludes: true);
        queryBoth.Expression.ToString().Should().Contain("AsSplitQuery").And.Contain("IgnoreAutoIncludes");
        var results = await queryBoth.ToListAsync();
        results.Should().HaveCount(3);

        var querySplitOnly = db.Products.Apply(spec, asSplitQuery: true, ignoreAutoIncludes: false);
        querySplitOnly.Expression.ToString().Should().Contain("AsSplitQuery").And.NotContain("IgnoreAutoIncludes");

        var queryAutoOnly = db.Products.Apply(spec, asSplitQuery: false, ignoreAutoIncludes: true);
        queryAutoOnly.Expression.ToString().Should().NotContain("AsSplitQuery").And.Contain("IgnoreAutoIncludes");

        var queryNone = db.Products.Apply(spec, asSplitQuery: false, ignoreAutoIncludes: false);
        queryNone.Expression.ToString().Should().NotContain("AsSplitQuery").And.NotContain("IgnoreAutoIncludes");

        var projectedSpec = new QuerySpec<Product, string>()
            .Where(p => p.IsActive)
            .Select(p => p.Name);

        var projectedBoth = db.Products.Apply(projectedSpec, asSplitQuery: true, ignoreAutoIncludes: true);
        projectedBoth.Expression.ToString().Should().Contain("AsSplitQuery").And.Contain("IgnoreAutoIncludes");
        var projectedResults = await projectedBoth.ToListAsync();
        projectedResults.Should().HaveCount(3);

        var projSplitOnly = db.Products.Apply(projectedSpec, asSplitQuery: true, ignoreAutoIncludes: false);
        projSplitOnly.Expression.ToString().Should().Contain("AsSplitQuery").And.NotContain("IgnoreAutoIncludes");

        var projAutoOnly = db.Products.Apply(projectedSpec, asSplitQuery: false, ignoreAutoIncludes: true);
        projAutoOnly.Expression.ToString().Should().NotContain("AsSplitQuery").And.Contain("IgnoreAutoIncludes");

        var projNone = db.Products.Apply(projectedSpec, asSplitQuery: false, ignoreAutoIncludes: false);
        projNone.Expression.ToString().Should().NotContain("AsSplitQuery").And.NotContain("IgnoreAutoIncludes");

        // Argument null checks
        var act1 = () => QuerySpecEfCoreExtensions.Apply<Product>(null!, spec);
        act1.Should().Throw<ArgumentNullException>().WithParameterName("source");

        var act2 = () => QuerySpecEfCoreExtensions.Apply<Product>(db.Products, null!);
        act2.Should().Throw<ArgumentNullException>().WithParameterName("specification");

        var act3 = () => QuerySpecEfCoreExtensions.Apply<Product, string>(null!, projectedSpec);
        act3.Should().Throw<ArgumentNullException>().WithParameterName("source");

        var act4 = () => QuerySpecEfCoreExtensions.Apply<Product, string>(db.Products, null!);
        act4.Should().Throw<ArgumentNullException>().WithParameterName("specification");
    }

    private sealed class CustomTestEvaluator : ISpecificationEvaluator
    {
        public bool GetQueryCalled { get; private set; }

        public IQueryable<T> GetQuery<T>(IQueryable<T> source, QuerySpec<T> specification)
        {
            GetQueryCalled = true;
            return EfSpecificationEvaluator.GetQuery(source, specification);
        }

        public IQueryable<TResult> GetQuery<T, TResult>(IQueryable<T> source, QuerySpec<T, TResult> specification)
        {
            GetQueryCalled = true;
            return EfSpecificationEvaluator.GetQuery(source, specification);
        }
    }
}








