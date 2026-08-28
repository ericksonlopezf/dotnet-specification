# adr-023: ISpecification<T> Placement in Abstractions vs Core

## Status

Accepted

## Date

2026-08-14

## Context

A core architectural principle of DDD and Clean Architecture is that Domain contracts should have minimal dependencies, while Domain implementations and helper engines may reside in core packages. In `EricksonLopez.Specification`, the minimal interface `ISpecification<T>` and the query descriptor `QuerySpec<T>` define the boundary contracts of the library.

## Problem

Should `ISpecification<T>` live in `EricksonLopez.Specification.Abstractions` or `EricksonLopez.Specification` (Core)?

- If placed in Core, consumers who only need the interface contract in pure domain entity libraries would be forced to reference the entire expression compilation and evaluation engine.
- If placed in Abstractions, pure domain entities and interfaces can reference only `Abstractions` with zero dependencies on engine visitors, caching, or interpreters.

## Options Considered

### Option A: `ISpecification<T>` in `EricksonLopez.Specification.Abstractions` — Accepted

- `Abstractions` contains `ISpecification<T>`, `IReadRepository<T>`, `QuerySpec<T>`, `QuerySpecProjected<T, TResult>`, and `OrderClause<T>`.
- `Core` contains `Specification<T>` (abstract base class), `ExpressionComposer`, `ExpressionInterpreter`, `ExpressionSimplifier`, `ExpressionCompilationCache`, etc.
- Pure Domain projects only need to reference `Abstractions`.

### Option B: Everything in a single `EricksonLopez.Specification` package

- Simpler packaging model (one NuGet package).
- Couples interface contracts with the internal expression engine and caching infrastructure.

## Decision

Implement Option A: Place `ISpecification<T>` and pure query descriptors in `EricksonLopez.Specification.Abstractions`. `Specification<T>` (abstract class) and runtime expression engines reside in `EricksonLopez.Specification`.

## Decision Drivers

- **Clean Architecture**: Domain model entities and interfaces depend only on abstractions, not concrete engines.
- **Micro-packaging**: Lowers dependency footprint for downstream packages.
- **AOT & Trimming**: `Abstractions` contains zero IL/reflection, making it 100% trim-safe and AOT-pure.

## Consequences

### Positive

- Clean separation between interface contracts and execution engines.
- External libraries can implement or accept `ISpecification<T>` without taking a dependency on the expression engine.

### Negative

- Consumers using the standard `Specification<T>` base class reference `EricksonLopez.Specification`, which brings `Abstractions` transitively.

## Reconsideration Criteria

None. This separation is fundamental to Clean Architecture and DDD principles.
