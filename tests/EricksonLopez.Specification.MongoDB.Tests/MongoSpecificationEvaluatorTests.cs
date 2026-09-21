// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.Specification;
using EricksonLopez.Specification.MongoDB;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Specification.MongoDB.Tests;

public sealed class TestDocument
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal Price { get; init; }
    public string Category { get; init; } = string.Empty;
}

public sealed class MongoSpecificationEvaluatorTests
{
    private static BsonDocument RenderFilter(FilterDefinition<TestDocument> filter)
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<TestDocument>();
        return filter.Render(new RenderArgs<TestDocument>(serializer, BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument RenderSort(SortDefinition<TestDocument> sort)
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<TestDocument>();
        return sort.Render(new RenderArgs<TestDocument>(serializer, BsonSerializer.SerializerRegistry));
    }

    #region GetFilter Tests

    [Fact]
    public void GetFilter_WhenSpecificationNull_ThrowsArgumentNullException()
    {
        var act = () => MongoSpecificationEvaluator.GetFilter<TestDocument>(null!);
        act.Should().ThrowExactly<ArgumentNullException>();
    }

    [Fact]
    public void GetFilter_WithEmptySpecification_ReturnsEmptyFilter()
    {
        var spec = QuerySpec<TestDocument>.Empty;
        var filter = MongoSpecificationEvaluator.GetFilter(spec);

        var rendered = RenderFilter(filter);
        rendered.ElementCount.Should().Be(0);
    }

    [Fact]
    public void GetFilter_WithSingleCriterion_BuildsCorrectFilter()
    {
        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.IsActive);
        var filter = MongoSpecificationEvaluator.GetFilter(spec);

        var rendered = RenderFilter(filter);
        rendered.Contains("IsActive").Should().BeTrue();
        rendered["IsActive"].AsBoolean.Should().BeTrue();
    }

    [Fact]
    public void GetFilter_WithMultipleCriteria_CombinesFiltersWithAnd()
    {
        var spec = QuerySpec<TestDocument>.Empty
            .Where(d => d.IsActive)
            .Where(d => d.Price > 100m)
            .Where(d => d.Category == "Electronics");

        var filter = MongoSpecificationEvaluator.GetFilter(spec);
        var rendered = RenderFilter(filter);

        rendered.Should().NotBeNull();
        var renderedString = rendered.ToString();
        renderedString.Should().Contain("IsActive");
        renderedString.Should().Contain("Price");
        renderedString.Should().Contain("Category");
        rendered.Contains("$or").Should().BeFalse();
        renderedString.Should().NotContain("$or");
    }

    #endregion

    #region GetSort Tests

    [Fact]
    public void GetSort_WhenSpecificationNull_ThrowsArgumentNullException()
    {
        var act = () => MongoSpecificationEvaluator.GetSort<TestDocument>(null!);
        act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("specification");
    }

    [Fact]
    public void GetSort_WithNoOrderClauses_ReturnsNull()
    {
        var spec = QuerySpec<TestDocument>.Empty;
        var sort = MongoSpecificationEvaluator.GetSort(spec);

        sort.Should().BeNull();
    }

    [Fact]
    public void GetSort_WithSingleAscendingClause_BuildsAscendingSort()
    {
        var spec = QuerySpec<TestDocument>.Empty.OrderBy(d => d.Name);
        var sort = MongoSpecificationEvaluator.GetSort(spec);

        sort.Should().NotBeNull();
        var rendered = RenderSort(sort!);
        rendered.Contains("Name").Should().BeTrue();
        rendered["Name"].AsInt32.Should().Be(1);
    }

    [Fact]
    public void GetSort_WithSingleDescendingClause_BuildsDescendingSort()
    {
        var spec = QuerySpec<TestDocument>.Empty.OrderByDescending(d => d.Price);
        var sort = MongoSpecificationEvaluator.GetSort(spec);

        sort.Should().NotBeNull();
        var rendered = RenderSort(sort!);
        rendered.Contains("Price").Should().BeTrue();
        rendered["Price"].AsInt32.Should().Be(-1);
    }

    [Fact]
    public void GetSort_WithMultipleClauses_CombinesSortsCorrectly()
    {
        var spec = QuerySpec<TestDocument>.Empty
            .OrderBy(d => d.Category)
            .OrderByDescending(d => d.Price);

        var sort = MongoSpecificationEvaluator.GetSort(spec);

        sort.Should().NotBeNull();
        var rendered = RenderSort(sort!);
        rendered.Contains("Category").Should().BeTrue();
        rendered.Contains("Price").Should().BeTrue();
        rendered["Category"].AsInt32.Should().Be(1);
        rendered["Price"].AsInt32.Should().Be(-1);
    }

    #endregion

    #region ApplySpecification Tests

    [Fact]
    public void ApplySpecification_WhenFindFluentNull_ThrowsArgumentNullException()
    {
        var spec = QuerySpec<TestDocument>.Empty;
        IFindFluent<TestDocument, TestDocument> findFluent = null!;

        var act = () => findFluent.ApplySpecification(spec);
        act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("findFluent");
    }

