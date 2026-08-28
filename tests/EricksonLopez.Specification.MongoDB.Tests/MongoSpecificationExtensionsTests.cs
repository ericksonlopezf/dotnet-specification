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

public sealed class MongoSpecificationExtensionsTests
{
    [Fact]
    public async Task FindAsync_WhenCollectionNull_ThrowsArgumentNullException()
    {
        var spec = QuerySpec<TestDocument>.Empty;
        IMongoCollection<TestDocument> collection = null!;

        Func<Task> act = async () => await collection.FindAsync(spec);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task FindAsync_WhenSpecificationNull_ThrowsArgumentNullException()
    {
        var collection = Substitute.For<IMongoCollection<TestDocument>>();

        Func<Task> act = async () => await MongoSpecificationExtensions.FindAsync(collection, null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WhenCollectionNull_ThrowsArgumentNullException()
    {
        var spec = QuerySpec<TestDocument>.Empty;
        IMongoCollection<TestDocument> collection = null!;

        Func<Task> act = async () => await collection.FirstOrDefaultAsync(spec);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task FirstOrDefaultAsync_WhenSpecificationNull_ThrowsArgumentNullException()
    {
        var collection = Substitute.For<IMongoCollection<TestDocument>>();

        Func<Task> act = async () => await collection.FirstOrDefaultAsync<TestDocument>(null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CountDocumentsAsync_WhenCollectionNull_ThrowsArgumentNullException()
    {
        var spec = QuerySpec<TestDocument>.Empty;
        IMongoCollection<TestDocument> collection = null!;

        Func<Task> act = async () => await collection.CountDocumentsAsync(spec);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CountDocumentsAsync_WhenSpecificationNull_ThrowsArgumentNullException()
    {
        var collection = Substitute.For<IMongoCollection<TestDocument>>();

        Func<Task> act = async () => await collection.CountDocumentsAsync<TestDocument>(null!);
        await act.Should().ThrowExactlyAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CountDocumentsAsync_DelegatesToCollection_WithFilterAndCancellationToken()
    {
        var collection = Substitute.For<IMongoCollection<TestDocument>>();
        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.IsActive);
        using var cts = new CancellationTokenSource();

        collection.CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(42L));

        var count = await collection.CountDocumentsAsync(spec, cts.Token);

        count.Should().Be(42L);
        await collection.Received(1).CountDocumentsAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<CountOptions>(),
            cts.Token);
    }

    [Fact]
    public async Task FindAsync_ExecutesFindFluentAndReturnsDocumentList()
    {
        var cursor = Substitute.For<IAsyncCursor<TestDocument>>();
        var docs = new List<TestDocument>
        {
            new() { Id = "1", Name = "Doc1", IsActive = true },
            new() { Id = "2", Name = "Doc2", IsActive = true }
        };
        cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));
        cursor.Current.Returns(docs);

        var collection = Substitute.For<IMongoCollection<TestDocument>>();
        collection.FindAsync(
            Arg.Any<FilterDefinition<TestDocument>>(),
            Arg.Any<FindOptions<TestDocument, TestDocument>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(cursor));

        var spec = QuerySpec<TestDocument>.Empty
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Page(1, 10);

        using var cts = new CancellationTokenSource();
        var results = await collection.FindAsync(spec, cts.Token);

        results.Should().HaveCount(2);
        results[0].Name.Should().Be("Doc1");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ExecutesFindFluentAndReturnsFirstDocument()
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

        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.Name == "Doc1");

        using var cts = new CancellationTokenSource();
        var result = await collection.FirstOrDefaultAsync(spec, cts.Token);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Doc1");
    }
}
