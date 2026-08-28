# adr-014: No Dynamic Reflection / Runtime-String Queries

**Status**: Accepted  
**Date**: 2026-08-13  
**Deciders**: Erickson Lopez

---

## Context

Libraries like Sieve, Gridify, and OData enable dynamic query building from external strings
(e.g., HTTP query parameters: `?filters=Name@=foo,IsActive==true&sorts=Name`). This is a
legitimate and valuable pattern for API pagination and filtering endpoints. The question is
whether `QuerySpec<T>` should support this via reflection-based dynamic query construction.

## Problem

Supporting dynamic, reflection-based query building (e.g., `OrderBy("PropertyName")`,
`Where("PropertyName", "operator", value)`) would require:

- Runtime reflection to resolve property names → `PropertyInfo.GetValue()`
- String-to-expression compilation at runtime
- Runtime type checking and coercion

These requirements are fundamentally incompatible with the library's core design constraints.

## Options Considered

### Option A — Add `WhereField(string fieldName, ComparisonOperator op, object value)`
Pros: convenient for API/filter scenarios.  
Cons: see "Decision" rationale below.

### Option B — Add `OrderBy(string propertyName)` (string-based ordering)
Pros: matches Ardalis's API; familiar to consumers.  
Cons: type-unsafe; see adr-003 for full rationale.

### Option C — Recommend external libraries (Sieve, Gridify) at the API/infrastructure layer
Pros: specialized tools; composable with `QuerySpec<T>` at the repository layer.  
Cons: consumers must integrate two libraries.

## Decision

**Dynamic, reflection-based query building will never be part of this library.**
Consumers who need dynamic API filtering should use Sieve, Gridify, or OData at the
API/infrastructure layer, then pass a concrete `QuerySpec<T>` to the repository.

## Why

1. **NativeAOT incompatibility**: Reflection (`PropertyInfo.GetValue`, `Type.GetProperty`,
   `Expression.Property` resolved from strings at runtime) requires metadata that the AOT
   trimmer removes. This is the primary differentiator of this library — AOT-first design
   (see adr-009) — and dynamic reflection destroys it.

2. **Type unsafety**: String-based property names are not verified by the compiler. Renames
   cause silent runtime failures. This library's value is compile-time guarantees via
   expression trees, not runtime string evaluation.

3. **SQL injection surface**: String field names passed to SQL renderers — even with
   identifier quoting — can be vectors for injection if not carefully sanitized. The
   expression-tree path provably cannot produce injection.

4. **Wrong layer**: Dynamic query building belongs in the API/infrastructure layer, not in
   the domain specification layer. Domain specs express _domain invariants_; dynamic
   filters express _user preferences_. Mixing these violates Clean Architecture.

5. **Existing tools are better**: Sieve (4M+ NuGet downloads) and Gridify are purpose-built
   for this. They handle pagination, sorting, and filtering with full test suites. Building
   a competing implementation here adds maintenance burden with no architectural benefit.

## Consequences

- `QuerySpec<T>` remains fully NativeAOT-compatible.
- Consumers building API filtering endpoints use Sieve/Gridify at the controller/application
  layer and convert to `QuerySpec<T>` before calling repositories.
- The integration guide (`docs/cookbook.md`) documents the recommended composition pattern.

## Rejected Alternatives

- `OrderBy(string propertyName)` — covered in adr-003.
- `Where(string field, object value)` — rejected; type-unsafe, AOT-incompatible, injection risk.
- Source-generated dynamic filters — rejected; adds source generator complexity without
  clear benefit over existing typed expression-tree approach.

## Reconsideration Criteria

Only reconsider if a compile-time-safe mechanism for dynamic field selection exists that is
NativeAOT-compatible and does not require reflection metadata. No such mechanism is standard
in .NET today.
