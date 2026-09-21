# Level 05: Processing — Batch, Conditional, and Concurrent

## Overview

Level 5 demonstrates how specifications interact with batch processing, conditional filtering, and concurrent execution.

## Key APIs Demonstrated

- **QuerySpec<T>.Page(page, pageSize)**: 1-based pagination for batch iteration.
- **Spec.True<T>()**: Identity element for AND chains — no filtering effect. Use as default when a filter is disabled.
- **Spec.False<T>()**: Identity element for OR chains — no filtering effect. Use as base for dynamic OR accumulation.
- **QuerySpec<T> immutability**: Thread-safe by design. Multiple workers can share a single QuerySpec<T> instance.
- **CancellationToken**: Passes through all async paths.

## Patterns

### Batch Processing Pattern

`csharp
var baseSpec = QuerySpec<Customer>.Empty
    .Where(c => c.IsActive)
    .OrderBy(c => c.Name);

int page = 1;
while (!ct.IsCancellationRequested)
{
    var batch = await repo.ListAsync(baseSpec.Page(page, pageSize), ct);
    if (batch.Count == 0) break;
    // process batch...
    page++;
}
`

### Dynamic AND Filter Pattern (Spec.True)

`csharp
Specification<Customer> filter = filterByActive
    ? new ActiveCustomerSpecification()
    : Spec.True<Customer>(); // pass-through — no filtering
`

### Dynamic OR Chain Pattern (Spec.False)

`csharp
Specification<Customer> nameFilter = Spec.False<Customer>(); // neutral base
foreach (var name in segments)
    nameFilter = nameFilter.Or(Spec.For<Customer>(c => c.Name.Contains(name)));
`

## Running Example

See [Level5_Processing.cs](../../samples/Showcase/Levels/Level5_Processing.cs).
