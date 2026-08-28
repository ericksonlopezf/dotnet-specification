# STRATEGIC ROADMAP — EricksonLopez.Specification

> **Version**: 2.0 — Post-Audit Architecture Execution (2026-08-14)  
> **Strategy**: Pure Domain Predicates · Zero-ORM SQL Engine · Native AOT-First · Compile-Time Governance  
> **Repository Status**: 1,052 tests passing · 0 warnings (`--warnaserror`) · 11 Roslyn Analyzers · 6 SQL Dialects (PostgreSQL, SQL Server, SQLite, MySQL, MariaDB, Oracle)

---

## 1. Value Proposition & Market Differentiators

`EricksonLopez.Specification` fills an unmet need in the .NET ecosystem:

| Market Capability | Ardalis.Specification | LinqKit | EricksonLopez.Specification |
|---|---|---|---|
| **DDD Domain Purity** (Predicate-only Specification) | ❌ Mixes EF Core ORM | N/A (Utility) | ✅ **Pure Domain Contract** |
| **Direct SQL Translation for Dapper** | ❌ EF Core only | ❌ None | ✅ **PostgreSQL, SQL Server, SQLite, MySQL, MariaDB, Oracle** |
| **Native AOT In-Memory Interpretation** | ❌ JIT dependent | ❌ JIT dependent | ✅ **`ExpressionInterpreter`** |
| **Compile-Time Architectural Analyzers** | ❌ None | ❌ None | ✅ **11 Roslyn Analyzers (`SPEC001-011`)** |
| **Immutable Query Intent Descriptors** | ❌ Mutable builder | N/A | ✅ **Sealed records (`QuerySpec<T>`)** |
| **Bounded Plan & Delegate Caching** | ❌ Basic / Unbounded | ❌ None | ✅ **LRU 512 + Structural Equality** |

---

## 2. Release Status & Accomplishments (v1.0 Ready)

All foundational engineering, correctness fixes, and performance features are fully implemented and verified:

| Milestone / Component | Status | Implementation Details |
|---|---|---|
| **Expression Trees as Core Model** | ✅ Completed | `Expression<Func<T, bool>>` with parameter rebinding (No `Invoke`). |
| **Immutability & Separation** | ✅ Completed | `Specification<T>` in Domain; `QuerySpec<T>` in Application layer. |
| **Structural Equality Engine** | ✅ Completed | `ExpressionEqualityComparer` and `ExpressionHasher` deep tree comparison. |
| **Bounded Plan Cache** | ✅ Completed | `QueryPlanCache` with LRU eviction (512 capacity) for SQL hot paths. |
| **Compilation Cache** | ✅ Completed | `ExpressionCompilationCache` with structural equality keys (Zero collisions). |
| **Expression Simplification** | ✅ Completed | `ExpressionSimplifier` auto-simplifies boolean identities in `CompositeSpecification`. |
| **Ergonomic Combinators** | ✅ Completed | `Spec.All<T>()` and `Spec.Any<T>()` with span-based bulk composition. |
| **AOT Debug Formatter** | ✅ Completed | `ToDebugString()` on `Specification<T>` and `IExpressionSpecification<T>`. |
| **Roslyn Analyzers Suite** | ✅ Completed | SPEC001 through SPEC011 enforcing sealed specs, immutability, and safety. |
| **SQL Translation & Dialects** | ✅ Completed | PostgreSQL (`ILIKE`, `$n`), SQL Server (`OFFSET/FETCH`), SQLite, MySQL/MariaDB, Oracle. |
| **Dapper Extensions** | ✅ Completed | `QueryAsync`, `QuerySingleAsync`, `ExecuteScalarAsync` over `IDbConnection`. |
| **Architecture Decision Records** | ✅ Completed | 28 ADRs (adr-001 through adr-028) documenting all choices and rejections. |

---

## 3. Multi-Phase Strategic Roadmap

### Phase 0 — Pre-Release Polish & Release Hygiene (Immediate)

