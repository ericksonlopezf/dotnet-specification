# AOT — EricksonLopez.Specification NativeAOT Guide

> **Version**: 1.0 — 2026-08-14
> **Architecture Principle**: Truth in Engineering — No unsupported claims.
> **Evidence**: Explicit BCL annotations (`[RequiresDynamicCode]`, `[RequiresUnreferencedCode]`, `[DynamicallyAccessedMembers]`), full test suites, and NativeAOT verification.

---

## 1. Why AOT Matters for Specification Libraries

Native AOT compilation (in .NET 8, 9, 10) produces fully self-contained, pre-compiled native binaries without a JIT compiler. This brings instantaneous startup times and minimal memory footprint (ideal for AWS Lambda, Azure Functions, microservices, containerized workloads, and edge computing).

However, Native AOT imposes two strict architectural constraints:
1. **No runtime code generation**: `Expression.Compile()`, `System.Reflection.Emit`, and dynamic IL emit are prohibited at runtime.
2. **Aggressive Trimming**: Unreferenced types, methods, fields, and properties are stripped by `IL Linker` at build time unless explicitly preserved.

Traditional specification libraries (such as LinqKit, Ardalis.Specification) either rely on `Expression.Compile()` or runtime tree rewriting (`AsExpandable()`), making them fail or produce unannotated crashes under Native AOT.

**EricksonLopez.Specification** was engineered to provide a dual-engine architecture:
- **JIT Runtimes**: High-performance compiled delegates cached structurally via `ExpressionCompilationCache`.
- **Native AOT Runtimes**: Zero dynamic code execution via `ExpressionInterpreter` (interpreted in-memory evaluation) and `QuerySpecTranslator` (static AST SQL generation).

---

## 2. ExpressionInterpreter Node Support Matrix

Under Native AOT, in-memory specification validation (`spec.IsSatisfiedBy(candidate)`) is evaluated without invoking `Expression.Compile()`.

The table below outlines the exact node type support in `ExpressionInterpreter`:

| Expression Node Type | C# Syntax Example | Supported in AOT? | Implementation Details |
|---|---|:---:|---|
| `ConstantExpression` | `42`, `"US"`, `true` | ✅ **YES** | Direct constant evaluation |
| `ParameterExpression` | `c => c...` | ✅ **YES** | Reference matching to candidate instance |
| `MemberExpression` (Instance) | `c.IsActive`, `c.CreditLimit` | ✅ **YES** | Reflected PropertyInfo/FieldInfo with `DynamicallyAccessedMembers` |
| `MemberExpression` (Static) | `DateTime.UtcNow` | ✅ **YES** | Static member reflection |
| `BinaryExpression` (Logical) | `a && b`, `a \|\| b` | ✅ **YES** | Boolean short-circuit evaluation |
| `BinaryExpression` (Null Coalesce) | `c.CountryCode ?? "US"` | ✅ **YES** | `ExpressionType.Coalesce` evaluation |
| `BinaryExpression` (Comparison) | `==`, `!=`, `<`, `<=`, `>`, `>=` | ✅ **YES** | `IComparable` type comparison & `Equals` |
| `UnaryExpression` (Logical Not) | `!c.IsActive` | ✅ **YES** | Boolean negation |
| `UnaryExpression` (Convert) | `(decimal)c.Id` | ✅ **YES** | `Convert.ChangeType` with InvariantCulture |
| `ConditionalExpression` | `c.IsActive ? c.Points > 10 : c.Id == 1` | ✅ **YES** | Ternary conditional branch evaluation |
| `TypeBinaryExpression` | `c is Customer` | ✅ **YES** | `ExpressionType.TypeIs` type assignability test |
| `MethodCallExpression` | `c.Name.StartsWith("A")` | ✅ **YES** | Method invocation via reflection |
| `BlockExpression` | `{ ... }` | ❌ **NO** | Not supported in domain predicates (throws `NotSupportedException`) |
| `LoopExpression` | `while (...)` | ❌ **NO** | Out of scope for domain specifications |
| `NewExpression` | `new { c.Id }` | ❌ **NO** | Projections belong in `QuerySpec<T, TResult>`, not filter predicates |

> [!NOTE]
> For any non-supported edge cases in standard JIT environments, developers can explicitly invoke `spec.ToCompiledPredicate()` to leverage runtime JIT compilation.

---

## 3. Component AOT Classification

### 3.1 Fully AOT Compatible (Zero Dynamic Code, Zero Warnings)

