# Architectural Decision Record: reject-003
## Rejection of EF Core / IQueryable Tight Coupling in Specification Abstractions

### Status
**REJECTED (Permanent Directorial Invariant)**

### Context
Legacy specification patterns often define specifications as wrapper types around `IQueryable<T> => IQueryable<T>` expressions, tightly binding the specification contract to Entity Framework Core or LINQ providers.

### Decision
Permanently rejected. `EricksonLopez.Specification.Abstractions` defines pure, vendor-neutral `QuerySpec<T>` descriptors with explicit expressions, sort clauses, and cursor parameters. Provider adapters (`Specification.EntityFrameworkCore`, `Specification.Dapper`, `Specification.MongoDB`, `Specification.Sql`) translate pure descriptors into native queries.

### Consequences
- Clean Architecture compliance: Domain layers consume `QuerySpec<T>` without referencing ORMs.
- Polyglot persistence: The same specification can target EF Core, Dapper, SQL Server, PostgreSQL, or MongoDB.
- Native AOT trimming safety without hidden runtime reflection.
