// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Specification.Showcase.Domain;

/// <summary>
/// Mock implementation of <see cref="IReadRepository{Customer}"/> for showcase execution.
/// </summary>
public sealed class MockCustomerRepository : IReadRepository<Customer>
{
    private static readonly List<Customer> _store =
    [
        new Customer { Name = "Alice",   IsActive = true,  TotalPurchases = 30, CreditLimit = 12_000m },
        new Customer { Name = "Bob",     IsActive = false, TotalPurchases = 2,  CreditLimit = 500m    },
        new Customer { Name = "Charlie", IsActive = true,  TotalPurchases = 15, CreditLimit = 6_000m  },
        new Customer { Name = "Diana",   IsActive = true,  TotalPurchases = 45, CreditLimit = 20_000m },
    ];

    private static IReadOnlyList<Customer> Filter(QuerySpec<Customer> spec)
    {
        Expression<Func<Customer, bool>>? predicate = null;

        if (!spec.Criteria.IsEmpty)
        {
            predicate = spec.Criteria.Length == 1
                ? spec.Criteria[0]
                : ExpressionComposer.AndAll<Customer>(spec.Criteria.AsSpan());
        }

        IEnumerable<Customer> result = _store;

        if (predicate != null)
        {
            var compiled = predicate.Compile();
            result = Enumerable.Where(result, compiled);
        }

        foreach (var order in spec.OrderClauses)
        {
            var orderKey = order.KeySelector.Compile();
            result = order.Direction == OrderDirection.Ascending
                ? Enumerable.OrderBy(result, c => orderKey(c))
                : Enumerable.OrderByDescending(result, c => orderKey(c));
        }

        if (spec.SkipCount.HasValue) result = Enumerable.Skip(result, spec.SkipCount.Value);
        if (spec.TakeCount.HasValue) result = Enumerable.Take(result, spec.TakeCount.Value);

        return result.ToList();
    }

    /// <inheritdoc/>
    public Task<Customer?> FirstOrDefaultAsync(QuerySpec<Customer> specification, CancellationToken cancellationToken = default)
        => Task.FromResult(Filter(specification).FirstOrDefault());

    /// <inheritdoc/>
    public Task<Customer?> SingleOrDefaultAsync(QuerySpec<Customer> specification, CancellationToken cancellationToken = default)
    {
        var results = Filter(specification);
        if (results.Count > 1) throw new InvalidOperationException("Sequence contains more than one element.");
        return Task.FromResult(results.FirstOrDefault());
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Customer>> ListAsync(QuerySpec<Customer> specification, CancellationToken cancellationToken = default)
        => Task.FromResult(Filter(specification));

    /// <inheritdoc/>
    public Task<int> CountAsync(QuerySpec<Customer> specification, CancellationToken cancellationToken = default)
    {
        var countSpec = specification with { OrderClauses = [], SkipCount = null, TakeCount = null };
        return Task.FromResult(Filter(countSpec).Count);
    }

    /// <inheritdoc/>
    public Task<bool> AnyAsync(QuerySpec<Customer> specification, CancellationToken cancellationToken = default)
    {
        var anySpec = specification with { OrderClauses = [], SkipCount = null, TakeCount = 1 };
        return Task.FromResult(Filter(anySpec).Count > 0);
    }

    /// <inheritdoc/>
    public Task<Customer?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull
    {
        if (id is Guid guidId)
            return Task.FromResult(_store.FirstOrDefault(c => c.Id == guidId));
        return Task.FromResult<Customer?>(null);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<TResult>> ListAsync<TResult>(
        QuerySpec<Customer, TResult> specification,
        CancellationToken cancellationToken = default)
    {
        if (specification.Selector == null)
            throw new InvalidOperationException("QuerySpec<T, TResult> requires a Selector.");

        IEnumerable<Customer> result = _store;

        if (!specification.Criteria.IsEmpty)
        {
            var predicate = specification.Criteria.Length == 1
                ? specification.Criteria[0]
                : ExpressionComposer.AndAll<Customer>(specification.Criteria.AsSpan());
            result = Enumerable.Where(result, predicate.Compile());
        }

        if (specification.SkipCount.HasValue) result = Enumerable.Skip(result, specification.SkipCount.Value);
        if (specification.TakeCount.HasValue) result = Enumerable.Take(result, specification.TakeCount.Value);

        var selector = specification.Selector.Compile();
        IReadOnlyList<TResult> projected = Enumerable.Select(result, selector).ToList();

        return Task.FromResult(projected);
    }
}
