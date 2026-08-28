# adr-010: No EF Core Dependency in Core Packages

**Status**: Accepted
**Date**: 2026-08-13

## Context

EF Core is the dominant .NET ORM. Many specification libraries couple directly to it.
IQueryable<T> is defined in System.Linq (BCL) -- not in EF Core.

## Problem

Should EricksonLopez.Specification depend on Microsoft.EntityFrameworkCore?

## Decision

NO. Core packages (Abstractions + EricksonLopez.Specification) have zero ORM dependencies.
EF Core integration is provided via the Linq package (IQueryable<T> is BCL).

## Why

- Dapper consumers would inherit a 20+ package transitive dependency graph unnecessarily
- Provider independence is a core architectural pillar
- IQueryable<T> is in System.Linq (BCL) -- no EF Core needed for the LINQ adapter
- AsNoTracking and AsSplitQuery are inert boolean flags in QuerySpec<T> -- not EF Core types
- Separation enables testing with in-memory LINQ providers without EF Core runtime

## Consequences

- EF Core users install the Linq package (thin adapter, no EF Core reference)
- EF Core-specific features (AsNoTracking, AsSplitQuery) are inert flags interpreted by the consumer's repository
- Include/ThenInclude is not in the core (see adr-002)
- No EFCoreSpecificationEvaluator class (Ardalis pattern) -- consumers implement their own repository

## Competitive Impact

Ardalis core is also EF Core-free. This is table stakes for a serious specification library.
The differentiator is sustaining zero-ORM-dependency all the way through the SQL translation layer.
