# adr-016: No Auto-generated BuildExpression()

## Status
Accepted

## Date
2026-08-13

**Status**: Accepted  
**Date**: 2026-08-13  
**Deciders**: Erickson Lopez

---

## Context

`Specification<T>` requires consumers to implement a single abstract method:

```csharp
protected abstract Expression<Func<T, bool>> BuildExpression();
```

This is the core of the specification pattern: the developer _explicitly_ declares the
predicate. During the design of the library and the Roslyn Generators package
(`EricksonLopez.Specification.Generators`), it was considered whether a source generator
could auto-generate `BuildExpression()` from attributes or constructor parameters.

For example:

```csharp
// Hypothetical: auto-generated BuildExpression from attributes
[SpecFilter(nameof(Customer.IsActive), equals: true)]
[SpecFilter(nameof(Customer.Tier), equals: "Premium")]
public sealed partial class ActivePremiumSpec : Specification<Customer> { }
```

## Problem

Auto-generating `BuildExpression()` from attributes, constructor parameters, or
convention-based introspection would require the generator to:

1. Parse attribute arguments into expression tree fragments.
2. Compose those fragments into a valid `Expression<Func<T, bool>>`.
3. Handle all expression node types: binary, member access, method calls, closures.
4. Validate at compile time that property names and types are correct.

## Options Considered

### Option A — Attribute-driven source generator
Cons: see "Decision" rationale below.

### Option B — Fluent DSL inside generator (non-expression)
Cons: same problems; adds a secondary API surface that consumers must learn.

### Option C — Keep `BuildExpression()` as explicit developer responsibility
Pros: intentional, type-safe, IDE-navigable, debuggable, AOT-safe, DDD-aligned.

## Decision

**Auto-generation of `BuildExpression()` will never be implemented**, whether via
source generators, T4 templates, attributes, or any other code-generation mechanism.

## Why

1. **Destroys type safety**: Attribute-based property references (`nameof`-like) can be
   verified by a generator at compile time for simple member access, but not for complex
   expressions (`c => c.Orders.Any(o => o.Total > 1000)`). The generator would need to
   handle arbitrary expression composition, which is essentially reimplementing the
   C# compiler's expression tree lowering.

2. **Destroys DDD intent**: In DDD, a specification is a _named business rule_. The
   explicit implementation of `BuildExpression()` documents what that rule means —
   it is a first-class artifact of the domain model. Auto-generation replaces this
   meaningful implementation with generated boilerplate that is harder to understand,
   review, and test.

3. **Debuggability loss**: When `BuildExpression()` is auto-generated, setting a breakpoint
   inside it is either impossible or requires navigating generated code. Developers lose the
   ability to step through the predicate logic in the IDE.

4. **Composition incompatibility**: `BuildExpression()` allows arbitrary expression tree
   composition via `And()`, `Or()`, `Not()`. Auto-generation of a static attribute-driven
   predicate cannot express this composition. The moment a consumer needs composition, they
   must abandon the generated code.

5. **AOT and trimmer implications**: A generator that builds expressions from attribute
   metadata must either embed the member access pattern (safe) or use reflection-based
   lookup at runtime (AOT-incompatible). The trivial safe cases are exactly those where
   writing `BuildExpression()` manually is trivial too.

6. **Source generators add fragility**: Generated code that must stay in sync with
   domain model changes creates a class of bugs (stale generated code) that are hard to
   detect. Explicit code is always in sync because the developer writes it.

## Consequences

- Consumers always implement `BuildExpression()` explicitly. This is a one-line to
  five-line method for typical specifications.
- The Roslyn Generators package (`EricksonLopez.Specification.Generators`) focuses on
  _structural validation_ (SPEC001-010) and _static catalog generation_ (v1.2+), not
  on auto-generating predicate logic.
- SPEC001 (sealed/abstract analyzer) and SPEC002 (mutable state analyzer) guide consumers
  toward correct implementations; they do not replace explicit implementation.

## Rejected Alternatives

- `[SpecFilter]` attribute-driven generator — rejected; see rationale above.
- Fluent DSL generator (`SpecBuilder<T>.Where(...).Build()`) — rejected; duplicate of
  `QuerySpec<T>` which already serves this purpose at the query layer.
- Record-based specification (auto-derived from record properties) — rejected; records
  don't express predicate logic; they express value equality.

## Reconsideration Criteria

Only reconsider if C# gains language-level support for compile-time expression tree
construction from declarative attributes — analogous to how `[GeneratedRegex]` generates
regex at compile time. Until then, explicit `BuildExpression()` is the correct approach.
