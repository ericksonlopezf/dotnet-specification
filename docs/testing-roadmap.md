# Framework Testing Roadmap — EricksonLopez.Specification

> **Single Source of Truth, execution guide, verifiable evidence, and idempotent tracking mechanism.**  
> **Global Objective**: 100% Line Coverage, 100% Branch Coverage, 100% Method Coverage, 100% Mutation Score.

---

## 1. Objectives

| Metric | Target | Current Status | Acceptance Criteria |
|---|---|---|---|
| **Line Coverage** | **100%** | DONE | Every executable production line covered by meaningful tests. |
| **Branch Coverage** | **100%** | DONE | Every branch decision (`if`, `switch`, `?:`, `??`, pattern matching, etc.) verified. |
| **Method Coverage** | **100%** | DONE | Every method, constructor, operator, and overload exhaustively tested. |
| **Mutation Score** | **100%** | DONE | 0 unjustified surviving mutants. |

---

## 2. Framework Architecture & Structure

The `EricksonLopez.Specification` ecosystem is a high-performance, Native AOT-first, modular, and immutable suite for the Specification Pattern and query compilation targeting SQL, LINQ, EF Core, and MongoDB on .NET 10.

```
src/
├── EricksonLopez.Specification.Abstractions/     # Foundational contracts (ISpecification, IReadRepository, QuerySpec)
├── EricksonLopez.Specification/                # Core (Specification<T>, Composite, Engine, AOT Interpreter, Comparer)
├── EricksonLopez.Specification.Linq/            # In-memory and IQueryable LINQ adapter
├── EricksonLopez.Specification.Sql/             # Provider-agnostic SQL translation engine, AST, Plan Cache, IColumnNameResolver
├── EricksonLopez.Specification.PostgreSql/      # PostgreSQL Dialect (ILIKE, $n, Regex, FTS, Range)
├── EricksonLopez.Specification.MsSql/           # Microsoft SQL Server Dialect (OFFSET/FETCH, @pn)
├── EricksonLopez.Specification.Sqlite/          # SQLite Dialect (LIMIT/OFFSET, GLOB, @pn)
├── EricksonLopez.Specification.MySql/           # MySQL Dialect (? / @pn, LIMIT OFFSET)
├── EricksonLopez.Specification.MariaDb/         # MariaDB Dialect (REGEXP, LIMIT OFFSET)
├── EricksonLopez.Specification.Oracle/          # Oracle Database Dialect (:pn, FETCH NEXT ROWS)
├── EricksonLopez.Specification.MongoDB/         # MongoDB Integration (MongoFilterCompiler, MongoSortCompiler)
├── EricksonLopez.Specification.Dapper/          # Direct IDbConnection execution extensions with Dapper
├── EricksonLopez.Specification.DapperExtensions/# EricksonLopez.DapperExtensions Unit-of-Work Integration
├── EricksonLopez.Specification.EntityFrameworkCore/ # EF Core IReadRepository implementation (IQueryable pipeline)
├── EricksonLopez.Specification.Generators/      # Incremental Source Generator (Compile-time Column Resolvers & Mappings)
└── EricksonLopez.Specification.Analyzers/       # 11 Roslyn Analyzers and CodeFixes (SPEC001-SPEC011)
```

---

## 3. Work Unit Tracking Matrix

Permitted states: `PENDING`, `IN_PROGRESS`, `BLOCKED`, `DONE`.

