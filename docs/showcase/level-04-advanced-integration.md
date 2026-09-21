# Level 04: Advanced Integration — SQL Translation Pipeline

## Overview

Level 4 demonstrates the SQL translation pipeline that converts QuerySpec<T> into parameterized SQL queries without ORM dependencies. This is the core of the Dapper integration.

## Key Components

- **QuerySpecTranslator<T>**: Traverses QuerySpec<T> expression trees and builds a QueryModel (SQL AST).
- **QueryModel**: Provider-agnostic record: TableName, Filters, Orders, Skip, Take, IsDistinct, QueryType, Parameters, Projections.
- **ISqlDialect**: Renders QueryModel into dialect-specific SQL with parameters.
- **IColumnNameResolver**: Maps C# property names to database column names.
- **QueryPlanCache**: Thread-safe bounded LRU cache for translated plans.
- **SqlQuery**: Result record containing Sql string and Parameters dictionary.

## Running Example (Level4_AdvancedIntegration.cs)

See [Level4_AdvancedIntegration.cs](../../samples/Showcase/Levels/Level4_AdvancedIntegration.cs) for the complete executable demonstration of:

1. QuerySpecTranslator<Customer>("customers", SnakeCaseColumnNameResolver.Default)
2. Rendering with 6 dialects: PostgreSqlDialect, MsSqlDialect, SqliteDialect, MySqlDialect, MariaDbDialect, OracleDialect
3. QueryPlanCache — LRU cache with TryGetPlan, SetPlan, Clear, Count, Capacity
4. SqlPredicateNode AST inspection: AndPredicateNode, BinaryPredicateNode, InPredicateNode, BetweenPredicateNode
5. QuerySpec<T,TResult> with .Select() projection
6. SeekAfter / SeekBefore keyset pagination with WithCursor
7. TagWith() for diagnostic query labeling

## Supported Expressions

The translator handles: member access, constants, binary comparisons (==, !=, <, >, <=, >=), Contains, StartsWith, EndsWith, boolean AND/OR/NOT, null comparisons.

**Unsupported**: string.IsNullOrEmpty(), complex method chains → use equivalent expressions instead.

## Column Name Resolvers

| Resolver | IsActive → | Usage |
|---|---|---|
| SnakeCaseColumnNameResolver.Default | is_active | PostgreSQL, standard SQL |
| VerbatimColumnNameResolver.Default | IsActive | EF Core shadow properties |
| Custom IColumnNameResolver | Any mapping | Legacy databases |
