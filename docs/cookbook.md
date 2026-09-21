# EricksonLopez.Specification — Enterprise Cookbook

Practical, copy-paste-ready recipes for every production scenario across Domain-Driven Design (DDD), Entity Framework Core, Dapper SQL translation, NativeAOT, Source Generation, and OpenTelemetry observability.

---

## Table of Contents

1. [Recipe 1: Define a Reusable Domain Specification](#recipe-1-define-a-reusable-domain-specification)
2. [Recipe 2: Compose Specifications (AND / OR / NOT)](#recipe-2-compose-specifications-and--or--not)
3. [Recipe 3: Bulk Composition (Spec.All / Spec.Any)](#recipe-3-bulk-composition-specall--specany)
4. [Recipe 4: Inline Specifications with Spec.For<T>](#recipe-4-inline-specifications-with-specfort)
5. [Recipe 5: Multi-Column Search (Spec.Search)](#recipe-5-multi-column-search-specsearch)
6. [Recipe 6: Inclusive Range Filtering (Spec.Between)](#recipe-6-inclusive-range-filtering-specbetween)
7. [Recipe 7: Full-Text Search Filtering (Spec.FullText)](#recipe-7-full-text-search-filtering-specfulltext)
8. [Recipe 8: Immutable Query Specification (QuerySpec<T>)](#recipe-8-immutable-query-specification-queryspect)
9. [Recipe 9: Keyset / Cursor Pagination](#recipe-9-keyset--cursor-pagination)
10. [Recipe 10: Multi-Column Search with QuerySpec](#recipe-10-multi-column-search-with-queryspec)
11. [Recipe 11: Diagnostic Query Tagging (TagWith)](#recipe-11-diagnostic-query-tagging-tagwith)
12. [Recipe 12: Projected Queries (QuerySpec<T, TResult>)](#recipe-12-projected-queries-queryspect-tresult)
13. [Recipe 13: Execute QuerySpec on EF Core (IQueryable.Apply)](#recipe-13-execute-queryspec-on-ef-core-iqueryableapply)
14. [Recipe 14: EF Core Query Hints (AsSplitQuery & IgnoreAutoIncludes)](#recipe-14-ef-core-query-hints-assplitquery--ignoreautoincludes)
15. [Recipe 15: Generic EF Core Read Repository & GetByIdAsync](#recipe-15-generic-ef-core-read-repository--getbyidasync)
16. [Recipe 16: Custom ISpecificationEvaluator for EF Core](#recipe-16-custom-ispecificationevaluator-for-ef-core)
17. [Recipe 17: Dependency Injection Registration](#recipe-17-dependency-injection-registration)
18. [Recipe 18: Dapper SQL Translation (PostgreSQL)](#recipe-18-dapper-sql-translation-postgresql)
19. [Recipe 19: Dapper SQL Translation (Microsoft SQL Server)](#recipe-19-dapper-sql-translation-microsoft-sql-server)
20. [Recipe 20: Dapper SQL Translation (MySQL / MariaDB)](#recipe-20-dapper-sql-translation-mysql--mariadb)
21. [Recipe 21: Dapper SQL Translation (SQLite)](#recipe-21-dapper-sql-translation-sqlite)
22. [Recipe 22: Dapper SQL Translation (Oracle)](#recipe-22-dapper-sql-translation-oracle)
23. [Recipe 23: Compile-Time Column Resolution ([SpecColumnResolver])](#recipe-23-compile-time-column-resolution-speccolumnresolver)
24. [Recipe 24: Strongly-Typed Ordering Helpers with Source Generator ([Spec])](#recipe-24-strongly-typed-ordering-helpers-with-source-generator-spec)
25. [Recipe 25: High-Throughput In-Memory Evaluation with Bounded LRU Cache](#recipe-25-high-throughput-in-memory-evaluation-with-bounded-lru-cache)
26. [Recipe 26: 100% NativeAOT & Dapper.AOT Configuration](#recipe-26-100-nativeaot--dapperaot-configuration)
27. [Recipe 27: OpenTelemetry Metrics & Diagnostic Tracing](#recipe-27-opentelemetry-metrics--diagnostic-tracing)
28. [Recipe 28: Functional Result Pattern (ReadRepositoryResultExtensions)](#recipe-28-functional-result-pattern-readrepositoryresultextensions)
29. [Recipe 29: Fluent MongoDB Querying (MongoSpecificationEvaluator)](#recipe-29-fluent-mongodb-querying-mongospecificationevaluator)

---

## Recipe 1: Define a Reusable Domain Specification

**Problem**: Encapsulate a core business rule in the domain layer without ORM or infrastructure dependencies.

```csharp
// Domain layer — zero infrastructure dependencies
public sealed class ActivePremiumCustomer : Specification<Customer>
{
    private readonly decimal _minimumPurchases;

    public ActivePremiumCustomer(decimal minimumPurchases = 1_000m)
    {
        _minimumPurchases = minimumPurchases;
    }

    protected override Expression<Func<Customer, bool>> BuildExpression()
        => c => c.IsActive && c.TotalPurchases >= _minimumPurchases && c.DeletedAt == null;
}

// In-memory evaluation (NativeAOT-safe, zero IL generation)
var spec = new ActivePremiumCustomer(minimumPurchases: 500m);
bool isEligible = spec.IsSatisfiedBy(new Customer { IsActive = true, TotalPurchases = 750m });
```

> **Architectural Rule**: Always seal concrete specifications (`SPEC001` analyzer rule).

---

## Recipe 2: Compose Specifications (AND / OR / NOT)

**Problem**: Dynamically combine multiple business rules while preserving clean expression trees for LINQ and SQL translators.

```csharp
var active = new ActiveCustomer();
var premium = new PremiumTier(minimumPurchases: 1_000m);
var notDeleted = Spec.For<Customer>(c => c.DeletedAt == null);

// AND composition — parameter rewriting, no Expression.Invoke
var vipAndActive = active.And(premium).And(notDeleted);

// OR composition
var promoEligible = active.Or(premium);

// NOT composition
var inactiveCustomer = active.Not();

// Direct translation to SQL / LINQ
var results = dbContext.Customers.Where(vipAndActive.ToExpression()).ToList();
```

---

## Recipe 3: Bulk Composition (Spec.All / Spec.Any)

**Problem**: Combine a dynamic collection of specifications using balanced expression trees without deep nesting.

```csharp
var filters = new List<Specification<Customer>>
{
    new ActiveCustomer(),
    new CountryCustomer("US"),
    new MinimumCreditLimit(500m)
};

// Composes all with AND (returns Spec.True<T>() if list is empty)
Specification<Customer> combinedAnd = Spec.All(filters.ToArray());

// Composes all with OR (returns Spec.False<T>() if list is empty)
Specification<Customer> combinedOr = Spec.Any(filters.ToArray());
```

---

## Recipe 4: Inline Specifications with Spec.For<T>

**Problem**: Create an ad-hoc specification without declaring a dedicated class.

```csharp
var highValueSpec = Spec.For<Customer>(c => c.TotalPurchases > 10_000m);

// Compose inline specs directly with domain specifications
var qualifiedSpec = new ActiveCustomer().And(highValueSpec);

bool qualifies = qualifiedSpec.IsSatisfiedBy(customer);
```

---

## Recipe 5: Multi-Column Search (Spec.Search)

**Problem**: Search for a substring across multiple string properties using logical OR.

```csharp
// Multi-column search specification
var searchSpec = Spec.Search<Customer>(
    searchPhrase: "Acme",
    c => c.Name,
    c => c.Description,
    c => c.ContactEmail
);

// Evaluates in-memory or in database queries
bool matches = searchSpec.IsSatisfiedBy(customer);
```

---

## Recipe 6: Inclusive Range Filtering (Spec.Between)

**Problem**: Filter records within an inclusive upper and lower range.

```csharp
// Strongly-typed range specification
var creditRangeSpec = Spec.Between<Customer, decimal>(
    c => c.CreditLimit,
    lower: 1_000m,
    upper: 5_000m
);

// Works with any IComparable<TProperty> (dates, numbers, timestamps)
var dateRangeSpec = Spec.Between<Order, DateTime>(
    o => o.CreatedAt,
    lower: DateTime.UtcNow.AddDays(-30),
    upper: DateTime.UtcNow
);
```

---

## Recipe 7: Full-Text Search Filtering (Spec.FullText)

**Problem**: Match entities using provider-agnostic full-text search semantics.

```csharp
var ftsSpec = Spec.FullText<Product>(
    p => p.Description,
    searchPhrase: "ergonomic wireless keyboard"
);
```

---

## Recipe 8: Immutable Query Specification (QuerySpec<T>)

**Problem**: Define filtering, ordering, pagination, and uniqueness in an immutable descriptor.

```csharp
var query = QuerySpec<Customer>.Empty
    .Where(c => c.IsActive)
    .Where(c => c.CountryCode == "US")
    .OrderByDescending(c => c.TotalPurchases)
    .ThenBy(c => c.Name)
    .Page(page: 1, pageSize: 25)
    .Distinct();

// Convert domain specification into QuerySpec
var spec = new ActivePremiumCustomer();
QuerySpec<Customer> customerQuery = spec.ToQuerySpec()
    .OrderBy(c => c.Name)
    .Take(50);
```

---

## Recipe 9: Keyset / Cursor Pagination

**Problem**: Implement high-performance keyset pagination without slow `OFFSET` scans on large tables.

```csharp
// Forward seek (Next Page)
var nextPageQuery = QuerySpec<Order>.Empty
    .Where(o => o.IsPaid)
    .OrderByDescending(o => o.Id)
    .SeekAfter(o => o.Id, cursorValue: 105420, take: 20);

// Backward seek (Previous Page)
var prevPageQuery = QuerySpec<Order>.Empty
    .Where(o => o.IsPaid)
    .OrderBy(o => o.Id)
    .SeekBefore(o => o.Id, cursorValue: 105420, take: 20);
```

---

## Recipe 10: Multi-Column Search with QuerySpec

**Problem**: Add search filters to a fluent QuerySpec chain.

```csharp
var query = QuerySpec<Customer>.Empty
    .Where(c => c.IsActive)
    .Search("Enterprise", c => c.Name, c => c.Industry)
    .OrderBy(c => c.Name)
    .Page(1, 20);
```

---

## Recipe 11: Diagnostic Query Tagging (TagWith)

**Problem**: Tag generated database queries with diagnostic comments for distributed tracing, profiling, and APM tools.

```csharp
var query = QuerySpec<Customer>.Empty
    .Where(c => c.IsActive)
    .TagWith("TenantId:42;Handler:GetActiveCustomersQuery");

// EF Core emits:
// -- TenantId:42;Handler:GetActiveCustomersQuery
// SELECT [c].[Id], [c].[Name] FROM [Customers] AS [c] WHERE [c].[IsActive] = 1
```

---

## Recipe 12: Projected Queries (QuerySpec<T, TResult>)

**Problem**: Retrieve strongly-typed DTOs instead of full entity models.

```csharp
public sealed record CustomerSummaryDto(int Id, string Name, decimal TotalPurchases);

var projectedQuery = QuerySpec<Customer, CustomerSummaryDto>.Empty
    .Where(c => c.IsActive)
    .OrderByDescending(c => c.TotalPurchases)
    .Take(10)
    .Select(c => new CustomerSummaryDto(c.Id, c.Name, c.TotalPurchases));

// Execute via EF Core
List<CustomerSummaryDto> dtos = await dbContext.Customers
    .Apply(projectedQuery)
    .ToListAsync();
```

---

## Recipe 13: Execute QuerySpec on EF Core (IQueryable.Apply)

**Problem**: Apply a specification directly onto an EF Core `IQueryable<T>`.

```csharp
using EricksonLopez.Specification.Linq;

// Applies criteria, order clauses, pagination, distinct, and tag
IQueryable<Customer> queryable = dbContext.Customers.Apply(querySpec);

var totalCount = await queryable.CountAsync();
var pagedData = await queryable.ToListAsync();
```

---

## Recipe 14: EF Core Query Hints (AsSplitQuery & IgnoreAutoIncludes)

**Problem**: Apply performance-sensitive query hints when evaluating specifications in EF Core.

```csharp
using EricksonLopez.Specification.EntityFrameworkCore;

// Configures split query and disables navigation auto-loading
IQueryable<Order> query = dbContext.Orders.Apply(
    orderQuerySpec,
    asSplitQuery: true,
    ignoreAutoIncludes: true
);

var orders = await query.ToListAsync();
```

---

## Recipe 15: Generic EF Core Read Repository & GetByIdAsync

**Problem**: Implement clean architecture with `IReadRepository<T>` and primary-key lookups.

```csharp
// Inject IReadRepository<Customer>
public class CustomerService
{
    private readonly IReadRepository<Customer> _repository;

    public CustomerService(IReadRepository<Customer> repository)
    {
        _repository = repository;
    }

    public async Task<Customer?> GetCustomerAsync(int id, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(id, ct);
    }

    public async Task<IReadOnlyList<Customer>> GetActiveVipsAsync(CancellationToken ct = default)
    {
        var spec = new ActivePremiumCustomer().ToQuerySpec().OrderBy(c => c.Name);
        return await _repository.ListAsync(spec, ct);
    }
}
```

---

## Recipe 16: Custom ISpecificationEvaluator for EF Core

**Problem**: Intercept and customize specification evaluation in EF Core for tenant filtering or soft-deletes.

```csharp
public sealed class MultiTenantSpecificationEvaluator : ISpecificationEvaluator
{
    private readonly ITenantProvider _tenantProvider;

    public MultiTenantSpecificationEvaluator(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public IQueryable<T> GetQuery<T>(IQueryable<T> source, QuerySpec<T> specification)
    {
        // Enforce tenant isolation before evaluating specification
        var query = source.Apply(specification);
        return query;
    }

    public IQueryable<TResult> GetQuery<T, TResult>(IQueryable<T> source, QuerySpec<T, TResult> specification)
    {
        return source.Apply(specification);
    }
}
```

---

## Recipe 17: Dependency Injection Registration

**Problem**: Register specification repositories and evaluators with `IServiceCollection`.

```csharp
public static void ConfigureServices(IServiceCollection services, IConfiguration config)
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(config.GetConnectionString("Default")));

    // Registers IReadRepository<> and ISpecificationEvaluator
    services.AddSpecificationEntityFramework<AppDbContext>();

    // Or register strongly typed repository:
    services.AddEfReadRepository<AppDbContext, Customer>();
}
```

---

## Recipe 18: Dapper SQL Translation (PostgreSQL)

**Problem**: Generate optimized parameterized SQL for PostgreSQL using Dapper.

```csharp
using EricksonLopez.Specification.Dapper;
using EricksonLopez.Specification.PostgreSql;

var translator = new QuerySpecTranslator<Customer>("customers");
var dialect = PostgreSqlDialect.Default;

var spec = new ActiveCustomer().ToQuerySpec()
    .Where(c => c.CountryCode == "US")
    .OrderByDescending(c => c.TotalPurchases)
    .Page(page: 1, pageSize: 20);

// Generates:
// SELECT * FROM "customers" WHERE "is_active" = @p1 AND "country_code" = @p2 ORDER BY "total_purchases" DESC LIMIT @_take OFFSET @_skip
IEnumerable<Customer> items = await connection.QueryAsync(spec, translator, dialect);
int count = await connection.CountAsync(spec, translator, dialect);
bool exists = await connection.AnyAsync(spec, translator, dialect);
```

---

## Recipe 19: Dapper SQL Translation (Microsoft SQL Server)

**Problem**: Generate optimized T-SQL with `TOP` / `OFFSET FETCH` for SQL Server.

```csharp
using EricksonLopez.Specification.Dapper;
using EricksonLopez.Specification.MsSql;

var translator = new QuerySpecTranslator<Customer>("Customers");
var dialect = MsSqlDialect.Default;

var spec = QuerySpec<Customer>.Empty
    .Where(c => c.IsActive)
    .OrderBy(c => c.Name)
    .Page(page: 2, pageSize: 10);

// Generates:
// SELECT * FROM [Customers] WHERE [is_active] = @p1 ORDER BY [name] ASC OFFSET @_skip ROWS FETCH NEXT @_take ROWS ONLY
IEnumerable<Customer> customers = await connection.QueryAsync(spec, translator, dialect);
```

---

## Recipe 20: Dapper SQL Translation (MySQL / MariaDB)

**Problem**: Generate backtick-quoted SQL for MySQL or MariaDB.

```csharp
using EricksonLopez.Specification.Dapper;
using EricksonLopez.Specification.MySql;
// Or using EricksonLopez.Specification.MariaDb;

var translator = new QuerySpecTranslator<Customer>("customers");
var dialect = MySqlDialect.Default; // Or MariaDbDialect.Default

var spec = QuerySpec<Customer>.Empty
    .Where(c => c.TotalPurchases > 100m)
    .OrderBy(c => c.Id)
    .Take(50);

// Generates:
// SELECT * FROM `customers` WHERE `total_purchases` > @p1 ORDER BY `id` ASC LIMIT @_take
IEnumerable<Customer> result = await connection.QueryAsync(spec, translator, dialect);
```

---

## Recipe 21: Dapper SQL Translation (SQLite)

**Problem**: Generate lightweight SQLite SQL queries.

```csharp
using EricksonLopez.Specification.Dapper;
using EricksonLopez.Specification.Sqlite;

var translator = new QuerySpecTranslator<Customer>("customers");
var dialect = SqliteDialect.Default;

IEnumerable<Customer> result = await connection.QueryAsync(spec, translator, dialect);
```

---

## Recipe 22: Dapper SQL Translation (Oracle)

**Problem**: Generate double-quoted SQL with positional `:p` parameters and `FETCH FIRST` for Oracle.

```csharp
using EricksonLopez.Specification.Dapper;
using EricksonLopez.Specification.Oracle;

var translator = new QuerySpecTranslator<Customer>("CUSTOMERS");
var dialect = OracleDialect.Default;

// Generates:
// SELECT * FROM "CUSTOMERS" WHERE "IS_ACTIVE" = :p1 ORDER BY "ID" ASC OFFSET :_skip ROWS FETCH NEXT :_take ROWS ONLY
IEnumerable<Customer> result = await connection.QueryAsync(spec, translator, dialect);
```

---

## Recipe 23: Compile-Time Column Resolution ([SpecColumnResolver])

**Problem**: Eliminate runtime reflection in SQL translators for 100% NativeAOT compatibility.

```csharp
// Source-generated zero-reflection resolver
[SpecColumnResolver(typeof(Customer), Convention = NamingConvention.SnakeCase)]
public sealed partial class CustomerColumnResolver;

// Pass into translator
var translator = new QuerySpecTranslator<Customer>(
    tableName: "customers",
    columnNameResolver: new CustomerColumnResolver()
);
```

---

## Recipe 24: Strongly-Typed Ordering Helpers with Source Generator ([Spec])

**Problem**: Generate compile-time type-safe ordering expressions for entity properties.

```csharp
[Spec(typeof(Customer))]
public partial class CustomerOrderings;

// Generates static expressions:
// CustomerOrderings.OrderById
// CustomerOrderings.OrderByName
// CustomerOrderings.OrderByTotalPurchases

var query = QuerySpec<Customer>.Empty
    .Where(c => c.IsActive)
    .OrderBy(CustomerOrderings.OrderByTotalPurchases);
```

---

## Recipe 25: High-Throughput In-Memory Evaluation with Bounded LRU Cache

**Problem**: Evaluate dynamic expressions millions of times in memory without unbound memory growth or repeated compilation costs.

```csharp
// Configure cache capacity (defaults to 512)
ExpressionCompilationCache.Capacity = 1024;

// Get or compile with automatic structural expression hashing & O(1) LRU eviction
Func<Customer, bool> compiled = spec.ToCompiledPredicate();

// Execute in tight loops
foreach (var customer in customerBatch)
{
    if (compiled(customer))
    {
        ProcessCustomer(customer);
    }
}
```

---

## Recipe 26: 100% NativeAOT & Dapper.AOT Configuration

**Problem**: Deploy applications under NativeAOT compilation with zero reflection warnings and complete type safety.

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <IsAotCompatible>true</IsAotCompatible>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

```csharp
// In NativeAOT:
// 1. Use IsSatisfiedBy() for domain specifications (ExpressionInterpreter evaluates AST without dynamic code generation)
// 2. Use [SpecColumnResolver] for Dapper translators
// 3. Use Dapper.AOT [DapperAot] context
[DapperAot]
public partial class AppDapperContext : IDapperContext;
```

---

## Recipe 27: OpenTelemetry Metrics & Diagnostic Tracing

**Problem**: Monitor specification execution count, composition frequency, and evaluation performance in production.

```csharp
using OpenTelemetry.Metrics;

var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("EricksonLopez.Specification")
    .AddPrometheusExporter()
    .Build();

// Metrics recorded automatically:
// - specification.evaluations_total
// - specification.compositions_total
// - specification.sql_translations_total
```

---

## Recipe 28: Functional Result Pattern (ReadRepositoryResultExtensions)

**Problem**: Execute specifications via `IReadRepository<T>` returning functional `Result<T>` envelopes without throwing expected business exceptions (like not-found or multiple matches).

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Result;
using EricksonLopez.Specification;
using EricksonLopez.Specification.Result;

public sealed class GetCustomerByIdQueryHandler
{
    private readonly IReadRepository<Customer> _repository;

    public GetCustomerByIdQueryHandler(IReadRepository<Customer> repository)
    {
        _repository = repository;
    }

    public async Task<Result<Customer>> Handle(Guid customerId, CancellationToken ct)
    {
        // Executes query returning Result.Success(customer) or Result.Failure(Error.NotFound)
        Result<Customer> result = await _repository.GetByIdResultAsync<Customer, Guid>(customerId, ct);

        return result;
    }
}
```

> **Note**: `OperationCanceledException` is automatically preserved and rethrown to ensure cooperative task cancellation.

---

## Recipe 29: Fluent MongoDB Querying (MongoSpecificationEvaluator)

**Problem**: Query MongoDB collections using domain specifications and `QuerySpec<TDocument>` directly via fluent drivers.

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using EricksonLopez.Specification;
using EricksonLopez.Specification.MongoDB;

public sealed class MongoCustomerService
{
    private readonly IMongoCollection<Customer> _mongoCollection;

    public MongoCustomerService(IMongoCollection<Customer> mongoCollection)
    {
        _mongoCollection = mongoCollection;
    }

    public async Task<List<Customer>> GetActiveVipCustomersAsync(CancellationToken ct)
    {
        var activeSpec = new ActiveCustomerSpecification();
        var querySpec = QuerySpec<Customer>.Empty
            .And(activeSpec)
            .OrderByDescending(c => c.TotalPurchases)
            .Page(page: 1, pageSize: 20);

        // Fluent query execution on IMongoCollection<Customer>:
        IFindFluent<Customer, Customer> findFluent = _mongoCollection.Find(querySpec);

        return await findFluent.ToListAsync(ct);
    }
}
```