    [Fact]
    public void ApplySpecification_WhenSpecificationNull_ThrowsArgumentNullException()
    {
        var findFluent = Substitute.For<IFindFluent<TestDocument, TestDocument>>();

        var act = () => findFluent.ApplySpecification<TestDocument>(null!);
        act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("specification");
    }

    [Fact]
    public void ApplySpecification_WithSortSkipAndLimit_AppliesAllCorrectly()
    {
        var spec = QuerySpec<TestDocument>.Empty
            .OrderBy(d => d.Name)
            .Skip(10)
            .Take(25);

        var findFluent = Substitute.For<IFindFluent<TestDocument, TestDocument>>();
        findFluent.Sort(Arg.Any<SortDefinition<TestDocument>>()).Returns(findFluent);
        findFluent.Skip(Arg.Any<int>()).Returns(findFluent);
        findFluent.Limit(Arg.Any<int>()).Returns(findFluent);

        var result = findFluent.ApplySpecification(spec);

        result.Should().BeSameAs(findFluent);
        findFluent.Received(1).Sort(Arg.Any<SortDefinition<TestDocument>>());
        findFluent.Received(1).Skip(10);
        findFluent.Received(1).Limit(25);
    }

    [Fact]
    public void ApplySpecification_WithoutSortOrPagination_DoesNotCallSortSkipOrLimit()
    {
        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.IsActive);

        var findFluent = Substitute.For<IFindFluent<TestDocument, TestDocument>>();

        var result = findFluent.ApplySpecification(spec);

        result.Should().BeSameAs(findFluent);
        findFluent.DidNotReceive().Sort(Arg.Any<SortDefinition<TestDocument>>());
        findFluent.DidNotReceive().Skip(Arg.Any<int>());
        findFluent.DidNotReceive().Limit(Arg.Any<int>());
    }

    [Fact]
    public void ApplySpecification_WithNullFilter_SetsFilterDirectly()
    {
        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.IsActive);
        var findFluent = Substitute.For<IFindFluent<TestDocument, TestDocument>>();
        findFluent.Filter = null;

        var result = findFluent.ApplySpecification(spec);

        result.Should().BeSameAs(findFluent);
        findFluent.Filter.Should().NotBeNull();
        var rendered = RenderFilter(findFluent.Filter);
        rendered.Contains("IsActive").Should().BeTrue();
        rendered["IsActive"].AsBoolean.Should().BeTrue();
    }

    [Fact]
    public void ApplySpecification_WithEmptyFilter_SetsFilterDirectly()
    {
        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.IsActive);
        var findFluent = Substitute.For<IFindFluent<TestDocument, TestDocument>>();
        findFluent.Filter = Builders<TestDocument>.Filter.Empty;

        var result = findFluent.ApplySpecification(spec);

        result.Should().BeSameAs(findFluent);
        findFluent.Filter.Should().NotBeNull();
        var rendered = RenderFilter(findFluent.Filter);
        rendered.Contains("IsActive").Should().BeTrue();
        rendered["IsActive"].AsBoolean.Should().BeTrue();
    }

    [Fact]
    public void ApplySpecification_WithExistingFilter_CombinesFiltersWithAnd()
    {
        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.IsActive);
        var findFluent = Substitute.For<IFindFluent<TestDocument, TestDocument>>();
        findFluent.Filter = Builders<TestDocument>.Filter.Eq(d => d.Category, "Books");

        var result = findFluent.ApplySpecification(spec);

        result.Should().BeSameAs(findFluent);
        findFluent.Filter.Should().NotBeNull();
        var rendered = RenderFilter(findFluent.Filter);
        rendered.Contains("Category").Should().BeTrue();
        rendered["Category"].AsString.Should().Be("Books");
        rendered.Contains("IsActive").Should().BeTrue();
        rendered["IsActive"].AsBoolean.Should().BeTrue();
    }

    #endregion

    #region Find Tests

    [Fact]
    public void Find_WhenCollectionNull_ThrowsArgumentNullException()
    {
        var spec = QuerySpec<TestDocument>.Empty;
        IMongoCollection<TestDocument> collection = null!;

        var act = () => collection.Find(spec);
        act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("collection");
    }

    [Fact]
    public void Find_WhenSpecificationNull_ThrowsArgumentNullException()
    {
        var collection = Substitute.For<IMongoCollection<TestDocument>>();

        var act = () => collection.Find((QuerySpec<TestDocument>)null!);
        act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("specification");
    }

    [Fact]
    public void Find_ValidSpecification_ReturnsConfiguredFindFluent()
    {
        var spec = QuerySpec<TestDocument>.Empty.Where(d => d.IsActive).OrderBy(d => d.Name);
        var collection = Substitute.For<IMongoCollection<TestDocument>>();

        var result = collection.Find(spec);

        result.Should().NotBeNull();
        result.Filter.Should().NotBeNull();
        var rendered = RenderFilter(result.Filter);
        rendered.ToString().Should().Contain("IsActive");
    }

    #endregion
}
