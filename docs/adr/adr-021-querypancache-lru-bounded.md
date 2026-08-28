# adr-021: QueryPlanCache Must Be Bounded (LRU Strategy)

**Status**: Accepted  
**Date**: 2026-08-14  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: Infrastructure / SQL / Memory Safety

---

## Context

`QueryPlanCache` provides SQL translation caching via a `ConcurrentDictionary<CacheKey, QueryModel>`:

```csharp
private static readonly ConcurrentDictionary<CacheKey, QueryModel> Cache = new();
```

The existing code itself acknowledged the limitation (translated from legacy code comment):

```csharp
// In a real bounded cache implementation an LRU would be used,
// but for this initial phase we use a ConcurrentDictionary.
Cache.TryAdd(new CacheKey(criteria, tableName), plan);
```

The cache has no eviction policy and no maximum size.

---

## Problem

An unbounded cache that grows without eviction is a memory leak for applications that:
- Dynamically compose specifications at runtime (e.g., user-defined filters)
- Process different entities or table names programmatically
- Run as long-lived services (microservices, workers, APIs)

In such scenarios, the cache grows monotonically until the process is restarted or OOM-killed.

---

## Options Considered

### Option A — Implement bounded LRU with configurable capacity

```csharp
public static class QueryPlanCache
{
    // Configurable before first use
    public static int MaxEntries { get; set; } = 512;
    
    private static readonly LruCache<CacheKey, QueryModel> Cache 
        = new(MaxEntries);
}
```

> **Implementation Note**: The actual implementation uses the property name `Capacity` (not `MaxEntries` as initially proposed here). The API is: `public static int Capacity { get; set; }` with the same default of 512. The semantic is identical; only the name differs.


### Option B — Add capacity limit without LRU (drop-newest)

Once the maximum size is reached, new entries are simply not cached. Existing entries are never evicted.

### Option C — Make cache configurable per `QuerySpecTranslator<T>` instance

```csharp
var translator = new QuerySpecTranslator<Customer>("customers", maxCacheEntries: 256);
```

This removes the global static cache entirely.

### Option D — Remove cache, document as consumer responsibility

Remove the cache entirely. Document that consumers who need SQL caching should implement it at their own repository layer.

---

## Decision

**Accepted: Option A — Implement bounded LRU with configurable static capacity.**

Default: **512 entries**. This covers the vast majority of applications (which have far fewer distinct specification patterns than 512) while protecting against the memory leak scenario.

The implementation uses a doubly-linked list + dictionary structure (`LinkedList<T>` + `Dictionary<TKey, LinkedListNode<TValue>>`), wrapped in a `lock` or `ReaderWriterLockSlim` for thread safety.

---

## Decision Drivers

- **Memory safety**: Unbounded caches are production bugs, not "future work."
- **Correctness before optimization**: A safe cache with 512 entries is better than a "fast" cache that leaks memory.
- **API stability**: The cache is internal implementation detail — changing it is not a breaking change.
- **Configurability**: 512 is a sensible default but applications with very dynamic specs may need to adjust.

---

## Consequences

### Positive

- No memory leak for long-running services with dynamic specifications.
- Default capacity covers the common case with zero configuration.
- Cache is still beneficial for the most common pattern (repeated specs of the same type).

### Negative

- LRU eviction has overhead vs. `ConcurrentDictionary.TryAdd` (lock acquisition per operation).
- Applications with 512+ distinct specifications will have cache evictions — these will fall back to translation, which is the correct behavior.

---

## Implementation Notes

```csharp
internal sealed class LruCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _map;
    private readonly LinkedList<(TKey Key, TValue Value)> _list;
    private readonly object _lock = new();

    public LruCache(int capacity)
    {
        _capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(capacity);
        _list = new LinkedList<(TKey, TValue)>();
    }

    public bool TryGet(TKey key, out TValue value)
    {
        lock (_lock)
        {
            if (!_map.TryGetValue(key, out var node))
            { value = default!; return false; }
            
            _list.Remove(node);
            _list.AddFirst(node);
            value = node.Value.Value;
            return true;
        }
    }

    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_map.TryGetValue(key, out var existing))
            {
                _list.Remove(existing);
                _map.Remove(key);
            }
            else if (_map.Count >= _capacity)
            {
                var lru = _list.Last!;
                _list.RemoveLast();
                _map.Remove(lru.Value.Key);
            }
            
            var node = _list.AddFirst((key, value));
            _map[key] = node;
        }
    }
}
```

---

## Multi-Criteria Cache Fix

As a related fix, `QuerySpecTranslator` currently only caches when `Criteria.Length == 1`. For multi-criteria specs, the criteria should be composed via `ExpressionComposer.AndAll()` before caching:

```csharp
var cacheKey = spec.Criteria.Length == 1
    ? spec.Criteria[0]
    : ExpressionComposer.AndAll<T>(spec.Criteria.AsSpan());
```

This makes the cache effective for the most common real-world case (multiple `Where()` clauses).

---

## Why Alternatives Were Rejected

**Option B**: Drop-newest is easier to implement but worse for cache effectiveness. If the capacity is reached by temporary entries, permanent high-frequency entries will never be cached.

**Option C**: Instance-level caches mean that two `QuerySpecTranslator<Customer>` instances for the same table do not share cache entries. This eliminates cross-instance cache benefits for common repository patterns.

**Option D**: Removing the cache entirely makes the SQL adapter non-competitive for hot Dapper paths. The cache is a real performance benefit that should exist and be correct.

---

## Reconsideration Criteria

If profiling shows that LRU lock contention is a bottleneck at high concurrency, a sharded or lock-free LRU strategy can be evaluated. This requires benchmark evidence before implementation.
