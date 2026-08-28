# What EricksonLopez.Specification Will NOT Become

> This document is a **constraint contract**, not a feature list.  
> Every item here has been explicitly evaluated and rejected. Not through neglect, but through deliberate architectural decision.  
> When a new feature is requested, this document is the first stop.

---

## The One-Line Test

Before any feature is considered, apply this test:

> _"Does this feature express, compose, or translate a business predicate? Or does it do something else?"_

If the answer is "something else" — it does not belong in this library.

---

## What This Library Is

A small, composable, AOT-first, provider-independent abstraction for:

1. **Encoding** business rules as typed expression trees
2. **Composing** those rules with AND / OR / NOT without ORM coupling
3. **Evaluating** those rules in-memory (AOT-safe interpreter) or as compiled delegates (JIT)
4. **Translating** those rules to IQueryable predicates or SQL WHERE clauses via adapters

---

## What This Library Will NOT Become

### ❌ ORM

EricksonLopez.Specification is not, and will never be, an Object-Relational Mapper.

It does not:
- Track entity state
- Generate DDL
- Manage identity maps
- Implement lazy loading
- Provide change detection
- Manage relationships

ADR: adr-010 (No EF Core in core), adr-002 (No Include/ThenInclude)

---

### ❌ Repository Implementation

The library provides `IReadRepository<T>` as an **interface contract** — a port for the domain layer.

It does not provide implementations. Repository implementations belong in the Infrastructure layer and depend on a specific ORM or data access technology.

ADR: adr-001 (No write repository)

---

### ❌ Unit of Work

The Unit of Work pattern coordinates transactions across multiple repositories. This is an Infrastructure concern that has no relationship to predicate composition.

---

### ❌ Change Tracker

Tracking which entities have been modified, added, or deleted is an ORM concern. Specifications express selection intent, not state mutation intent.

---

### ❌ Lazy Loading

Navigation property loading via proxies or interceptors requires deep ORM integration. Specifications are predicates — they do not load related data.

The `Include`/`ThenInclude` pattern from Ardalis.Specification was explicitly evaluated and rejected. See adr-002.

---

### ❌ Database Connection Management

Dapper adapter methods accept `IDbConnection` from the consumer. The library does not:
- Create connections
- Manage connection pools
- Open or close connections
- Configure connection strings

---

### ❌ SQL Execution Engine

The `Sql` package translates specifications to SQL. It does not execute SQL. Execution is the consumer's responsibility (via Dapper, ADO.NET, or another library).

ADR: adr-013 (No raw SQL), adr-011 (No dynamic string queries)

---

### ❌ Transaction Manager

Transactions are a concern of the Unit of Work or the consumer's application layer. The Dapper adapter accepts an optional `IDbTransaction` — it does not create or manage transactions.

---

### ❌ CQRS Framework

This library provides query descriptors (`QuerySpec<T>`). It is not a dispatcher, mediator, or CQRS bus. Handlers that use specifications are application-layer concerns.

---

### ❌ Mediator

There is no `ISender`, `IMediator`, `IPublisher`, or request/response pipeline in this library. MediatR, Wolverine, Brighter, and similar libraries serve that purpose. They may use specifications internally, but specifications do not depend on them.

---

### ❌ Validation Framework

Specifications express selection predicates. Validation frameworks (FluentValidation, DataAnnotations) express validation rules with error messages, severity levels, and correction hints.

These are related but distinct concerns. Using a specification to validate is a valid consumer pattern. Embedding FluentValidation in the specification core is not.

ADR: adr-007 (No FluentValidation integration)

---

### ❌ Authorization Framework

Specifications can compose authorization rules (e.g., `VisibleToUserSpec`). However:
- Authorization policies have identity, roles, claims, and policy evaluation pipelines
- Those concerns belong in ASP.NET Core Authorization or a dedicated policy library
- This library provides predicates that authorization logic may use, not authorization itself

---

### ❌ Caching Framework

The library caches:
1. Compiled expression delegates (`ExpressionCompilationCache`) — bounded, JIT-only
2. SQL query plans (`QueryPlanCache`) — bounded LRU

It does not cache query results. Result caching (e.g., Redis, MemoryCache) is an application-layer concern.

---

### ❌ Distributed Cache

Query result distribution, invalidation strategies, and cache consistency are infrastructure concerns for the application layer.

---

### ❌ Persistence Abstraction (beyond IReadRepository)

`IReadRepository<T>` is provided as a minimal read-side contract. The library does not provide:
- `IPersistenceContext`
- `IUnitOfWork`
- Generic CRUD repositories
- Session management
- Data mapper abstractions

