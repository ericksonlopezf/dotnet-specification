# Level 06: Error Handling — Exceptions and Result Pattern

## Overview

Level 6 covers runtime exceptions, pre-execution validation, and the functional Result Pattern.

## Exception Types

| Exception | Trigger | Solution |
|---|---|---|
| ArgumentNullException | QuerySpec.Where(null) | Validate before calling |
| ArgumentOutOfRangeException | Page(0, n) or Skip(-1) | Use page >= 1, skip >= 0 |
| ArgumentException | Spec.Between(lower:50, upper:10) | Ensure lower <= upper |
| NotSupportedException | Translator receives unsupported expression | Rewrite expression |
| InvalidOperationException | SingleOrDefaultAsync() finds multiple | Narrow criteria or use FirstOrDefaultAsync |

## Pre-Execution Validation

Use QuerySpecExtensions to validate before executing:

`csharp
if (!spec.HasCriteria())
    _logger.LogWarning("Unbounded query — no filter criteria.");
if (!spec.HasPagination())
    _logger.LogWarning("Unbounded query — no Take limit.");
if (spec.HasOrdering() && !spec.HasPagination())
    _logger.LogWarning("Sorting without pagination can be expensive.");
`

## Result Pattern

ReadRepositoryResultExtensions transforms null returns into typed Result<T>:

`csharp
var result = await repo.FirstOrDefaultResultAsync(spec);
if (result.IsFailure)
    return Error.NotFound; // null → Result.Failure(Error.NotFound)
`

Available methods:
- FirstOrDefaultResultAsync
- SingleOrDefaultResultAsync
- ListResultAsync
- GetByIdResultAsync

## Running Example

See [Level6_ErrorHandling.cs](../../samples/Showcase/Levels/Level6_ErrorHandling.cs).
