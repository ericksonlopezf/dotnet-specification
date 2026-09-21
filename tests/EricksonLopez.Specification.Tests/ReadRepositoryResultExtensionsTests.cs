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
        public bool ThrowGeneric { get; set; }

        public Task<T?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            if (ThrowGeneric) throw new InvalidOperationException("DB connection failed");
            return Task.FromResult(SingleItem);
        }

        public Task<T?> FirstOrDefaultAsync(QuerySpec<T> specification, CancellationToken cancellationToken = default)
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            if (ThrowGeneric) throw new InvalidOperationException("DB connection failed");
            return Task.FromResult(SingleItem);
        }

        public Task<T?> SingleOrDefaultAsync(QuerySpec<T> specification, CancellationToken cancellationToken = default)
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            if (ThrowGeneric) throw new InvalidOperationException("DB connection failed");
            return Task.FromResult(SingleItem);
        }

        public Task<IReadOnlyList<T>> ListAsync(QuerySpec<T> specification, CancellationToken cancellationToken = default)
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            if (ThrowGeneric) throw new InvalidOperationException("DB connection failed");
            return Task.FromResult(ListItems);
        }

        public Task<IReadOnlyList<TResult>> ListAsync<TResult>(QuerySpec<T, TResult> specification, CancellationToken cancellationToken = default)
        {
            if (ThrowCancellation) throw new OperationCanceledException();
            if (ThrowGeneric) throw new InvalidOperationException("DB connection failed");
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
}
