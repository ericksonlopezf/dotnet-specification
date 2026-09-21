# adr-004: No SAT-Based Predicate Simplification

## Status
Accepted

## Date
2026-08-12

**Status**: Accepted  
**Date**: 2026-08-12  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: Scope / Academic Features

---

## Context

During the design of `ExpressionSimplifier`, it was evaluated whether the library should implement boolean simplification based on SAT (Satisfiability solvers) or BDD (Binary Decision Diagrams) algorithms, which would enable simplifications such as:

- `(A && B) || (A && !B)` → `A`
- `A && (A || B)` → `A`
- Tautology detection: `A || !A` → `true`
- Contradiction detection: `A && !A` → `false`

## Decision

**REJECTED. `ExpressionSimplifier` covers only constant folding and basic algebraic simplifications. SAT/BDD will not be implemented.**

## Rationale

1. **Extreme implementation cost vs. minimal benefit.** A robust SAT solver over .NET expression trees requires: NNF conversion, Tseitin transformation, DPLL/CDCL algorithm, and back-conversion to lambdas. Months of work for the 0.01% of use cases.

2. **The existing `ExpressionSimplifier` covers 99% of practical cases.** Constant folding (`true && x` → `x`, `false || x` → `x`) solves the cases that appear in real specification composition.

3. **Unacceptable regression risk.** An incorrect expression transformation can silently change the behavior of a critical business rule. Without formal correctness proofs, the risk outweighs the benefit.

4. **Incompatible with NativeAOT.** SAT algorithms in .NET typically require unpredictable heap allocations and possibly reflection for expression tree introspection. Not aligned with the AOT profile.

5. **Academically interesting, commercially irrelevant.** None of the ~10 competitors evaluated have SAT simplification. There is no documented demand in GitHub issues, StackOverflow, or any other channel.

## Consequences

- **Positive**: No added complexity. No risk of incorrect transformation. High maintainability.
- **Negative**: Very complex specifications with redundant predicates will not be automatically simplified beyond constant folding.
- **Mitigation**: In practice, well-designed specifications (one per business rule) do not produce redundancies requiring SAT.

## Scope of the Existing ExpressionSimplifier

The implemented `ExpressionSimplifier` covers:
- `true && X` → `X`
- `false && X` → `false`
- `X && true` → `X`
- `true || X` → `true`
- `false || X` → `X`
- `!true` → `false`, `!false` → `true`
- Evaluation of constants in simple comparisons

This scope is sufficient for all real specification composition.
