# Frequently Asked Questions (FAQ)

---

### 1. Why another Specification library? How does it differ from Ardalis.Specification?

| Dimension | Ardalis.Specification | EricksonLopez.Specification |
|---|---|---|
| **Architectural Separation** | Mixes domain rules, EF Core query options, `.Include()`, and tracking in one mutable class | **Strict separation**: Pure Domain `Specification<T>` vs Query Intent `QuerySpec<T>` |
| **Native AOT & Trimming** | Relies on dynamic compilation and reflection-heavy evaluators | **100% Native AOT Compatible** out-of-the-box via `ExpressionInterpreter` |
| **Direct SQL Translation** | Requires EF Core to execute queries | **Built-in SQL AST Translator** supporting 6 database engines without any ORM |
| **Expression Composition** | Uses `Expression.Invoke`, which breaks multiple LINQ providers and query compilers | **`ParameterReplacer` AST rewriting**: Generates clean invoke-free expressions |
| **Immutability & Concurrency** | Mutable builder pattern; instances are not thread-safe | **Purely immutable**: Instances are 100% thread-safe and can be registered as `Singleton` |
| **Database Support** | EF Core only | EF Core, Dapper, PostgreSQL, SQL Server, SQLite, MySQL, MariaDB, Oracle, MongoDB |

---

### 2. Is this library compatible with Native AOT in .NET 8 and .NET 10?

**Yes.** All assemblies are built with `<IsAotCompatible>true</IsAotCompatible>` and verified with `EnableTrimAnalyzer`.
- In-memory evaluation (`IsSatisfiedBy`) uses an AST node-walking interpreter (`ExpressionInterpreter`) instead of calling `expression.Compile()`.
- SQL generation is entirely static string and parameter formatting.
- Any JIT-specific dynamic compilation features (such as `ToCompiledPredicate()`) are explicitly decorated with `[RequiresDynamicCode]` to alert developers at build time.

---

### 3. Why are there no `.Include()` or `.ThenInclude()` methods on `Specification<T>`?

Per **ADR-002** (Architectural Decision Record), entity graph loading (`Eager Loading` / `.Include`) is an **infrastructure persistence concern**, not a domain business rule.
- A business rule ("Is this customer active?") should never dictate relational join trees or foreign key fetching.
- Eager loading concerns belong in repository implementations, projection queries (`QuerySpec<T, TResult>`), or specialized application query services.

---

### 4. Can I use this library without Entity Framework Core?

**Absolutely.** The core library (`EricksonLopez.Specification`) and abstractions (`EricksonLopez.Specification.Abstractions`) have zero third-party dependencies.
- You can evaluate specifications entirely in-memory over `IEnumerable<T>`.
- You can translate specifications directly to raw SQL with `EricksonLopez.Specification.Sql` and execute via `Dapper`.
- You can compile specifications to MongoDB filter definitions with `EricksonLopez.Specification.MongoDB`.

---

### 5. What C# operators can I use to combine specifications?

The library provides first-class operator overloads for natural C# expressions:
- `&` (logical AND) and `BitwiseAnd`
- `|` (logical OR) and `BitwiseOr`
- `!` (logical NOT) and `LogicalNot`
- `&&` and `||` short-circuiting composition via `operator true` and `operator false`

```csharp
var spec = activeSpec && (vipSpec || highCreditSpec);
```

---

### 6. How does keyset / cursor pagination work?

Offset-based pagination (`OFFSET 100000 ROWS FETCH NEXT 20 ROWS`) forces database engines to scan and discard 100,000 index rows, causing performance degradation.

Keyset pagination (`SeekAfter` / `SeekBefore`) filters on index bounds:

```csharp
// WHERE created_at < @cursor ORDER BY created_at DESC LIMIT 20
var page = QuerySpec<Order>.Empty
    .OrderByDescending(o => o.CreatedAt)
    .SeekAfter(o => o.CreatedAt, lastSeenTimestamp, take: 20);
```

This guarantees $O(1)$ query execution regardless of how deep the user paginates.

---

### 7. Are specifications and QuerySpec instances thread-safe?

**Yes.** All specification classes and `QuerySpec<T>` records are **strictly immutable**. Any method that modifies criteria, ordering, or pagination returns a new instance. They can be safely cached, shared across asynchronous tasks, and registered as DI Singletons.

---

### 8. What is the difference between `Spec.Between` and `Spec.InRange`?

Both methods create range predicates. `Spec.Between` supports inclusive boundaries for both non-nullable and nullable struct properties (`c => c.DiscountRate, 0.05m, 0.20m`), while `Spec.InRange` is an alias for range containment. Both validate that `lower <= upper` and throw `ArgumentException` if violated.