| Component | Assembly | AOT Classification | Notes |
|---|---|---|---|
| `ISpecification<T>` | `Abstractions` | Pure Interface | 100% AOT safe |
| `Specification<T>` | `Core` | Abstract Base Class | Uses `ExpressionInterpreter` by default |
| `ExpressionComposer` | `Core` | Expression Tree Builder | Parameter rebinding via `ExpressionVisitor` |
| `ParameterReplacer` | `Core` | Expression Tree Rewriter | Pure visitor without dynamic code |
| `ExpressionSimplifier` | `Core` | Expression Tree Rewriter | Constant folding visitor |
| `ExpressionHasher` | `Core` | Expression Tree Hasher | Structural hashing with `HashCode.Combine` |
| `ExpressionEqualityComparer` | `Core` | Structural Comparer | Deep node-by-node AST traversal |
| `QuerySpec<T>` | `Abstractions` | Immutable Query Descriptor | Pure immutable record |
| `QuerySpec<T, TResult>` | `Abstractions` | Projected Query Descriptor | Pure immutable record |
| `PostgreSqlDialect` | `PostgreSql` | SQL Renderer | Pure string formatting & AST rendering |
| `MsSqlDialect` | `MsSql` | SQL Renderer | Pure string formatting & AST rendering |
| `MySqlDialect` | `MySql` | SQL Renderer | Pure string formatting & AST rendering |
| `MariaDbDialect` | `MariaDb` | SQL Renderer | Pure string formatting & AST rendering |
| `SqliteDialect` | `Sqlite` | SQL Renderer | Pure string formatting & AST rendering |
| `OracleDialect` | `Oracle` | SQL Renderer | Pure string formatting & AST rendering |

### 3.2 AOT Compatible with Linker Annotations

| Component | Linker Annotation | Rationale & Guidance |
|---|---|---|
| `ExpressionInterpreter` | `[UnconditionalSuppressMessage("Trimming", "IL2072")]` | Reads properties from candidate `T`. Ensure entity types retain properties via `[DynamicallyAccessedMembers]` or standard serialization attributes. |
| `QuerySpecTranslator<T>` | `[RequiresUnreferencedCode(...)]` | Extracts constant values captured in closures (e.g. `var min = 5; Where(c => c.Id > min)`). Closures are extracted via field reflection. |
| `QueryPlanCache` | None (Internal LRU) | Thread-safe bounded cache (default 512 entries) using `ExpressionEqualityComparer`. |

### 3.3 JIT-Only (Strictly Marked with `[RequiresDynamicCode]`)

| Method / Type | Linker Annotation | AOT Behavior |
|---|---|---|
| `ExpressionCompilationCache.GetOrCompile<T>()` | `[RequiresDynamicCode(...)]` `[RequiresUnreferencedCode(...)]` | Linker emits a compile-time warning/error if invoked in a Native AOT publish. |
| `Specification<T>.ToCompiledPredicate()` | `[RequiresDynamicCode(...)]` `[RequiresUnreferencedCode(...)]` | Explicitly opt-in to JIT IL compilation. |

---

## 4. End-to-End NativeAOT Example

```csharp
using System;
using System.Linq.Expressions;
using EricksonLopez.Specification;
using EricksonLopez.Specification.Sql;
using EricksonLopez.Specification.PostgreSql;

// 1. Domain Specification (Pure Domain Layer)
public sealed class HighValueCustomerSpec : Specification<Customer>
{
    private readonly decimal _threshold;

    public HighValueCustomerSpec(decimal threshold) => _threshold = threshold;

    protected override Expression<Func<Customer, bool>> BuildExpression()
        => c => c.IsActive && c.TotalPurchases >= _threshold;
}

// 2. In-Memory Validation (AOT Safe via ExpressionInterpreter)
var spec = new HighValueCustomerSpec(1000m);
var customer = new Customer { IsActive = true, TotalPurchases = 1500m };

// Runs entirely via interpreted AST walk -- ZERO IL emitted at runtime
bool isEligible = spec.IsSatisfiedBy(customer); 

// 3. Application Query Building (AOT Safe)
var query = QuerySpec<Customer>.Empty
    .Where(spec)
    .OrderByDescending(c => c.TotalPurchases)
    .Page(page: 1, pageSize: 25);

// 4. SQL AST Translation & Rendering
var translator = new QuerySpecTranslator<Customer>("customers");
var dialect = PostgreSqlDialect.Default;

var plan = translator.Translate(query);
var sqlResult = dialect.Render(plan);

Console.WriteLine(sqlResult.Sql);
// Output: SELECT * FROM customers WHERE (is_active = $1 AND total_purchases >= $2) ORDER BY total_purchases DESC LIMIT 25 OFFSET 0
```

---

## 5. Summary & Competitive Comparison

| Feature | EricksonLopez.Specification | Ardalis.Specification | LinqKit | EF Core Precompiled AOT |
|---|:---:|:---:|:---:|:---:|
| **In-Memory Validation in AOT** | ✅ Yes (Interpreted) | ❌ Broken (`Compile()`) | ❌ Broken (`AsExpandable`) | ❌ N/A |
| **Dynamic SQL Generation in AOT** | ✅ Yes (AST Translator) | ❌ Requires EF Core | ❌ Broken | ❌ Only static queries |
| **Accurate BCL Linker Warnings** | ✅ Yes (`[RequiresDynamicCode]`) | ❌ No annotations | ❌ No annotations | ✅ Yes |
| **Zero Runtime Code Gen by Default** | ✅ Yes | ❌ No | ❌ No | ✅ Yes |
