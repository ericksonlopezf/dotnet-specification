// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using NSubstitute;
using Xunit;

namespace EricksonLopez.Specification.Tests;

/// <summary>
/// Verifies contract fidelity, substitutability, and interface behavior for Abstractions interfaces.
/// </summary>
public sealed class AbstractionsContractTests
{
    [Fact]
    public void ISpecification_CanBeImplemented_AndEvaluated()
    {
        var spec = Substitute.For<ISpecification<Customer>>();
        var customer = new Customer { Id = 1, Name = "Alice", IsActive = true };

        spec.IsSatisfiedBy(customer).Returns(true);

        spec.IsSatisfiedBy(customer).Should().BeTrue();
        spec.Received(1).IsSatisfiedBy(customer);
    }

    [Fact]
    public async Task IReadRepository_AllMethods_AreSubstitutable()
    {
        var repo = Substitute.For<IReadRepository<Customer>>();
        var spec = QuerySpec<Customer>.Empty.Where(c => c.IsActive);
        var projectedSpec = QuerySpec<Customer, string>.Empty.Select(c => c.Name);
        var customer = new Customer { Id = 1, Name = "Alice", IsActive = true };
        var customerList = new List<Customer> { customer };
        var nameList = new List<string> { "Alice" };

        repo.FirstOrDefaultAsync(spec, Arg.Any<CancellationToken>()).Returns(customer);
        repo.ListAsync(spec, Arg.Any<CancellationToken>()).Returns(customerList);
        repo.CountAsync(spec, Arg.Any<CancellationToken>()).Returns(1);
        repo.AnyAsync(spec, Arg.Any<CancellationToken>()).Returns(true);
        repo.SingleOrDefaultAsync(spec, Arg.Any<CancellationToken>()).Returns(customer);
        repo.ListAsync(projectedSpec, Arg.Any<CancellationToken>()).Returns(nameList);

        var first = await repo.FirstOrDefaultAsync(spec);
        var list = await repo.ListAsync(spec);
        var count = await repo.CountAsync(spec);
        var any = await repo.AnyAsync(spec);
        var single = await repo.SingleOrDefaultAsync(spec);
        var projectedList = await repo.ListAsync(projectedSpec);

        first.Should().Be(customer);
        list.Should().ContainSingle().Which.Should().Be(customer);
        count.Should().Be(1);
        any.Should().BeTrue();
        single.Should().Be(customer);
        projectedList.Should().ContainSingle().Which.Should().Be("Alice");
    }

    [Fact]
    public async Task IReadRepository_GetByIdAsync_DefaultImplementation_ReturnsNull()
    {
        IReadRepository<Customer> repo = new DefaultReadRepo();
        var result = await repo.GetByIdAsync(123);
        result.Should().BeNull();
    }

    private sealed class DefaultReadRepo : IReadRepository<Customer>
    {
        public Task<Customer?> FirstOrDefaultAsync(QuerySpec<Customer> spec, CancellationToken cancellationToken = default) => Task.FromResult<Customer?>(null);
        public Task<IReadOnlyList<Customer>> ListAsync(QuerySpec<Customer> spec, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Customer>>(Array.Empty<Customer>());
        public Task<int> CountAsync(QuerySpec<Customer> spec, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> AnyAsync(QuerySpec<Customer> spec, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Customer?> SingleOrDefaultAsync(QuerySpec<Customer> spec, CancellationToken cancellationToken = default) => Task.FromResult<Customer?>(null);
        public Task<IReadOnlyList<TResult>> ListAsync<TResult>(QuerySpec<Customer, TResult> spec, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TResult>>(Array.Empty<TResult>());
    }
}





