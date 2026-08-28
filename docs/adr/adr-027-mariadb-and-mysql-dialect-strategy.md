# adr-027: MariaDB and MySQL Native Dialect Strategy

## Status

Accepted

## Date

2026-08-19

## Context

The SQL translation layer of `EricksonLopez.Specification` translates agnostic `QueryModel` ASTs into engine-specific parameterized SQL via the `ISqlDialect` contract.

MySQL and MariaDB are two of the most widely adopted open-source relational database management systems in enterprise and cloud environments. While they share a historical lineage and common syntax foundations, their distinct identities require clear architectural separation and explicit dialect identification for telemetry, logging, and caching (`QueryPlanCache`).

## Problem

A generic SQL renderer cannot generate optimal, safe, and correct SQL for MySQL and MariaDB due to specific syntactic requirements:
1. **Identifier Quoting**: MySQL and MariaDB use backticks (`` `identifier` ``) rather than standard SQL double quotes (`"identifier"`) or SQL Server square brackets (`[identifier]`). Escaping existing backticks within identifiers requires doubling them (`` `` ``).
2. **Parameter Formatting**: Parameters are named and prefixed with `@` (e.g. `@paramName`).
3. **Collection Membership (`IN` predicates)**: MySQL/MariaDB do not support array parameters or table-valued parameters natively in query strings. Collection parameters passed to `IN` clauses must be expanded into individual named parameters (`@p_0, @p_1, ...`). Additionally, empty collections (`IN ()`) produce invalid SQL syntax and must be safely transformed into boolean identities (`1 = 0` for `IN`, `1 = 1` for `NOT IN`).
4. **Pagination**: Pagination relies on `LIMIT n OFFSET m`. In cases where `Skip` is specified without `Take`, MySQL/MariaDB require an explicit `LIMIT` bound; the maximum 64-bit unsigned integer (`18446744073709551615`) must be supplied.
5. **Full-Text Matching**: Full-text searches require `MATCH(\`column\`) AGAINST(@param IN NATURAL LANGUAGE MODE)`.
6. **Engine Differentiation**: Providing both `MySqlDialect` and `MariaDbDialect` allows consuming applications and observability frameworks to accurately record engine identities (`DialectName => "MySQL"` vs `DialectName => "MariaDB"`).

## Options Considered

### Option A: Use standard ANSI SQL formatting with double quotes — Rejected
- **Drawback:** MySQL and MariaDB only accept double quotes for identifiers when `ANSI_QUOTES` SQL mode is explicitly enabled on the server connection. Relying on this assumption causes runtime syntax errors on default configurations.

### Option B: A single `MySqlDialect` without explicit `MariaDbDialect` — Rejected
- **Drawback:** Forces MariaDB users to configure `MySqlDialect.Default`, polluting telemetry metrics, query plan caches, and diagnostics with an inaccurate database engine name.

### Option C: Dedicated `MySqlDialect` in `EricksonLopez.Specification.MySql` and `MariaDbDialect` in `EricksonLopez.Specification.MariaDb` with Native Syntax Handling — Accepted
- Provide standalone, high-performance implementations of `ISqlDialect` in dedicated symmetric packages for both MySQL and MariaDB.
- Native support for backtick quoting, `@` parameter prefix, `LIMIT / OFFSET` pagination, full-text `MATCH ... AGAINST`, range/between predicates, and robust `IN` clause expansion with empty collection guards.
- Packages have zero external dependencies and are 100% Native AOT compatible.

## Decision

Adopt **Option C**:
1. Implement `MySqlDialect` (`DialectName => "MySQL"`) in `EricksonLopez.Specification.MySql` and `MariaDbDialect` (`DialectName => "MariaDB"`) in `EricksonLopez.Specification.MariaDb`.
2. Ensure both dialects implement native identifier quoting with backticks, escaping nested backticks as `` `` ``.
3. Expand `IEnumerable` parameter values into inline named parameters (`@p_0, @p_1, ...`) during query rendering.
4. Render empty `IN` clauses as `1 = 0` and empty `NOT IN` clauses as `1 = 1`.
5. Support `LIMIT @_take`, `OFFSET @_skip`, and `LIMIT 18446744073709551615 OFFSET @_skip` when only `Skip` is provided.
6. Support `MATCH(\`col\`) AGAINST(@param IN NATURAL LANGUAGE MODE)` for full-text nodes.

## Consequences

### Positive
- **Correctness**: Generated SQL adheres strictly to MySQL and MariaDB grammar rules without requiring special server SQL modes.
- **Security & Safety**: Parameter expansion prevents SQL injection and ensures type safety.
- **AOT & Zero Allocations**: Self-contained implementations without heavy dependencies or reflection.
- **Observability**: Clear dialect name distinction between MySQL and MariaDB.

### Negative
- Query parameter dictionaries must be expanded for collection parameters, adding minor allocation overhead during collection expansion (unavoidable without engine-level array parameter support).
