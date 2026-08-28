// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EricksonLopez.Specification.Sql;

/// <summary>
/// Provides a bounded LRU (Least Recently Used) cache for generated <see cref="QueryModel"/> plans,
/// keyed by expression structural equality and table name.
/// </summary>
/// <remarks>
/// Prevents unbounded memory consumption by enforcing a strict capacity limit (default: 512 plans).
/// When capacity is exceeded, the least recently used query plans are evicted in O(1) time.
/// </remarks>
public static class QueryPlanCache
{
    private const int DefaultCapacity = 512;
    private static int _capacity = DefaultCapacity;

    private static readonly object _sync = new();
    private static readonly Dictionary<CacheKey, LinkedListNode<CacheEntry>> _map = new();
    private static readonly LinkedList<CacheEntry> _lruList = new();

    /// <summary>
    /// Gets or sets the maximum number of query plans retained in the cache.
    /// Default is 512.
    /// </summary>
    public static int Capacity
    {
        get
        {
            lock (_sync)
            {
                return _capacity;
            }
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            lock (_sync)
            {
                _capacity = value;
                while (_map.Count > _capacity)
                {
                    var last = _lruList.Last!;
                    _lruList.RemoveLast();
                    _map.Remove(last.Value.Key);
                }
            }

        }
    }

    /// <summary>
    /// Gets the current number of query plans held in the cache.
    /// </summary>
    public static int Count
    {
        get
        {
            lock (_sync)
            {
                return _map.Count;
            }
        }
    }

    /// <summary>
    /// Attempts to retrieve a cached <see cref="QueryModel"/> for the specified criteria and table.
    /// </summary>
    /// <param name="criteria">The expression criteria to look up.</param>
    /// <param name="tableName">The target database table name.</param>
    /// <param name="plan">The resolved query plan if found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a cached plan was found; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="criteria"/> or <paramref name="tableName"/> is <see langword="null"/></exception>
    public static bool TryGetPlan(Expression criteria, string tableName, out QueryModel plan)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(tableName);

        var key = new CacheKey(criteria, tableName);
        lock (_sync)
        {
            if (_map.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                if (node != _lruList.First)
                {
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                }
                plan = node.Value.Plan;
                return true;
            }
        }

        plan = null!;
        return false;
    }

    /// <summary>
    /// Stores or updates a <see cref="QueryModel"/> in the LRU cache.
    /// </summary>
    /// <param name="criteria">The expression criteria.</param>
    /// <param name="tableName">The target database table name.</param>
    /// <param name="plan">The translated query model to cache.</param>
    /// <exception cref="ArgumentNullException"><paramref name="criteria"/>, <paramref name="tableName"/>, or <paramref name="plan"/> is <see langword="null"/></exception>
    public static void SetPlan(Expression criteria, string tableName, QueryModel plan)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(tableName);
        ArgumentNullException.ThrowIfNull(plan);

        var key = new CacheKey(criteria, tableName);
        lock (_sync)
        {
            if (_map.TryGetValue(key, out var existingNode))
            {
                existingNode.Value = new CacheEntry(key, plan);
                if (existingNode != _lruList.First)
                {
                    _lruList.Remove(existingNode);
                    _lruList.AddFirst(existingNode);
                }
                return;
            }

            // Evict LRU item if at capacity
            if (_map.Count >= _capacity)
            {
                var oldest = _lruList.Last!;
                _lruList.RemoveLast();
                _map.Remove(oldest.Value.Key);
            }


            var entry = new CacheEntry(key, plan);
            var newNode = new LinkedListNode<CacheEntry>(entry);
            _lruList.AddFirst(newNode);
            _map[key] = newNode;
        }
    }

    /// <summary>
    /// Clears all entries from the query plan cache.
    /// </summary>
    public static void Clear()
    {
        lock (_sync)
        {
            _map.Clear();
            _lruList.Clear();
        }
    }

    private sealed record CacheEntry(CacheKey Key, QueryModel Plan);

    internal readonly struct CacheKey : IEquatable<CacheKey>
    {
        private readonly Expression _criteria;
        private readonly string _tableName;
        private readonly int _hashCode;

        public CacheKey(Expression criteria, string tableName)
        {
            _criteria = criteria;
            _tableName = tableName;
            _hashCode = HashCode.Combine(
                ExpressionEqualityComparer.Default.GetHashCode(criteria),
                tableName.GetHashCode(StringComparison.Ordinal));
        }

        public bool Equals(CacheKey other)
        {
            return _tableName == other._tableName &&
                   ExpressionEqualityComparer.Default.Equals(_criteria, other._criteria);
        }

        public override bool Equals(object? obj) => obj is CacheKey other && Equals(other);

        public override int GetHashCode() => _hashCode;
    }
}


