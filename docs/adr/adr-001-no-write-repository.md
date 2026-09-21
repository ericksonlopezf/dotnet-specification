# adr-001: No Write Repository (`IRepository<T>`)

## Status
Accepted

## Date
2026-08-12

**Status**: Accepted  
**Date**: 2026-08-12  
**Deciders**: EricksonLopez.Specification architecture audit  
**Category**: Scope / Architectural Boundary

---

## Context

During the competitive audit, it was evaluated whether the library should include a contract for write repositories (`IRepository<T>`, `Add`, `Update`, `Delete`, `SaveChangesAsync`), following the pattern of Ardalis.Specification which includes them in `EfRepository<T>`.

100% of competitor libraries that include write repositories (Ardalis being the primary example) ended up becoming complete repository frameworks, not Specification Pattern libraries.

## Decision

**REJECTED. EricksonLopez.Specification will never implement write repositories.**

## Rationale

1. **Not the responsibility of the Specification Pattern.** The Specification Pattern defines domain predicates. Writes are state operations. Mixing them violates SRP and SoC.

2. **Documented anti-pattern in Ardalis.** Ardalis chose to include writes. The result is that `EfRepository<T>` inherits ~15 methods with no relationship to specifications. It is a God Library anti-pattern.

3. **Incompatible with the AOT+Dapper differentiator.** Write repositories in Dapper require a completely different design from EF Core. Any shared abstraction would be a leak at the lowest common denominator.

4. **Destroys the minimalist philosophy.** Every dependency added increases coupling in the consuming project. Without writes: the package depends only on `System.Linq.Expressions` and BCL primitives.

5. **Ardalis cannot copy our architecture.** With 18.8M downloads, any breaking change in their write repositories would destroy their ecosystem. This is a permanent structural advantage.

## Consequences

- **Positive**: Minimalist library, zero ORM coupling, compatible with any persistence framework, DDD-correct.
- **Negative**: Users migrating from Ardalis must implement their own write repositories. Documented in `docs/migration-from-ardalis.md`.
- **Mitigation**: The cookbook (`docs/cookbook.md`) documents the complete pattern for implementing `IReadRepository<T>` with Dapper and EF Core.

## Documented Alternative

```csharp
// In the consuming project's infrastructure layer:
public sealed class CustomerWriteRepository : ICustomerWriteRepository
{
    private readonly IDbConnection _connection;

    public Task AddAsync(Customer customer, CancellationToken ct = default)
        => _connection.ExecuteAsync("INSERT INTO customers ...", customer);

    public Task UpdateAsync(Customer customer, CancellationToken ct = default)
        => _connection.ExecuteAsync("UPDATE customers SET ...", customer);
}
```
