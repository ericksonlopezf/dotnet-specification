// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Specification;
using EricksonLopez.Specification.MongoDB;
using MongoDB.Driver;
using Xunit;

namespace EricksonLopez.Specification.MongoDB.IntegrationTests;

[Collection("MongoDbCollection")]
[Trait("Category", "Integration")]
public sealed class MongoSpecificationEvaluatorIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;
    private IMongoCollection<TestDocument>? _collection;
    private MongoClient? _client;

    public MongoSpecificationEvaluatorIntegrationTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        if (!_fixture.IsAvailable)
        {
            return;
        }

        _client = new MongoClient(_fixture.ConnectionString);
        var database = _client.GetDatabase("TestDatabase");
        _collection = database.GetCollection<TestDocument>("TestDocuments");

        // Clean up before tests
        await database.DropCollectionAsync("TestDocuments");

        // Seed data
        var docs = new List<TestDocument>
        {
            new() { Id = "1", Name = "Doc A", IsActive = true, Price = 10m, Category = "C1" },
            new() { Id = "2", Name = "Doc B", IsActive = true, Price = 20m, Category = "C1" },
            new() { Id = "3", Name = "Doc C", IsActive = false, Price = 30m, Category = "C2" },
            new() { Id = "4", Name = "Doc D", IsActive = true, Price = 40m, Category = "C2" },
            new() { Id = "5", Name = "Doc E", IsActive = false, Price = 50m, Category = "C3" },
        };

        await _collection.InsertManyAsync(docs);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private IMongoCollection<TestDocument>? GetRequiredCollection()
    {
        if (!_fixture.IsAvailable || _collection is null)
        {
            return null;
        }
        return _collection;
    }

    [SkippableFact]
    public async Task ApplySpecification_WithWhere_ReturnsFilteredResults()
    {
        var collection = GetRequiredCollection();
        Skip.If(collection is null, "MongoDB database is not available.");

        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.IsActive);

        var filter = MongoSpecificationEvaluator.GetFilter(spec);

        var serializerRegistry = global::MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
        var documentSerializer = serializerRegistry.GetSerializer<TestDocument>();
        var rendered = filter.Render(new global::MongoDB.Driver.RenderArgs<TestDocument>(documentSerializer, serializerRegistry));

        var query = collection.Find(filter);
        var filteredQuery = query.ApplySpecification(spec);

        var results = await filteredQuery.ToListAsync();

        results.Should().HaveCount(3);
        results.Should().AllSatisfy(d => d.IsActive.Should().BeTrue());
    }

    [SkippableFact]
    public async Task ApplySpecification_WithMultipleFilters_CombinesCorrectly()
    {
        var collection = GetRequiredCollection();
        Skip.If(collection is null, "MongoDB database is not available.");

        var spec = QuerySpec<TestDocument>.Empty
            .Where(d => d.IsActive)
            .Where(d => d.Price > 15m);

        var filter = MongoSpecificationEvaluator.GetFilter(spec);
        var query = collection.Find(filter);
        var filteredQuery = query.ApplySpecification(spec);

        var results = await filteredQuery.ToListAsync();

        results.Should().HaveCount(2);
        results.Should().Contain(d => d.Id == "2");
        results.Should().Contain(d => d.Id == "4");
    }

    [SkippableFact]
    public async Task ApplySpecification_WithOrderByAndTake_ReturnsCorrectResults()
    {
        var collection = GetRequiredCollection();
        Skip.If(collection is null, "MongoDB database is not available.");

        var spec = QuerySpec<TestDocument>.Empty
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.Price)
            .Take(2);

        var filter = MongoSpecificationEvaluator.GetFilter(spec);
        var query = collection.Find(filter);
        var filteredQuery = query.ApplySpecification(spec);

        var results = await filteredQuery.ToListAsync();

        results.Should().HaveCount(2);
        results[0].Id.Should().Be("4"); // Price 40
        results[1].Id.Should().Be("2"); // Price 20
    }

    [SkippableFact]
    public async Task ApplySpecification_WithSkipAndTake_ReturnsCorrectResults()
    {
        var collection = GetRequiredCollection();
        Skip.If(collection is null, "MongoDB database is not available.");

        var spec = QuerySpec<TestDocument>.Empty
            .OrderBy(d => d.Price)
            .Skip(2)
            .Take(2);

        var filter = MongoSpecificationEvaluator.GetFilter(spec);
        var query = collection.Find(filter);
        var filteredQuery = query.ApplySpecification(spec);

        var results = await filteredQuery.ToListAsync();

        results.Should().HaveCount(2);
        results[0].Id.Should().Be("3"); // Price 30
        results[1].Id.Should().Be("4"); // Price 40
    }
}

public sealed class TestDocument
{
    [global::MongoDB.Bson.Serialization.Attributes.BsonId]
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal Price { get; init; }
    public string Category { get; init; } = string.Empty;
}
