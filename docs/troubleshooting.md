# Troubleshooting Guide: EricksonLopez.Specification

Common issues, compiler errors, analyzer warnings, and runtime diagnostics when working with **EricksonLopez.Specification**.

---

## 1. Runtime Exceptions

### `NotSupportedException` in `QuerySpecTranslator`

#### Symptom
```text
System.NotSupportedException: Method 'Boolean IsNullOrEmpty(System.String)' is not supported in SQL translation.
```

#### Root Cause
`QuerySpecTranslator<T>` translates expression trees into relational SQL AST nodes (`QueryModel`). Non-mapped .NET methods like `string.IsNullOrEmpty` or arbitrary user methods cannot be automatically converted to SQL.

#### Solution
Rewrite using translatable primitive comparisons:

```csharp
// ❌ FAILS:
var spec = QuerySpec<Customer>.Empty.Where(c => string.IsNullOrEmpty(c.Email));

// ✅ WORKS:
var spec = QuerySpec<Customer>.Empty.Where(c => c.Email == null || c.Email == string.Empty);
```

---

### `ArgumentException: Lower bound cannot be greater than upper bound`

#### Symptom
```text
System.ArgumentException: Lower bound '50' cannot be greater than upper bound '10'. (Parameter 'lower')
```

#### Root Cause
In `Spec.Between<T, TProperty>`, the lower boundary value is greater than the upper boundary value.

#### Solution
Ensure `lower <= upper`:

```csharp
// ❌ FAILS:
var spec = Spec.Between<Customer, int>(c => c.TotalPurchases, 50, 10);

// ✅ WORKS:
var spec = Spec.Between<Customer, int>(c => c.TotalPurchases, 10, 50);
```

---

### `ArgumentOutOfRangeException` on `Page` or `Skip`

#### Symptom
```text
System.ArgumentOutOfRangeException: Page number must be greater than or equal to 1. (Parameter 'page')
```

#### Root Cause
`QuerySpec<T>.Page(page, pageSize)` is **1-based**:
- `page < 1` throws `ArgumentOutOfRangeException`
- `pageSize < 1` throws `ArgumentOutOfRangeException`
- `Skip(count)` with `count < 0` throws `ArgumentOutOfRangeException`
- `Take(count)` with `count < 0` throws `ArgumentOutOfRangeException`

#### Solution
Sanitize UI / HTTP input parameters before calling `.Page()`:

```csharp
int safePage = Math.Max(1, request.Page);
int safePageSize = Math.Clamp(request.PageSize, 1, 100);

var query = QuerySpec<Customer>.Empty.Page(safePage, safePageSize);
```

---

## 2. Roslyn Analyzer Warnings & Errors

| Analyzer ID | Severity | Problem | Fix |
|---|:---:|---|---|
| **SPEC001** | Warning | Specification class is not `sealed` | Add `sealed` keyword: `public sealed class ActiveCustomerSpecification : Specification<Customer>` |
| **SPEC002** | Warning | Specification contains mutable fields or public setters | Ensure all specifications are immutable; pass parameters via constructor |
| **SPEC003** | Error | `Expression.Invoke` detected inside `BuildExpression` | Use `ExpressionComposer.And()` or combinators (`spec1.And(spec2)`) instead of invoking lambdas |
| **SPEC004** | Info | QuerySpec lacks pagination limits | Add `.Take(n)` or `.Page(p, s)` to prevent unbounded table scans |
| **SPEC007** | Warning | Non-translatable method in `BuildExpression` | Replace method calls with translatable member comparisons |
| **SPEC009** | Error | Async lambda inside `BuildExpression` | Domain specifications must be synchronous and pure |
| **SPEC011** | Warning | Inheritance from legacy `Ardalis.Specification` | Inherit from `EricksonLopez.Specification.Specification<T>` |

---

## 3. Query Performance & Diagnostic Warnings

### Unbounded Table Scan Warnings

#### Symptom
Diagnostic log entry:
```text
[Validation] WARNING: QuerySpec without filter criteria will return ALL records.
[Validation] WARNING: QuerySpec without pagination (Take) may produce an unbounded query.
```

#### Prevention
Use `QuerySpecExtensions` inspection helpers in query pipelines:

```csharp
if (!querySpec.HasCriteria())
{
    _logger.LogWarning("Execution attempted on unbounded query: {Tag}", querySpec.Tag);
}

if (!querySpec.HasPagination())
{
    // Apply safety default
    querySpec = querySpec.Take(100);
}
```

---

## 4. Cooperative Task Cancellation

### CancellationTokens in Repositories

When using `ReadRepositoryResultExtensions` (`ListResultAsync`, `FirstOrDefaultResultAsync`):
- Any caught `OperationCanceledException` is **never swallowed into a Result failure**.
- It is immediately rethrown so that ASP.NET Core request timeouts, background worker cancellations, and client disconnections abort gracefully.

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

try
{
    var result = await repo.ListResultAsync(querySpec, cts.Token);
}
catch (OperationCanceledException)
{
    // Normal cancellation handled by framework pipeline
}
```
