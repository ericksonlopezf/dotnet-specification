// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;

namespace EricksonLopez.Specification;

/// <summary>
/// Provides a thread-safe bounded LRU (Least Recently Used) cache for compiled expression delegates,
/// keyed by expression structural AST equality.
/// </summary>
/// <remarks>
/// <para>
/// Expression compilation via <c>Expression.Compile()</c> is an expensive operation
/// that invokes the JIT compiler. This cache ensures that each structurally unique expression
/// is compiled at most once per process, up to a configurable <see cref="Capacity"/> limit.
/// When the capacity is reached, the least recently used delegates are evicted in O(1) time.
/// </para>
/// <para>
/// <strong>AOT warning:</strong> This cache is only useful in JIT runtimes. All methods that
/// return compiled delegates are annotated with <see cref="RequiresDynamicCodeAttribute"/>
/// and will not work correctly under Native AOT.
/// </para>
/// <para>
/// <strong>Cache key:</strong> The key is the <see cref="Expression"/> tree itself, compared via
/// deep structural AST equality using <see cref="ExpressionEqualityComparer.Default"/>.
/// This guarantees complete correctness and prevents delegate corruption from hash collisions.
/// </para>
/// </remarks>
public static class ExpressionCompilationCache
{
    private const int DefaultCapacity = 512;
    private static int _capacity = DefaultCapacity;

    private static readonly object _sync = new();
    private static readonly Dictionary<Expression, LinkedListNode<CacheEntry>> _map = new(ExpressionEqualityComparer.Default);
    private static readonly LinkedList<CacheEntry> _lruList = new();

    /// <summary>
    /// Gets or sets the maximum number of compiled delegates retained in the cache.
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
    /// Retrieves a compiled delegate for the given predicate expression, compiling it only if not already cached.
    /// </summary>
    /// <typeparam name="T">The predicate input type.</typeparam>
    /// <param name="expression">The expression to compile.</param>
    /// <returns>A compiled delegate equivalent to the expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/></exception>
    [RequiresDynamicCode("Compiles expression trees to IL delegates at runtime. Not compatible with Native AOT.")]
    [RequiresUnreferencedCode("Expression compilation may require types that are trimmed.")]
    public static Func<T, bool> GetOrCompile<T>(Expression<Func<T, bool>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        lock (_sync)
        {
            if (_map.TryGetValue(expression, out var node))
            {
                if (node != _lruList.First)
                {
                    _lruList.Remove(node);
                    _lruList.AddFirst(node);
                }

                Diagnostics.SpecificationDiagnostics.ExpressionCacheHits.Add(1);
                return (Func<T, bool>)node.Value.Compiled;
            }
        }

        Diagnostics.SpecificationDiagnostics.ExpressionCacheMisses.Add(1);
        var compiled = expression.Compile();

        lock (_sync)
        {
            if (_map.TryGetValue(expression, out var existingNode))
            {
                return (Func<T, bool>)existingNode.Value.Compiled;
            }

            if (_map.Count >= _capacity)
            {
                var oldest = _lruList.Last!;
                _lruList.RemoveLast();
                _map.Remove(oldest.Value.Key);
            }

            var entry = new CacheEntry(expression, compiled);
            var newNode = new LinkedListNode<CacheEntry>(entry);
            _lruList.AddFirst(newNode);
            _map[expression] = newNode;
            return compiled;
        }
    }

    /// <summary>
    /// Gets the number of compiled delegates currently in the cache.
    /// </summary>
    /// <returns>The total number of distinct compiled delegates held in the cache.</returns>
    public static int CachedCount
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
    /// Clears the compilation cache. Use only in testing scenarios.
    /// </summary>
    internal static void Clear()
    {
        lock (_sync)
        {
            _map.Clear();
            _lruList.Clear();
        }
    }

    private sealed record CacheEntry(Expression Key, Delegate Compiled);
}


