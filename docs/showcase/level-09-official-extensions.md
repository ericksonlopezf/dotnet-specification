# Level 09: Official Extensions — Dapper, EF Core, MongoDB

## Overview

Level 9 demonstrates all official library integrations: Dapper SQL execution, EF Core helpers, and MongoDB.

## QuerySpecDapperExtensions

Executes QuerySpec<T> directly against IDbConnection:

`csharp
// QueryAsync — list with filters, ordering, pagination
IEnumerable<Customer> customers = await connection.QueryAsync<Customer>(
    spec, translator, dialect);

// QueryFirstOrDefaultAsync — single entity
Customer? first = await connection.QueryFirstOrDefaultAsync<Customer>(
    spec, translator, dialect);

// CountAsync — scalar count
int count = await connection.CountAsync<Customer>(
    spec, translator, dialect);

// AnyAsync — existence check
bool exists = await connection.AnyAsync<Customer>(
    spec, translator, dialect);
`

## QuerySpecLinqExtensions / QuerySpecEfCoreExtensions

`csharp
// LINQ Apply — works with EF Core DbSet or any IQueryable
IQueryable<Customer> query = dbContext.Customers.Apply(spec);

// EF Core specific options
IQueryable<Customer> splitQuery = dbContext.Customers
    .Apply(spec, asSplitQuery: true, ignoreAutoIncludes: true);
`

## EF Core Dependency Injection

`csharp
services.AddSpecificationEntityFramework();
services.AddEfReadRepository<MyDbContext, Customer>();
// Registers EfReadRepository<MyDbContext, Customer> : IReadRepository<Customer>
`

## MongoSpecificationEvaluator

`csharp
// Build FilterDefinition and SortDefinition
FilterDefinition<Customer> filter = MongoSpecificationEvaluator.GetFilter(spec);
SortDefinition<Customer>? sort = MongoSpecificationEvaluator.GetSort(spec);

// Fluent find
IFindFluent<Customer, Customer> find = collection.Find(spec);

// Apply to existing find
findFluent.ApplySpecification(spec);

// Async execution
IReadOnlyList<Customer> results = await collection.FindAsync(spec);
long count = await collection.CountDocumentsAsync(spec);
`

## Dialect Selection

| Database | Dialect | NuGet |
|---|---|---|
| PostgreSQL | PostgreSqlDialect.Default | EricksonLopez.Specification.PostgreSql |
| SQL Server | MsSqlDialect.Default | EricksonLopez.Specification.MsSql |
| SQLite | SqliteDialect.Default | EricksonLopez.Specification.Sqlite |
| MySQL | MySqlDialect.Default | EricksonLopez.Specification.MySql |
| MariaDB | MariaDbDialect.Default | EricksonLopez.Specification.MariaDb |
| Oracle | OracleDialect.Default | EricksonLopez.Specification.Oracle |

## Running Example

See [Level9_Extensions.cs](../../samples/Showcase/Levels/Level9_Extensions.cs).
