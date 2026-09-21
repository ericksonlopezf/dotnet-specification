# adr-008: Expression Trees as Internal Representation

## Status
Accepted

## Date
2026-08-13

**Status**: Accepted
**Date**: 2026-08-13

## Context

A Specification<T> must encode a business rule in a form that can be:
- Evaluated in-memory without infrastructure (domain rule checking)
- Translated to IQueryable<T> for LINQ providers (EF Core)
- Translated to parameterized SQL for Dapper
- Inspected and composed without executing
- Hashed for caching
- Debugged with a readable representation

## Problem

Which internal representation should encode the predicate?

## Options Considered

1. Expression<Func<T, bool>> — expression tree (BCL)
2. Func<T, bool> — compiled delegate
3. Custom AST (Specification AST nodes)
4. IQueryable<T> — LINQ query
5. String predicates

## Decision

Expression<Func<T, bool>> is the canonical internal representation.

## Why

- Inspectable via ExpressionVisitor (required for SQL translation and hashing)
- Translatable to IQueryable<T> without changes (EF Core, any LINQ provider)
- Translatable to SQL via QuerySpecTranslator<T>
- Composable without Expression.Invoke via ParameterReplacer
- AOT-compatible (expression tree construction is BCL, no dynamic code)
- No ORM dependency
- Structural hashing possible (ExpressionHasher)
- Compiled on demand (ExpressionCompilationCache) for JIT performance

## Consequences

- Expression.Compile() is needed for maximum in-memory evaluation performance (JIT-only path)
- ExpressionInterpreter is needed for AOT-safe in-memory evaluation
- QuerySpecTranslator<T> uses reflection for closure extraction (annotated)

## Rejected Alternatives

- Func<T, bool>: cannot be inspected, translated, or composed without compilation
- Custom AST: expression trees ARE a general-purpose AST; double AST adds complexity with no benefit for the predicate concern
- IQueryable<T>: ORM coupling, forces provider discovery
- String predicates: type-unsafe, injection risk, no compile-time validation
