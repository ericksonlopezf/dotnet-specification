# Best Practices — EricksonLopez.Specification

This document summarizes the key patterns and anti-patterns for using `EricksonLopez.Specification` correctly. Rules are derived from the library's design constraints, ADR decisions, and Roslyn analyzer diagnostics.

---

## 1. Domain Specifications

### ✅ Always seal concrete specifications (SPEC001)

Every concrete `Specification<T>` class must be `sealed`. Inheritance hierarchies for specifications create hidden coupling and break the Open/Closed Principle at the wrong level.

```csharp
// ✅ Correct
public sealed class ActiveCustomerSpec : Specification<Customer>
{
    protected override Expression<Func<Customer, bool>> BuildExpression()
        => c => c.IsActive && c.DeletedAt == null;
}

// ❌ Wrong — SPEC001 analyzer error
public class ActiveCustomerSpec : Specification<Customer> { ... }
```

### ✅ Specifications must be immutable (SPEC002)

Do not add mutable fields or properties to a `Specification<T>` class.

```csharp
// ✅ Correct — constructor parameter captured in closure
public sealed class PremiumTierSpec : Specification<Customer>
{
    private readonly decimal _minimum;

    public PremiumTierSpec(decimal minimum) => _minimum = minimum;

    protected override Expression<Func<Customer, bool>> BuildExpression()
        => c => c.TotalPurchases >= _minimum;
}

// ❌ Wrong — SPEC002 analyzer warning
public sealed class PremiumTierSpec : Specification<Customer>
{
    public decimal Minimum { get; set; }  // mutable — captures wrong value in expression
    ...
}
```

### ✅ Encapsulate logic inside `BuildExpression()`, not constructors

The body of `BuildExpression()` is the specification. Constructors should only capture parameters.

```csharp
// ✅ Correct
protected override Expression<Func<Customer, bool>> BuildExpression()
    => c => c.IsActive && c.TotalPurchases >= _minimum;

// ❌ Wrong — logic in constructor, expression is a pass-through
public ActivePremiumCustomer(Expression<Func<Customer, bool>> expr) : base(expr) { }
```

### ✅ Prefer concrete specification classes over `Spec.For<T>` for business rules

`Spec.For<T>` is useful for quick tests or one-off inline predicates. Core business logic should live in named classes for discoverability and reuse.

```csharp
// ✅ For tests or quick inline usage
var active = Spec.For<Customer>(c => c.IsActive);

// ✅ For reusable business rules
public sealed class ActiveCustomerSpec : Specification<Customer> { ... }
```

---

## 2. Specification Composition

### ✅ Compose with `.And()`, `.Or()`, `.Not()`

The composition methods rewrite expression trees without using `Expression.Invoke`, making the result compatible with EF Core, SQL translators, and AOT.

```csharp
var spec = new ActiveCustomerSpec()
    .And(new PremiumTierSpec(minimumPurchases: 1_000m))
    .And(Spec.For<Customer>(c => c.DeletedAt == null));
```

### ✅ Use `ExpressionComposer.AndAll(span)` for bulk composition

Avoids creating intermediate `CompositeSpecification<T>` objects for each pair.

```csharp
ReadOnlySpan<Specification<Customer>> rules =
[
    new ActiveCustomerSpec(),
    new PremiumTierSpec(1_000m),
    new RegionSpec("US"),
];
var combined = ExpressionComposer.AndAll(rules);
```

### ❌ Never use `Expression.Invoke` in specifications (SPEC003)

`Expression.Invoke` is not supported by EF Core's query translator and breaks SQL generation. The SPEC003 analyzer reports this as an error.

---

## 3. Query Building

### ✅ Use `QuerySpec<T>` in the Application layer, not the Domain layer

`QuerySpec<T>` carries query concerns (pagination, ordering, projection). It belongs in the Application layer handlers, not in Domain entities or specifications.

```csharp
// ✅ Application layer handler
var query = QuerySpec<Customer>.Empty
    .Where(new ActiveCustomerSpec())
    .OrderByDescending(c => c.CreatedAt)
    .Page(pageIndex: 1, pageSize: 20);
```

### ✅ Always set a pagination limit on unbounded queries (SPEC004)

Omitting `.Take()` or `.Page()` on a query that will execute against a database is an unbounded query. The SPEC004 analyzer reports this as an informational diagnostic.

```csharp
// ✅ Bounded
var query = QuerySpec<Customer>.Empty.Where(spec).Page(1, 50);

// ⚠️ SPEC004 warning — no limit, could return entire table
var query = QuerySpec<Customer>.Empty.Where(spec);
```

### ✅ Always order before paginating (SPEC005)