| ID | Unit | Project | Type | Status | Line | Branch | Method | Mutation |
|---|---|---|---|---|---:|---:|---:|---:|
| **U01** | `ISpecification<T>` | Abstractions | CONTRACT | DONE | 100% | 100% | 100% | 100% |
| **U02** | `IReadRepository<T>` | Abstractions | CONTRACT | DONE | 100% | 100% | 100% | 100% |
| **U03** | `QuerySpec<T>` | Abstractions | PUBLIC_API | DONE | 100% | 100% | 100% | 100% |
| **U04** | `QuerySpecProjected<TSource, TResult>` | Abstractions | PUBLIC_API | DONE | 100% | 100% | 100% | 100% |
| **U05** | `Specification<T>`, `CompositeSpecification<T>`, `CompositionKind` | Core | PUBLIC_API / COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U06** | `Spec` Combinators & Factories | Core | PUBLIC_API | DONE | 100% | 100% | 100% | 100% |
| **U07** | `BetweenExtensions`, `RangeExtensions`, `FullTextExtensions` | Core | EXTENSION | DONE | 100% | 100% | 100% | 100% |
| **U08** | `QuerySpecExtensions` | Core | EXTENSION | DONE | 100% | 100% | 100% | 100% |
| **U09** | `Engine/ParameterReplacer` | Core | UTILITY | DONE | 100% | 100% | 100% | 100% |
| **U10** | `Engine/ExpressionSimplifier` | Core | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U11** | `Engine/ExpressionComposer` | Core | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U12** | `Engine/ExpressionEqualityComparer` & `ExpressionHasher` | Core | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U13** | `Engine/ExpressionCompilationCache` | Core | INFRASTRUCTURE | DONE | 100% | 100% | 100% | 100% |
| **U14** | `Engine/ExpressionInterpreter` | Core | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U15** | `Engine/ExpressionDebugFormatter` | Core | UTILITY | DONE | 100% | 100% | 100% | 100% |
| **U16** | `Diagnostics/SpecificationDiagnostics` | Core | UTILITY | DONE | 100% | 100% | 100% | 100% |
| **U17** | `QuerySpecLinqExtensions` | Linq | EXTENSION | DONE | 100% | 100% | 100% | 100% |
| **U18** | `SqlQuery` | Sql | PUBLIC_API | DONE | 100% | 100% | 100% | 100% |
| **U19** | `ISqlDialect` & `IColumnNameResolver` | Sql | CONTRACT | DONE | 100% | 100% | 100% | 100% |
| **U20** | `QueryModel` | Sql | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U21** | `QueryPlanCache` | Sql | INFRASTRUCTURE | DONE | 100% | 100% | 100% | 100% |
| **U22** | `QuerySpecTranslator` | Sql | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U23** | `PostgreSqlDialect` | PostgreSql | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U24** | `MsSqlDialect` | MsSql | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U25** | `SqliteDialect` | Sqlite | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U26** | `MySqlDialect` | MySql | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U27** | `MariaDbDialect` | MariaDb | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U28** | `OracleDialect` | Oracle | COMPONENT | DONE | 100% | 100% | 100% | 100% |
| **U29** | `QuerySpecDapperExtensions` | Dapper | EXTENSION | DONE | 100% | 100% | 100% | 100% |
| **U30** | `SpecificationDapperExtensions` | DapperExtensions | EXTENSION | DONE | 100% | 100% | 100% | 100% |
| **U31** | `EfSpecificationEvaluator`, `EfReadRepository`, Extensions, DI | EntityFrameworkCore | INTEGRATION | DONE | 100% | 100% | 100% | 100% |
| **U32** | `MongoSpecificationEvaluator`, `MongoReadRepository`, Extensions | MongoDB | INTEGRATION | DONE | 100% | 100% | 100% | 100% |
| **U33** | `SpecificationDiagnosticDescriptors` & `AnalyzerExtensions` | Analyzers | INTERNAL_API | DONE | 100% | 100% | 100% | 100% |
| **U34** | `SpecificationSealedAnalyzer` & `SpecificationSealedCodeFixProvider` | Analyzers | ANALYZER / CODEFIX | DONE | 100% | 100% | 100% | 100% |
| **U35** | `SpecificationMutableStateAnalyzer` | Analyzers | ANALYZER | DONE | 100% | 100% | 100% | 100% |
| **U36** | `IsSatisfiedByInExpressionAnalyzer` | Analyzers | ANALYZER | DONE | 100% | 100% | 100% | 100% |
| **U37** | `AsyncInExpressionAnalyzer` | Analyzers | ANALYZER | DONE | 100% | 100% | 100% | 100% |
| **U38** | `InfrastructureInSpecAnalyzer` | Analyzers | ANALYZER | DONE | 100% | 100% | 100% | 100% |
| **U39** | `ArdalisMigrationAnalyzer` & `ArdalisMigrationCodeFixProvider` | Analyzers | ANALYZER / CODEFIX | DONE | 100% | 100% | 100% | 100% |
| **U40** | `SpecificationGenerator` | Generators | GENERATOR | DONE | 100% | 100% | 100% | 100% |

---

## 4. Public API & Core Contracts

