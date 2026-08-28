# Migration Guide

## From `Ardalis.Specification` to `EricksonLopez.Specification`

If you were using traditional libraries purely focused on Entity Framework Core, the transition will require re-thinking the architectural purpose of your rules.

### 1. Replacing `ISpecification<T>` (Ardalis)
In Ardalis, the Specification mixed pure conditional rules (`Where`), along with Infrastructure logic (`Includes`, `AsNoTracking`, `OrderBy`).

In **our library**, these responsibilities are strictly segregated:
- Business rules (the *what*) go in `Specification<T>`.
- Read necessities (the *how it is extracted*) go in `QuerySpec<T>`.

**Ardalis:**
```csharp
public class ActiveUsersSpec : Specification<User>
{
    public ActiveUsersSpec()
    {
        Query.Where(u => u.IsActive)
             .OrderBy(u => u.Name)
             .Include(u => u.Profile);
    }
}
```

**EricksonLopez.Specification:**
```csharp
// 1. Domain: Pure rule
public class ActiveUserSpec : Specification<User>
{
    protected override Expression<Func<User, bool>> BuildExpression() => u => u.IsActive;
}

// 2. Application: Query Assembly
var spec = QuerySpec<User>.Empty
    .And(new ActiveUserSpec())
    .OrderBy(u => u.Name);
// NOTE: `Includes` are not part of QuerySpec as they are an abstraction leak of EF Core. If you need Includes, define them in your IReadRepository implementation, not in the generic Specification.
```

### 2. Changing Invocation in Repositories
The execution also differs. While Ardalis injects the `ISpecificationEvaluator`, here you delegate to extension methods.

**Ardalis:**
```csharp
var users = await _repository.ListAsync(new ActiveUsersSpec());
```

**EricksonLopez.Specification (with EF Core):**
```csharp
// In your Repository implementation (Infrastructure)
public async Task<List<User>> QueryAsync(QuerySpec<User> query)
{
    return await _dbContext.Users.Apply(query).ToListAsync();
}
```
