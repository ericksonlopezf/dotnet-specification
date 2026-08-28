# adr-009: AOT-First Design

**Status**: Accepted
**Date**: 2026-08-13

## Context

.NET NativeAOT is growing with each runtime release. Serverless, MAUI, and edge workloads
increasingly require AOT-compatible code. Expression.Compile() -- the standard way to
evaluate expression trees in-memory -- is incompatible with NativeAOT.

No existing specification library supports NativeAOT. This is a structural market gap.

## Problem

How do we provide in-memory evaluation of specifications (IsSatisfiedBy) in NativeAOT environments?

## Options Considered

1. Compile-only path: Expression.Compile() as the only evaluation mechanism
2. AOT-first with interpreted fallback: ExpressionInterpreter as default, compiled as JIT opt-in
3. Skip in-memory evaluation entirely: only support IQueryable<T> path

## Decision

AOT-first design with dual evaluation paths:

- DEFAULT: ExpressionInterpreter.Evaluate() -- interpreted tree walk, no Expression.Compile()
  - Works in NativeAOT
  - 5-20x slower than compiled delegate
  - Correct and safe

- OPT-IN (JIT only): ExpressionCompilationCache.GetOrCompile()
  - Annotated [RequiresDynamicCode]
  - Linker error at publish -p:PublishAot=true if used in AOT context
  - Near-native delegate performance after cache hit

All dynamic code paths are annotated with [RequiresDynamicCode] and [RequiresUnreferencedCode].
All trimming-sensitive reflection paths are annotated with [DynamicallyAccessedMembers].

## Why

- NativeAOT is a structural advantage that cannot be quickly replicated by competitors
  (requires redesigning the entire evaluation model)
- Ardalis.Specification cannot add AOT without breaking 18.8M existing users
- LinqKit is AOT-incompatible by fundamental design (ExpressionExpander)
- Being first in this space creates a durable structural differentiator

## Consequences

- ExpressionInterpreter must cover all expression node types used in real specifications
- 5-20x interpreted overhead must be documented clearly and honestly
- ExpressionCompilationCache is a JIT-only opt-in, clearly marked
- NativeAOT sample project required as a release gate (validates the claim)

## Rejected Alternative

- Compile-only path: breaks NativeAOT, the primary differentiator
- Skip IsSatisfiedBy: domain layer would lose in-memory evaluation capability
