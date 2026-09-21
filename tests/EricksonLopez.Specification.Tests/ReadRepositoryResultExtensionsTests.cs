// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Specification;
using EricksonLopez.Specification.Result;
using Xunit;

namespace EricksonLopez.Specification.Tests;

public sealed class ReadRepositoryResultExtensionsTests
{
    private sealed class FakeRepository<T> : IReadRepository<T> where T : class
    {
        public T? SingleItem { get; set; }
        public IReadOnlyList<T> ListItems { get; set; } = [];
        public bool ThrowCancellation { get; set; }
        public bool ThrowInvalidOperation { get; set; }
        public bool ThrowGeneric { get; set; }

        public Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            if (ThrowInvalidOperation) throw new InvalidOperationException("DB invalid operation");
            if (ThrowGeneric) throw new Exception("DB connection failed");
            return Task.FromResult(SingleItem);
        }

        public Task<T?> FirstOrDefaultAsync(QuerySpec<T> specification, CancellationToken cancellationToken = default)
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            if (ThrowInvalidOperation) throw new InvalidOperationException("DB invalid operation");
            if (ThrowGeneric) throw new Exception("DB connection failed");
            return Task.FromResult(SingleItem);
        }

        public Task<T?> SingleOrDefaultAsync(QuerySpec<T> specification, CancellationToken cancellationToken = default)
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            if (ThrowInvalidOperation) throw new InvalidOperationException("DB invalid operation");
            if (ThrowGeneric) throw new Exception("DB connection failed");
            return Task.FromResult(SingleItem);
        }

        public Task<IReadOnlyList<T>> ListAsync(QuerySpec<T> specification, CancellationToken cancellationToken = default)
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            if (ThrowInvalidOperation) throw new InvalidOperationException("DB invalid operation");
            if (ThrowGeneric) throw new Exception("DB connection failed");
            return Task.FromResult(ListItems);
        }

        public Task<IReadOnlyList<TResult>> ListAsync<TResult>(QuerySpec<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            if (ThrowInvalidOperation) throw new InvalidOperationException("DB invalid operation");
            if (ThrowGeneric) throw new Exception("DB connection failed");
            return Task.FromResult<IReadOnlyList<TResult>>([]);
        }

        public Task<int> CountAsync(QuerySpec<T> specification, CancellationToken cancellationToken = default)
            => Task.FromResult(ListItems.Count);

        public Task<bool> AnyAsync(QuerySpec<T> specification, CancellationToken cancellationToken = default)
            => Task.FromResult(ListItems.Count > 0);
    }

    public sealed class Item
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    [Fact]
    public async Task ExtensionMethods_NullRepository_ThrowsArgumentNullException()
    {
        IReadRepository<Item> repo = null!;
        var spec = QuerySpec<Item>.Empty;

        var act1 = () => repo.FirstOrDefaultResultAsync(spec);
        await act1.Should().ThrowAsync<ArgumentNullException>();

        var act2 = () => repo.ListResultAsync(spec);
        await act2.Should().ThrowAsync<ArgumentNullException>();

        var act3 = () => repo.SingleOrDefaultResultAsync(spec);
        await act3.Should().ThrowAsync<ArgumentNullException>();

        var act4 = () => repo.GetByIdResultAsync(42);
        await act4.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task FirstOrDefaultResultAsync_Found_ReturnsSuccess()
    {
        var repo = new FakeRepository<Item> { SingleItem = new Item { Id = 1, Name = "Item 1" } };
        var spec = QuerySpec<Item>.Empty;

        var result = await repo.FirstOrDefaultResultAsync(spec);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Item 1");
    }

    [Fact]
    public async Task FirstOrDefaultResultAsync_NotFound_ReturnsNotFoundFailure()
    {
        var repo = new FakeRepository<Item> { SingleItem = null };
        var spec = QuerySpec<Item>.Empty;

        var result = await repo.FirstOrDefaultResultAsync(spec);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Item.NotFound");
        result.Error.Description.Should().Be("No Item found matching the specification.");
    }

    [Fact]
    public async Task FirstOrDefaultResultAsync_OperationCanceled_RethrowsException()
    {
        var repo = new FakeRepository<Item> { ThrowCancellation = true };
        var spec = QuerySpec<Item>.Empty;

        var act = () => repo.FirstOrDefaultResultAsync(spec);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task FirstOrDefaultResultAsync_GenericException_ReturnsFailure()
    {
        var repo = new FakeRepository<Item> { ThrowGeneric = true };
        var spec = QuerySpec<Item>.Empty;

        var result = await repo.FirstOrDefaultResultAsync(spec);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Database.Error");
        result.Error.Description.Should().Be("DB connection failed");
    }

    [Fact]
    public async Task ListResultAsync_ReturnsList()
    {
        var repo = new FakeRepository<Item>
        {
            ListItems = new List<Item> { new() { Id = 1 }, new() { Id = 2 } }
        };

        var result = await repo.ListResultAsync(QuerySpec<Item>.Empty);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListResultAsync_OperationCanceled_RethrowsException()
    {
        var repo = new FakeRepository<Item> { ThrowCancellation = true };
        var spec = QuerySpec<Item>.Empty;

        var act = () => repo.ListResultAsync(spec);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ListResultAsync_GenericException_ReturnsFailure()
    {
        var repo = new FakeRepository<Item> { ThrowGeneric = true };
        var spec = QuerySpec<Item>.Empty;

        var result = await repo.ListResultAsync(spec);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Database.Error");
        result.Error.Description.Should().Be("DB connection failed");
    }

    [Fact]
    public async Task SingleOrDefaultResultAsync_Found_ReturnsSuccess()
    {
        var repo = new FakeRepository<Item> { SingleItem = new Item { Id = 1, Name = "Item 1" } };
        var spec = QuerySpec<Item>.Empty;

        var result = await repo.SingleOrDefaultResultAsync(spec);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Item 1");
    }

    [Fact]
    public async Task SingleOrDefaultResultAsync_NotFound_ReturnsNotFoundFailure()
    {
        var repo = new FakeRepository<Item> { SingleItem = null };
        var spec = QuerySpec<Item>.Empty;

        var result = await repo.SingleOrDefaultResultAsync(spec);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Item.NotFound");
        result.Error.Description.Should().Be("No Item found matching the specification.");
    }

    [Fact]
    public async Task SingleOrDefaultResultAsync_OperationCanceled_RethrowsException()
    {
        var repo = new FakeRepository<Item> { ThrowCancellation = true };
        var spec = QuerySpec<Item>.Empty;

        var act = () => repo.SingleOrDefaultResultAsync(spec);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SingleOrDefaultResultAsync_MultipleMatches_ReturnsConflictFailure()
    {
        var repo = new FakeRepository<Item> { ThrowInvalidOperation = true };
        var spec = QuerySpec<Item>.Empty;

        var result = await repo.SingleOrDefaultResultAsync(spec);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Database.MultipleMatches");
        result.Error.Description.Should().Be("Multiple entities found matching the specification.");
    }

    [Fact]
    public async Task SingleOrDefaultResultAsync_GenericException_ReturnsFailure()
    {
        var repo = new FakeRepository<Item> { ThrowGeneric = true };
        var spec = QuerySpec<Item>.Empty;

        var result = await repo.SingleOrDefaultResultAsync(spec);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Database.Error");
        result.Error.Description.Should().Be("DB connection failed");
    }

    [Fact]
    public async Task GetByIdResultAsync_Found_ReturnsSuccess()
    {
        var repo = new FakeRepository<Item> { SingleItem = new Item { Id = 42, Name = "Item 42" } };

        var result = await repo.GetByIdResultAsync(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Item 42");
    }

    [Fact]
    public async Task GetByIdResultAsync_NotFound_ReturnsNotFoundFailure()
    {
        var repo = new FakeRepository<Item> { SingleItem = null };

        var result = await repo.GetByIdResultAsync(42);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Item.NotFound");
        result.Error.Description.Should().Be("No Item found with ID 42.");
    }

    [Fact]
    public async Task GetByIdResultAsync_OperationCanceled_RethrowsException()
    {
        var repo = new FakeRepository<Item> { ThrowCancellation = true };

        var act = () => repo.GetByIdResultAsync(42);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetByIdResultAsync_GenericException_ReturnsFailure()
    {
        var repo = new FakeRepository<Item> { ThrowGeneric = true };

        var result = await repo.GetByIdResultAsync(42);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Database.Error");
        result.Error.Description.Should().Be("DB connection failed");
    }
}
