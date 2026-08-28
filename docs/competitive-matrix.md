# COMPETITIVE-MATRIX — EricksonLopez.Specification

> **Version**: 1.0 — 2026-08-13
> **Methodology**: Source code analysis, NuGet metadata, GitHub issue tracking, architecture inference.
> **Critical stance**: This document does not assume EricksonLopez wins every comparison.

---

## Competitors in Scope

| Library | Downloads | Status | Category |
|---|---|---|---|
| Ardalis.Specification | 18.8M | Active v9.3.1 | Direct competitor |
| LinqKit.Core | 80M+ | Active | Expression composition utility (partial overlap) |
| EF Core (native) | 1B+ | Active | Platform (not a spec library) |
| NSpecifications | 566K | Low activity | Classic GoF pattern |
| FluentSpecification | 28.6K | Inactive | Validation focused |
| Sieve / Gridify / QueryKit | Varies | Active | Web API filter parsers (different category) |
| Dynamic LINQ | Varies | Active | String-based queries (anti-pattern to our philosophy) |

**Non-competitors clarification**:
- Sieve, Gridify, QueryKit: HTTP query string parsers. Different layer, different problem.
- Dynamic LINQ: string-based queries — antithesis of type safety.
- LinqKit: expression composition utility, NOT a Specification library.

---

## Full Feature Comparison Matrix

Legend: Native=first-class | Partial=limited/workaround | Rejected=intentionally absent | Adapter=provider-only | N/A=not applicable

### Core Specification

| Feature | EricksonLopez | Ardalis | LinqKit |
|---|---|---|---|
| `ISpecification<T>` minimal interface | Native | Native | N/A |
| `Specification<T>` abstract base | Native | Native | N/A |
| `Spec.For<T>(expr)` factory | Native | Not supported | N/A |
| `Spec.True<T>()` / `Spec.False<T>()` | Native | Partial | N/A |
| `IsSatisfiedBy(T)` in-memory | Native | Native | Not supported |
| `ToExpression()` expose tree | Native | Partial | N/A |
| **Immutable specification** | **Native** | **Rejected** | N/A |
| Thread-safe specification | Native | Partial | N/A |
| `IExpressionSpecification<T>` | Native | Not supported | N/A |
| Sealed specs enforced by analyzer | Native | Not supported | N/A |

### Predicate Composition

| Feature | EricksonLopez | Ardalis | LinqKit |
|---|---|---|---|
| `And(spec)` | Native | Partial (no typed API) | Native |
| `Or(spec)` | Native | Partial | Native |
| `Not()` | Native | Native | Native |
| **`Expression.Invoke`-free composition** | **Native** | **Partial** | **Rejected** |
| `AndAll(ReadOnlySpan)` bulk AND | Native | Not supported | Not supported |
| `OrAny(ReadOnlySpan)` bulk OR | Native | Not supported | Not supported |
| Boolean constant folding | Native | Not supported | Not supported |
| `NOT(NOT A) = A` elimination | Native | Not supported | Not supported |
| XOR composition | Rejected | Rejected | Rejected |
| SAT-based simplification | Rejected | Rejected | Rejected |

### Query Descriptor

| Feature | EricksonLopez | Ardalis |
|---|---|---|
| **Immutable query descriptor (sealed record)** | **Native** | **Rejected** |
| Multiple AND criteria ImmutableArray | Native | Native |
| `OrderBy<TKey>` strongly typed | Native | Native |
| `OrderByDescending<TKey>` | Native | Native |
| ThenBy / ThenByDescending | Native | Native |
| `Page(page, size)` offset pagination | Native | Native |
| Skip(n) / Take(n) | Native | Native |
| Distinct() | Native | Native |
| NoTracking() flag | Native | Native |
| SplitQuery() flag | Native | Native |
| `QuerySpec<T, TResult>` projected | Native | Partial |
| `QuerySpec<T>.Empty` | Native | Not supported |
| Keyset / cursor pagination (`SeekAfter`/`SeekBefore`) | Native | Not supported |
| Dynamic string ordering `OrderBy("Name")` | Rejected (adr-003) | Rejected |

### LINQ Integration

| Feature | EricksonLopez | Ardalis | LinqKit |
|---|---|---|---|
| `IQueryable<T>.Apply(querySpec)` | Native | Native | Not supported |
| Any(spec) / Count(spec) LINQ extensions | Native | Native | Native |
| Select projection | Native | Partial | Native |
| GroupBy | Rejected | Not supported | Not supported |
| SelectMany | Rejected | Not supported | Not supported |
| Include / ThenInclude in core | Rejected (adr-002) | Native | Not supported |

### Repository Contract

| Feature | EricksonLopez | Ardalis |
|---|---|---|
| `IReadRepository<T>` interface | Native | Native |
| FirstOrDefaultAsync / SingleOrDefaultAsync | Native | Native |
| ListAsync / CountAsync / AnyAsync | Native | Partial |
| `ListAsync<TResult>(QuerySpec<T,TResult>)` | Native | Partial |
| **`IRepository<T>` write operations** | **Rejected (adr-001)** | **Native** |

### SQL Translation (Dapper Path)

