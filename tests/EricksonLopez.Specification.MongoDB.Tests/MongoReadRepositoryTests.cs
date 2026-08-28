// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Specification;
using EricksonLopez.Specification.MongoDB;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Specification.MongoDB.Tests;

public sealed class MongoReadRepositoryTests
{
    [Fact]
    public void Constructor_WhenCollectionNull_ThrowsArgumentNullException()
    {
        var act = () => new MongoReadRepository<TestDocument>(null!);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValidCollection_InitializesSuccessfully()
    {
        var collection = Substitute.For<IMongoCollection<TestDocument>>();
        var repository = new MongoReadRepository<TestDocument>(collection);

        repository.Should().NotBeNull();
    }

    [Fact]
    public async Task ListAsync_WhenSpecificationNull_ThrowsArgumentNullException()
    {
        var collection = Substitute.For<IMongoCollection<TestDocument>>();
        var repository = new MongoReadRepository<TestDocument>(collection);

        Func<Task> act = async () => await repository.ListAsync(null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WhenSpecificationNull_ThrowsArgumentNullException()
    {
        var collection = Substitute.For<IMongoCollection<TestDocument>>();
        var repository = new MongoReadRepository<TestDocument>(collection);

        Func<Task> act = async () => await repository.FirstOrDefaultAsync(null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CountAsync_WhenSpecificationNull_ThrowsArgumentNullException()
    {
        var collection = Substitute.For<IMongoCollection<TestDocument>>();
        var repository = new MongoReadRepository<TestDocument>(collection);

        Func<Task> act = async () => await repository.CountAsync(null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CountAsync_ValidSpecification_DelegatesToCollection()
    {
        var collection = Substitute.For<IMongoCollection<TestDocument>>();
        var repository = new MongoReadRepository<TestDocument>(collection);
        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.IsActive);
        using var cts = new CancellationTokenSource();

        collection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(15L));

        var count = await repository.CountAsync(spec, cts.Token);

        count.Should().Be(15L);
        await collection.Received(1).CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            cts.Token);
    }

    [Fact]
    public async Task ListAsync_ValidSpecification_DelegatesToCollection()
    {
        var cursor = Substitute.For<IAsyncCursor<TestDocument>>();
        var docs = new List<TestDocument>
        {
            new() { Id = "1", Name = "Doc1", IsActive = true }
        };
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        cursor.Current.Returns(docs);

        var collection = Substitute.For<IMongoCollection<TestDocument>>();
        collection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));

        var repository = new MongoReadRepository<TestDocument>(collection);
        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.IsActive);
        using var cts = new CancellationTokenSource();

        var results = await repository.ListAsync(spec, cts.Token);

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Doc1");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ValidSpecification_DelegatesToCollection()
    {
        var cursor = Substitute.For<IAsyncCursor<TestDocument>>();
        var docs = new List<TestDocument>
        {
            new() { Id = "1", Name = "Doc1", IsActive = true }
        };
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        cursor.Current.Returns(docs);

        var collection = Substitute.For<IMongoCollection<TestDocument>>();
        collection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));

        var repository = new MongoReadRepository<TestDocument>(collection);
        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.Name == "Doc1");
        using var cts = new CancellationTokenSource();

        var result = await repository.FirstOrDefaultAsync(spec, cts.Token);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Doc1");
    }
}