* [x] **TASK-R001**: Synchronize `roadmap.md` and `roadmap-tasks.md` with true codebase state.
* [x] **TASK-R002**: Fix `QueryPlanCache` cache key safety (isolate single-criterion pure filters; exclude distinct/ordering conflicts).
* [x] **TASK-R003**: Integrate `ExpressionSimplifier` into `CompositeSpecification` for constant identity neutralization (`True && A = A`).
* [x] **TASK-R004**: Update `README.md` to remove inaccurate Source Generator claims and qualify AOT interpreter guidance.
* [x] **TASK-R005**: Update `docs/adr/README.md` and `adr-index.md` to index all ADRs (adr-001 through adr-028).
* [x] **TASK-R006**: Document benchmark methodology and execution profiles in `docs/benchmarks.md`.
* [x] **TASK-R007**: Configure CI and packaging to exclude experimental `Generators` project from v1.0 NuGet push (`<IsPackable>false</IsPackable>`).
  > **Note**: As of the current `publish.yml`, `EricksonLopez.Specification.Generators` **is included** in the publish step. This task's original intent (exclusion) was superseded. See adr-020 for context.
* [x] **TASK-R008**: Finalize NuGet package metadata (icons, tags, documentation links, descriptions across all `.csproj` files).

---

### Phase 1 — Developer Experience & Ecosystem Adoption (0–3 Months)

* [x] **TASK-R009**: Implement AOT-safe `ToDebugString()` for logging and debugging predicates.
* [x] **TASK-R010**: Implement `Spec.All<T>()` and `Spec.Any<T>()` static combinators.
* [x] **TASK-R011**: Expand `docs/aot.md` with complete matrix of supported node types and `[DynamicallyAccessedMembers]` guidance.
* [x] **TASK-R012**: Implement `EricksonLopez.Specification.MySql` dialect adapter (`MySqlDialect`).
* [x] **TASK-R013**: Validate `SPEC001` - `SPEC011` Roslyn analyzers and CodeFix providers.

---

### Phase 2 — Advanced Infrastructure & Tooling (3–6 Months)

* [x] **TASK-R014**: **Source Generator v2.0**:
  * Generate compile-time, zero-reflection `IColumnNameResolver` implementations from entity models (`SpecColumnResolverAttribute`).
  * Generate strongly-typed ordering and column mappings.
* [x] **TASK-R015**: **Property-Based Testing**:
  * Implement FsCheck / CsCheck suite verifying boolean algebra invariants across randomized expression trees.
* [x] **TASK-R016**: **Integration Test Matrix**:
  * Expand Testcontainers integration test suite covering real PostgreSQL, SQL Server, and SQLite instances.

---

### Phase 3 — Long-Term Evolution & Advanced SQL Capabilities

* [x] **TASK-R017**: Keyset / Cursor Pagination extensions for `QuerySpec<T>` (`SeekAfter`, `SeekBefore`, `WithCursor`).
* [x] **TASK-R018**: PostgreSQL advanced type support (`to_tsvector` / `plainto_tsquery` full-text search, `int4range` range operators, `Between` operator across all 4 SQL dialects).
* [x] **TASK-R019**: Automated Roslyn Migration Analyzer (`SPEC011`) and CodeFix providers for migrating from Ardalis.Specification to EricksonLopez.Specification.

---

## 4. What We Will NEVER Build (Architectural Boundaries)

To preserve DDD purity and zero-bloat performance, the following are permanently rejected by ADR:

* ❌ **ORM & Tracking Leaks**: `Include`, `ThenInclude`, `AsNoTracking`, `AsSplitQuery` (adr-002, adr-010, adr-018).
* ❌ **Dynamic String Queries & Reflection**: `OrderBy("String")`, `Where("String")` (adr-003, adr-011, adr-014).
* ❌ **Write Repository & UnitOfWork**: `Add`, `Update`, `Delete`, `Commit` (adr-001).
* ❌ **Async Specifications in Domain**: `Task<bool> IsSatisfiedByAsync()` (adr-017).
* ❌ **Raw SQL Injection**: `WhereRaw("sql")` (adr-013).
* ❌ **Aggregations & Analytical Queries**: `GroupBy`, `SelectMany`, `Sum` (adr-015).
* ❌ **XOR Composition & SAT Solvers**: (adr-004, adr-005).