| Feature | EricksonLopez | Ardalis |
|---|---|---|
| **Expression-to-SQL pipeline** | **Native** | **Rejected** |
| **Provider-agnostic QueryModel AST** | **Native** | **Rejected** |
| **Pluggable ISqlDialect** | **Native** | **Rejected** |
| PostgreSQL dialect (`PostgreSqlDialect`) | Native | Rejected |
| Microsoft SQL Server dialect (`MsSqlDialect`) | Native | Rejected |
| MySQL & MariaDB dialects (`MySqlDialect`, `MariaDbDialect`) | Native | Rejected |
| SQLite dialect (`SqliteDialect`) | Native | Rejected |
| Oracle dialect (`OracleDialect`) | Native | Rejected |
| Parameterized queries | Native | Rejected |
| IColumnNameResolver | Native | Rejected |
| LIKE (Contains/StartsWith/EndsWith) | Native | Rejected |
| IN / ANY collection predicates | Native | Rejected |
| WhereRaw / raw SQL | Rejected (Radr-003) | Rejected |
| JOINs via specification | Rejected | Rejected |

### AOT / NativeAOT

| Feature | EricksonLopez | Ardalis | LinqKit | EF Core |
|---|---|---|---|---|
| **Core AOT compatible** | **Yes** | **No** | **No** | Experimental only |
| **AOT-safe in-memory evaluation** | **Yes (ExpressionInterpreter)** | **No** | **No** | No |
| `[RequiresDynamicCode]` annotations | Yes | No | No | Partial |
| `[DynamicallyAccessedMembers]` | Yes | No | No | Partial |
| Trimming-safe core | Yes | No | No | Experimental |

### Analyzers (Compile-Time)

| Feature | EricksonLopez | Ardalis | LinqKit |
|---|---|---|---|
| Roslyn diagnostic analyzers | SPEC001-SPEC011 + CodeFixes | None | None |
| SPEC003: blocks `Expression.Invoke` | Yes | None | None |
| SPEC001: enforces sealed specs | Yes | None | None |
| SPEC007: warns non-translatable methods | Yes | None | None |
| SPEC011: detects legacy Ardalis specs | Yes | None | None |

---

## Capability Scores (0-10 Scale)

| Capability | EricksonLopez | Ardalis | LinqKit | EF Core Native |
|---|---|---|---|---|
| Predicate composition | 10 | 7 | 10 | 8 |
| Expression.Invoke-free | 10 | 6 | 0 | 10 |
| In-memory evaluation | 9 | 6 | 0 | 0 |
| DDD correctness | 9 | 5 | 2 | 3 |
| NativeAOT compatibility | 9 | 0 | 0 | 3 |
| Trimming safety | 8 | 0 | 0 | 3 |
| Dapper / SQL-first | 9 | 0 | 0 | 0 |
| EF Core integration | 8 | 10 | 7 | 10 |
| Provider independence | 10 | 8 | 10 | 0 |
| Immutable design | 10 | 3 | 7 | 8 |
| Roslyn analyzers | 10 | 0 | 0 | 0 |
| Structural expression hashing | 9 | 0 | 0 | 0 |
| Low allocations | 8 | 6 | 5 | 6 |
| API minimalism | 9 | 5 | 8 | 7 |
| Testing ergonomics | 9 | 7 | 4 | 5 |
| Documentation | 8 | 9 | 6 | 10 |
| Ecosystem adoption | 2 (new) | 10 | 10 | 10 |

---

## Unique Differentiators (Verified from Source)

1. **Only** spec library with NativeAOT-safe in-memory evaluation (`ExpressionInterpreter`)
2. **Only** spec library with Expression->SQL pipeline for Dapper across 6 dialects (`QuerySpecTranslator<T>` + `ISqlDialect`)
3. **Only** spec library guaranteeing `Expression.Invoke`-free composition (`ParameterReplacer`)
4. **Only** spec library shipping 11 Roslyn analyzers (`SPEC001-SPEC011`) & automated CodeFix providers
5. **Only** spec library with immutable thread-safe query descriptor as sealed record
6. **Only** spec library with structural expression hashing for cache keys
7. **Only** spec library with boolean constant folding (`ExpressionSimplifier`)
8. **Only** spec library with `ReadOnlySpan`-based bulk AND/OR composition (zero intermediate allocation)
9. **Only** spec library with correct DDD boundary (predicate-only `Specification<T>`)
10. **Only** spec library with pluggable `IColumnNameResolver` for column mapping without entity attributes

---

## Honest Competitive Weaknesses

| Weakness | Impact | Mitigation |
|---|---|---|
| 0 downloads vs Ardalis 18.8M | Adoption | Correctness + docs + AOT sample |
| Less documentation than Ardalis | Adoption | NOW-04 complete; ongoing |
| EF Core deep integration less refined | Feature | Linq adapter covers 95% of cases |
| Include workarounds (in infra layer) | Developer experience | Cookbook documents this clearly |
| No community resources (YouTube, blogs) | Adoption | Content strategy post v1.0 |
| ExpressionInterpreter 5-20x slower (vs compiled) | Performance | Documented; acceptable for domain validation |

---

## Market Position

**DO NOT compete**: Ardalis's EF Core CRUD teams (18.8M user base; no compelling switch reason)

**OWN the niche**:
- Teams using NativeAOT (.NET 9+ AOT, MAUI, serverless, edge)
- Teams using Dapper alongside or instead of EF Core (500M+ downloads)
- Teams with strict DDD architecture (predicate-only specifications enforced)
- Teams wanting compile-time architectural guardrails

**Strategic statement**: The only .NET specification library that works in NativeAOT, translates to Dapper SQL, and keeps your domain layer clean.
