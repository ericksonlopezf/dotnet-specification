# adr-012: Projection Boundary

**Status**: Accepted
**Date**: 2026-08-13

## Context

Projection maps entity objects (T) to DTOs (TResult). The question is where in the architecture
this mapping should be expressed.

## Problem

Should Specification<T> know about DTOs? Should Specification<T, TResult> be a first-class type?

## Decision

Projection lives in QuerySpec<T, TResult>, NOT in Specification<T>.

`csharp
// WRONG -- domain spec knows about application DTO
public sealed class ActiveOrderSpec : Specification<Order, OrderDto> { }

// CORRECT -- projection is a query descriptor concern
var query = QuerySpec<Order, OrderDto>.Empty
    .Where(new ActiveOrderSpec())
    .Select(o => new OrderDto(o.Id, o.Total));
`

## Why

- Specification<T> is a domain object. It must not know about application types (DTOs).
- Knowing about OrderDto from Order.Domain would create a dependency from domain to application layer -- an architectural inversion.
- Projection is a query/mapping concern, not a domain rule concern.
- QuerySpec<T, TResult> carries both the filter criteria and the projection selector -- the correct level.

## Consequences

- Domain specifications remain pure predicates
- DTO types are referenced only in QuerySpec<T, TResult> (application layer)
- Projection selector is Expression<Func<T, TResult>> -- translatable to SQL SELECT columns (via EF Core) or mappable via Dapper

## Rejected Alternative

- Specification<T, TResult> with projection in the domain spec: creates domain→application dependency.