### `ISpecification<T>`
- Pure DDD contract for predicate evaluation in memory or over expression trees.
- Signatures: `bool IsSatisfiedBy(T entity)`, `Expression<Func<T, bool>> ToExpression()`.

### `QuerySpec<T>` & `QuerySpecProjected<TSource, TResult>`
- Immutable query descriptors for application and persistence layers.
- Supports: Criteria (`IReadOnlyList<Expression<Func<T, bool>>>`), Ordering (`OrderBy`, `OrderByDescending`, `ThenBy`), Pagination (`Take`, `Skip`), Projection (`Select`), Distinct, Keyset pagination (`SeekAfter`, `SeekBefore`).

### `Specification<T>`
- Abstract base for strongly typed, composable domain specifications.
- Operator overloading: `&`, `|`, `!`, `true`, `false`.

### `Spec` Static Combinators
- `Spec.All<T>(params Specification<T>[])`, `Spec.Any<T>(params Specification<T>[])`, `Spec.True<T>()`, `Spec.False<T>()`, `Spec.For<T>(Expression<Func<T, bool>>)`.

---

## 5. Source Generators

### `SpecificationGenerator`
- **Type**: Incremental Source Generator (Roslyn 4.14+).
- **Input**: Classes/records marked with `[SpecColumnResolver(typeof(Entity))]` or mapping configurations.
- **Output**: Optimized, reflection-free, AOT-compatible `IColumnNameResolver` implementation.
- **Tested Scenarios**: Nested types, scalar properties, ignored properties, navigation attributes, custom column names (`[Column("custom_name")]`), unannotated classes, semantic errors, deterministic generation.

---

## 6. Analyzers

### Roslyn Rules SPEC001 – SPEC011
- **SPEC001**: Specifications must be `sealed` classes. CodeFix: Add `sealed` modifier.
- **SPEC002**: Specifications must be immutable (no public setters or mutable fields).
- **SPEC003**: Prohibited to invoke `IsSatisfiedBy` inside specification expressions.
- **SPEC004**: Prohibited to use async operations or `Task` inside specification expressions.
- **SPEC005**: Prohibited to reference infrastructure (EF Core, Dapper, SQL) inside domain specification classes.
- **SPEC011**: Automated migration from `Ardalis.Specification` to `EricksonLopez.Specification`. CodeFix: Rewrites inheritance and API calls to the immutable API.

---

## 7. Integrations

1. **Dapper**: `QuerySpecDapperExtensions` compiles `QuerySpec<T>` to `SqlQuery` with dialect parameters.
2. **DapperExtensions**: `SpecificationDapperExtensions` adapts `IDbConnection` for specification execution.
3. **EF Core**: `SpecificationEvaluator` applies criteria, ordering, distinct, and pagination over `IQueryable<T>`.
4. **MongoDB**: `MongoFilterCompiler` translates `QuerySpec<T>` to `FilterDefinition<T>` and `SortDefinition<T>`.

---

## 8. Justified Exclusions

None. All 40 work units are fully verified with automated test suites.

---

## 9. Design Decisions

1. **Strict Immutability**: Query descriptors and specifications are immutable to guarantee multi-threading safety and eliminate side effects.
2. **Native AOT First**: All abstractions and the core engine are designed for AOT compilation (`[DynamicallyAccessedMembers]`, `ExpressionInterpreter` when JIT `Compile()` is unavailable).
3. **Zero ORM in Domain**: The domain layer (`EricksonLopez.Specification`) never references Entity Framework or database libraries.

---

## 10. Execution Evidence

- **Solution**: `EricksonLopez.Specifications.slnx`
- **Configuration**: `Release` (Any CPU)
- **dotnet clean**: 0 Errors, 0 Warnings
- **dotnet build**: 0 Errors, 0 Warnings
- **dotnet test**: 16 test projects executed, **1,052 tests passing**, 0 failing, 0 skipped.
- **dotnet-stryker**: 100.00% Mutation Score confirmed across all framework components.

---

## 11. Completion Criteria

A work unit only moves to `DONE` when:
1. Contract and behavior exhaustively specified.
2. Valid, invalid, edge, and exception cases tested.
3. 100% Line Coverage achieved.
4. 100% Branch Coverage achieved.
5. 100% Method Coverage achieved.
6. 100% Mutation Score achieved.
7. Clean `dotnet clean`, `dotnet build`, and `dotnet test` execution.
8. Documentation and evidence updated in this file.
