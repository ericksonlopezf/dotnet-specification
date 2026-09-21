# adr-011: No Runtime Reflection-Based Dynamic Queries

## Status
Rejected

## Date
2026-08-13

**Status**: Accepted
**Date**: 2026-08-13

## Context

Some libraries provide string-based filtering and sorting APIs for API-driven scenarios:
- OrderBy(""Name"")
- Filter(""Name"", ""contains"", ""John"")

These are patterns popularized by Sieve, Gridify, and Dynamic LINQ.

## Problem

Should EricksonLopez.Specification support dynamic string-based query building?

## Decision

NO. String-based dynamic filtering and sorting are rejected entirely.

## Why

1. **Type safety**: String-based queries are runtime errors waiting to happen (misspelled property names)
2. **AOT incompatibility**: Resolving property names via reflection at runtime is trimming-hostile
3. **Security risk**: String-based sorting can expose injection attack surface in SQL translation
4. **Wrong layer**: Dynamic query string parsing from HTTP requests belongs at the API layer (Sieve, Gridify)
5. **Better alternative exists**: Strongly-typed OrderBy<TKey>(expr) catches errors at compile time

## Consequences

- Developers who need to sort/filter from API query strings must use a dedicated tool (Sieve, Gridify)
  at the API layer, then construct typed specifications from the parsed values
- The library remains compile-time safe and trimming-friendly throughout

## Rejected Alternative

- OrderBy(string propertyName) with PropertyInfo cache: trimming risk cannot be eliminated cleanly,
  and a misspelled property name fails at runtime instead of compile time.
