# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records for EricksonLopez.Specification.

ADRs document significant architectural decisions — both decisions TO implement and decisions NOT to implement.

---

## Index

### Architectural ADRs (Design Decisions)

| ADR | Title | Status |
|-----|-------|--------|
| [adr-006](./adr-006-specification-queryspec-separation.md) | Specification/QuerySpec Separation | Accepted |
| [adr-008](./adr-008-expression-trees-as-internal-representation.md) | Expression Trees as Internal Representation | Accepted |
| [adr-009](./adr-009-aot-first-design.md) | AOT-First Design | Accepted |
| [adr-010](./adr-010-no-efcore-in-core.md) | No EF Core in Core Package | Accepted |
| [adr-011](./adr-011-no-dynamic-string-queries.md) | No Dynamic String-Based Queries | Accepted |
| [adr-012](./adr-012-projection-boundary.md) | Projection Boundary | Accepted |
| [adr-018](./adr-018-remove-asnotracking-splitquery-from-queryspec.md) | Remove AsNoTracking/SplitQuery from QuerySpec&lt;T&gt; | Accepted |
| [adr-019](./adr-019-expression-compilation-cache-key-strategy.md) | ExpressionCompilationCache Key Strategy (Structural Equality) | Accepted |
| [adr-020](./adr-020-source-generator-strategy.md) | Source Generator — Exclude from v1.0, Redesign for v2.0 | Accepted |
| [adr-021](./adr-021-querypancache-lru-bounded.md) | QueryPlanCache Must Be Bounded (LRU Strategy) | Accepted |
| [adr-022](./adr-022-spec-all-any-combinators.md) | Spec.All / Spec.Any Static Combinators | Accepted |
| [adr-023](./adr-023-ispecification-in-abstractions.md) | ISpecification Placement in Abstractions vs Core | Accepted |
| [adr-024](./adr-024-convertkeyselector-boxing-strategy.md) | ConvertKeySelector Boxing Strategy | Accepted |
| [adr-025](./adr-025-expressionsimplifier-integration.md) | ExpressionSimplifier Integration in CompositeSpecification | Accepted |
| [adr-026](./adr-026-osherove-test-naming-convention.md) | Osherove Test Naming Convention & Local Suppression of IDE1006 / CA1707 | Accepted |
| [adr-027](./adr-027-mariadb-and-mysql-dialect-strategy.md) | MariaDB and MySQL Native Dialect Strategy | Accepted |
| [adr-028](./adr-028-sql-infrastructure-layer-and-dialect-package-decomposition.md) | SQL Infrastructure Layer Isolation & Dialect Decomposition | Accepted |

### Rejected Feature ADRs (What NOT to Implement)

| ADR | Feature | Status |
|-----|---------|--------|
| [adr-001](./adr-001-no-write-repository.md) | No Write Repository (`IRepository<T>`) | Accepted |
| [adr-002](./adr-002-no-include-theninclude.md) | No Include/ThenInclude in Core | Accepted |
| [adr-003](./adr-003-no-dynamic-string-ordering.md) | No Dynamic String Ordering | Accepted |
| [adr-004](./adr-004-no-sat-simplification.md) | No SAT-Based Predicate Simplification | Accepted |
| [adr-005](./adr-005-no-xor-composition.md) | No XOR/NAND/NOR Composition | Accepted |
| [adr-007](./adr-007-no-fluentvalidation-integration.md) | No FluentValidation Integration | Accepted |
| [adr-013](./adr-013-no-raw-sql.md) | No Raw SQL / `WhereRaw()` | Accepted |
| [adr-014](./adr-014-no-dynamic-reflection-queries.md) | No Dynamic Reflection Queries | Accepted |
| [adr-015](./adr-015-no-groupby-aggregation-selectmany.md) | No GroupBy / Aggregation / SelectMany | Accepted |
| [adr-016](./adr-016-no-auto-generated-buildexpression.md) | No Auto-generated `BuildExpression()` | Accepted |
| [adr-017](./adr-017-no-async-specifications.md) | No Async Specifications (`IAsyncSpecification<T>`) | Accepted |

---

## ADR Format

Each ADR follows this structure:

```
Title
Status: Accepted | Proposed | Deprecated | Superseded
Date

Context
Problem
Options Considered
Decision
Why
Consequences
Rejected Alternatives
Reconsideration Criteria (if applicable)
```

---

## How to Add a New ADR

1. Create a new file: `ADR-NNN-short-title.md`
2. Fill in the standard format above
3. Add an entry to this README
4. Reference the ADR from `architecture.md` if architectural, or `roadmap.md` if a rejected feature

---

## Key Principles Driving ADR Decisions

1. **Persistence ignorance**: `Specification<T>` knows nothing about databases or ORMs
2. **DDD correctness**: Domain specs are pure predicates; query concerns are in `QuerySpec<T>`
3. **AOT-first**: Core is NativeAOT-compatible by design; JIT paths are annotated opt-ins
4. **Provider independence**: Core has zero ORM dependencies
5. **Minimalism**: No feature is added without clear architectural value