---

### ❌ Migration Framework

Schema management, database migrations, and versioning are ORM and infrastructure concerns. The library has no schema awareness.

---

### ❌ Schema Management

The library does not know about or manage database schemas, tables, or column definitions (beyond the `IColumnNameResolver` for property-to-column name mapping).

---

### ❌ Domain Event System

Specifications evaluate conditions. They do not raise, publish, or handle domain events. Events are a consequence of state changes — predicates describe state.

---

### ❌ Application Service Framework

The library provides building blocks (predicates, query descriptors). It does not provide:
- Application service base classes
- Command/query handlers
- Pipeline behaviors
- Request/response decorators

---

### ❌ Dynamic LINQ / String-Based Queries

`OrderBy("Name")`, `Where("IsActive == true")`, or runtime-evaluated string expressions will not be supported.

Reasons:
- SQL injection risk
- No compile-time type safety
- No AOT compatibility
- No expression tree composition
- No translator support

ADR: adr-003, adr-011, adr-014

---

### ❌ Raw SQL in the Public API

`WhereRaw("custom SQL fragment")` will not exist in any public adapter.

`RawPredicateNode` exists as an internal AST node but is not exposed in the public SQL translation API.

ADR: adr-013

---

### ❌ Async Specifications

`Task<bool> IsSatisfiedByAsync(T entity)` will not be added.

A specification that performs I/O is not a specification — it is a domain service. The pattern for async scenarios: resolve external data first, then construct a synchronous specification.

ADR: adr-017

---

### ❌ GroupBy / Aggregations / SelectMany in QuerySpec

`QuerySpec<T>` is a selection descriptor for a single entity type. It expresses:
- Which entities to select (predicates)
- In what order (ordering)
- How many (pagination)
- What shape (projection)

It does not express aggregations (COUNT, SUM, AVG, GROUP BY), set operations (UNION, INTERSECT), or cross-entity joins.

ADR: adr-015

---

### ❌ Auto-Generated BuildExpression

Roslyn code generators that auto-generate `BuildExpression()` method bodies from attributes are rejected.

The expression is the domain rule. Generating it from metadata inverts the ownership — the business rule should be written by a domain expert, not generated from a database schema or attribute.

ADR: adr-016

---

### ❌ SAT-Based Predicate Simplification

Boolean satisfiability-based simplification (e.g., eliminating tautologies, contradictions, or redundant subexpressions at the propositional logic level) is rejected.

The cost: algorithmic complexity (NP-hard in the general case), implementation maintenance, and risk of semantic changes. The benefit: negligible for real-world specifications.

Simpler optimizations (constant folding, double-negation elimination) are implemented and sufficient.

ADR: adr-004

---

### ❌ XOR Composition

`Specification.Xor(other)` will not be added.

XOR (`A XOR B = (A AND NOT B) OR (NOT A AND B)`) has no documented domain use case in the Specification Pattern literature. It is composable from AND, OR, NOT — no additional API surface is needed.

ADR: adr-005

---

## Summary Table

| Category | Verdict | ADR |
|---|---|---|
| ORM | ❌ Rejected | adr-010, adr-002 |
| Write repository | ❌ Rejected | adr-001 |
| Unit of Work | ❌ Rejected | — |
| Change tracking | ❌ Rejected | — |
| Lazy loading / Includes | ❌ Rejected | adr-002 |
| Transaction management | ❌ Rejected | — |
| SQL execution engine | ❌ Rejected | — |
| CQRS framework | ❌ Rejected | — |
| Mediator | ❌ Rejected | — |
| Validation framework | ❌ Rejected | adr-007 |
| Authorization framework | ❌ Rejected | — |
| Result caching | ❌ Rejected | — |
| Distributed cache | ❌ Rejected | — |
| Dynamic LINQ / strings | ❌ Rejected | adr-003, adr-011, adr-014 |
| Raw SQL in public API | ❌ Rejected | adr-013 |
| Async specifications | ❌ Rejected | adr-017 |
| GroupBy / Aggregations | ❌ Rejected | adr-015 |
| Auto-generated BuildExpression | ❌ Rejected | adr-016 |
| SAT simplification | ❌ Rejected | adr-004 |
| XOR composition | ❌ Rejected | adr-005 |
| Application service framework | ❌ Rejected | — |
| Domain events | ❌ Rejected | — |

---

*Last updated: 2026-08-14 — Architectural audit post-report*