Applying pagination without a deterministic ordering produces non-repeatable results. SPEC005 reports ordering without pagination as an informational diagnostic (as a reminder to add pagination if missing).

---

## 4. Infrastructure and Repositories

### ✅ Define `IReadRepository<T>` implementations in the Infrastructure layer only

The `IReadRepository<T>` contract belongs to Abstractions. Its implementation (EF Core, Dapper) belongs in Infrastructure. Never reference `DbContext` or `IDbConnection` in Domain.

### ❌ Never inject `IServiceProvider` or `DbContext` into a specification (SPEC008)

Specifications must not have infrastructure dependencies. SPEC008 reports this as an error.

```csharp
// ❌ Wrong — SPEC008 error
public sealed class OrderByExternalServiceSpec : Specification<Customer>
{
    private readonly IExternalService _svc;
    public OrderByExternalServiceSpec(IExternalService svc) => _svc = svc;
    // This creates an infrastructure dependency in a domain object
}
```

### ❌ Never use `async`/`await` inside `BuildExpression()` (SPEC010)

Expression trees cannot contain `async`/`await`. SPEC010 reports this as an error at compile time.

---

## 5. AOT and Performance

### ✅ Use `IsSatisfiedBy()` for AOT-safe in-memory evaluation

`IsSatisfiedBy()` uses `ExpressionInterpreter` by default — no `Expression.Compile()`, fully AOT-safe.

```csharp
bool qualifies = spec.IsSatisfiedBy(customer);  // AOT-safe
```

### ✅ Cache specifications as singletons when possible

`Specification<T>` construction is cheap, but singleton reuse avoids even the one `Lazy<T>` allocation per instance.

```csharp
// ✅ Singleton for stateless specs
private static readonly ActiveCustomerSpec _activeSpec = new();

// QuerySpec<T> can be rebuilt per request — it's a lightweight record
```

### ✅ For high-frequency in-memory filtering in JIT contexts, use `ToCompiledPredicate()`

This is JIT-only and cannot be used in AOT.

```csharp
[RequiresDynamicCode("JIT-only")]
var compiled = spec.ToCompiledPredicate();  // compiled once, cached by structural hash
entities.Where(compiled);                   // native delegate — near-zero overhead
```

### ❌ Never use `ToCompiledPredicate()` in NativeAOT projects

The `[RequiresDynamicCode]` annotation will cause a linker error at publish time.

---

## 6. SQL Translation (Dapper path)

### ✅ Choose the right dialect for your database

| Database | Dialect Class | Package |
|---|---|---|
| PostgreSQL | `PostgreSqlDialect.Default` | `EricksonLopez.Specification.PostgreSql` |
| Microsoft SQL Server | `MsSqlDialect.Default` | `EricksonLopez.Specification.MsSql` |
| MySQL | `MySqlDialect.Default` | `EricksonLopez.Specification.MySql` |
| MariaDB | `MariaDbDialect.Default` | `EricksonLopez.Specification.MariaDb` |
| SQLite | `SqliteDialect.Default` | `EricksonLopez.Specification.Sqlite` |
| Oracle Database | `OracleDialect.Default` | `EricksonLopez.Specification.Oracle` |

### ✅ Use `SnakeCaseColumnNameResolver` for PostgreSQL conventions

```csharp
// Translates C# PascalCase properties to snake_case columns
var resolver = new SnakeCaseColumnNameResolver();
var translator = new QuerySpecTranslator<Customer>(resolver);
```

### ✅ Use Dapper.AOT for NativeAOT-compatible database execution

The `samples/NativeAotDapper/` project demonstrates the complete NativeAOT pattern with `Dapper.AOT` source-generated interceptors.

---

## 7. Analyzer Rules Reference

| ID | Severity | Rule | CodeFix |
|---|---|---|---|
| SPEC001 | Warning | Specification not sealed or abstract | `SpecificationSealedCodeFixProvider` |
| SPEC002 | Warning | Mutable state in specification | N/A |
| SPEC003 | Error | `Expression.Invoke` detected | N/A |
| SPEC004 | Info | No `Take()` or `Page()` limit (unbounded query) | N/A |
| SPEC005 | Info | Ordering applied without pagination | N/A |
| SPEC006 | Info | Domain specification used outside domain layer | N/A |
| SPEC007 | Warning | Non-translatable method call in expression | N/A |
| SPEC008 | Error | Specification captures `IServiceProvider` or `DbContext` | N/A |
| SPEC009 | Error | Async lambda inside `BuildExpression` | N/A |
| SPEC010 | Error | Calling `IsSatisfiedBy` inside `BuildExpression` | N/A |
| SPEC011 | Warning | Inheritance from legacy `Ardalis.Specification` | `ArdalisMigrationCodeFixProvider` |

