# adr-028: SQL Infrastructure Layer Isolation and Satellite Dialect Package Decomposition

## Status

Accepted

## Date

2026-08-19

## Context

`EricksonLopez.Specification` is designed under strict Domain-Driven Design (DDD) and Clean Architecture principles. 

A central question in the architecture of specification libraries is where SQL translation abstractions (`ISqlDialect`, `QueryModel`, `QuerySpecTranslator`, `QueryPlanCache`) and concrete engine dialects (`MsSqlDialect`, `PostgreSqlDialect`, `MySqlDialect`, `MariaDbDialect`, `SqliteDialect`, `OracleDialect`) should reside.

## Problem

Placing SQL translation constructs in the Core (`EricksonLopez.Specification`) or bundling specific database engine dialects inside the agnostic SQL engine package (`EricksonLopez.Specification.Sql`) creates severe architectural violations:

1. **Violation of Persistence Ignorance**: The Domain layer (`Specification<T>`, `Spec`, `ExpressionComposer`, `ExpressionInterpreter`) must have zero knowledge of databases, tables, columns, dialects, or SQL. Polluting the Core with SQL abstractions forces non-relational consumers (e.g. in-memory business rule validation, Document DBs, Event Sourcing, or LINQ/EF Core via `IQueryable.Apply`) to take unwanted dependencies on SQL infrastructure.
2. **Native AOT and Trimming Integrity**: The Core is 100% Native AOT-compatible with zero reflection warnings. In contrast, `QuerySpecTranslator<T>` extracts column mappings and closure values via expression reflection, requiring explicit `[RequiresUnreferencedCode]` trimming annotations. Mixing these concerns degrades Core AOT guarantees.
3. **Dialect Asymmetry and Coupling**: Embedding specific database dialects (e.g., `MsSqlDialect`) in `EricksonLopez.Specification.Sql` forces all other dialect packages (`PostgreSql`, `MySql`, `Sqlite`, `Oracle`) and `Dapper` to depend on an assembly containing SQL Server-specific code.

## Options Considered

### Option A: Place `ISqlDialect` and `QuerySpecTranslator` directly in Core (`EricksonLopez.Specification`) — Rejected
- **Drawback:** Destroys persistence ignorance in the domain model; pollutes the Core with relational SQL concepts; compromises trimming boundaries.

### Option B: Rename `EricksonLopez.Specification.Sql` to `EricksonLopez.Specification.MsSql` and keep the AST inside — Rejected
- **Drawback:** Forces `PostgreSql`, `MySql`, `Sqlite`, `Oracle`, and `Dapper` packages to reference a package named `MsSql`, creating misleading dependencies and inverted architectural boundaries.

### Option C: Isolate Agnostic AST & Engine in `EricksonLopez.Specification.Sql` and Decompose All Database Engines into Dedicated Satellite Packages — Accepted
- `EricksonLopez.Specification.Sql` serves strictly as the engine-agnostic SQL infrastructure layer (AST nodes, `ISqlDialect` contract, `QuerySpecTranslator<T>`, `QueryPlanCache`, column resolvers).
- Each relational database engine lives in its own dedicated, symmetric satellite package:
  - `EricksonLopez.Specification.MsSql` (`MsSqlDialect`)
  - `EricksonLopez.Specification.PostgreSql` (`PostgreSqlDialect`)
  - `EricksonLopez.Specification.MySql` (`MySqlDialect`)
  - `EricksonLopez.Specification.MariaDb` (`MariaDbDialect`)
  - `EricksonLopez.Specification.Sqlite` (`SqliteDialect`)
  - `EricksonLopez.Specification.Oracle` (`OracleDialect`)

## Decision

Adopt **Option C**:
1. Keep `EricksonLopez.Specification` purely focused on Domain predicates and in-memory evaluation with zero SQL dependencies.
2. Keep `EricksonLopez.Specification.Sql` strictly as the agnostic SQL infrastructure engine and contract layer.
3. Extract `MsSqlDialect` from `EricksonLopez.Specification.Sql` into its own package `EricksonLopez.Specification.MsSql`.
4. Introduce `EricksonLopez.Specification.Oracle` for native Oracle Database translation.

## Consequences

### Positive
- **Architectural Purity**: Full adherence to Clean Architecture, DDD, and Persistence Ignorance.
- **Symmetry**: All database engines have identical, predictable package naming and dependency models (`Core -> Abstractions <- Sql <- [EnginePackage]`).
- **Granular Deployment**: Consumers only reference the exact database driver/dialect package required by their application.
- **AOT Boundary Isolation**: Trimming annotations remain confined to the SQL infrastructure layer.

### Negative
- Additional NuGet packages to manage and version across the solution, mitigated by centralized package management (`Directory.Packages.props`).
