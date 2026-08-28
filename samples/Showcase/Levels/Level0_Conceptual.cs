// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Specification.Showcase.Levels;

/// <summary>
/// Level 0 — Conceptual Overview.
/// Introduces theoretical foundations, the problem solved by the library,
/// architectural trade-offs, and comparison against alternatives.
/// </summary>
public sealed class Level0_Conceptual : ILevel
{
    private readonly ILogger<Level0_Conceptual> _logger;

    /// <inheritdoc/>
    public string Name => "Level 0 — Conceptual Overview";

    /// <inheritdoc/>
    public string Description => "Theoretical foundations and architectural overview of EricksonLopez.Specification.";

    /// <summary>
    /// Initializes a new instance of the level.
    /// </summary>
    /// <param name="logger">The logger used for output.</param>
    public Level0_Conceptual(ILogger<Level0_Conceptual> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task ExecuteAsync()
    {
        _logger.LogInformation("--- {Name} ---", Name);

        Console.WriteLine("""

=============================================================
  WHAT IS EricksonLopez.Specification?
=============================================================

A high-performance implementation of the Specification Pattern
(Domain-Driven Design) for .NET 10+.

Encapsulates business rules into strongly typed classes that are:
  • Evaluated in-memory (IsSatisfiedBy)
  • Translated directly to SQL without an ORM (SQL AST Translator)
  • Composed with boolean logic (And / Or / Not)
  • Integrated with IQueryable (EF Core LINQ extension)
  • Safe for Native AOT (zero runtime dynamic code generation by default)

=============================================================
  WHAT PROBLEM DOES IT SOLVE?
=============================================================

Without the Specification Pattern:
  • Business rules are scattered across services and repositories.
  • The same predicates are duplicated in queries, validation, tests, and reports.
  • Changing a business rule requires modifying multiple files.
  • Inline lambda expressions cannot be unit tested in isolation.

With EricksonLopez.Specification:
  • Each rule lives in a single class: ActiveCustomerSpecification.
  • Unit tested directly: spec.IsSatisfiedBy(customer).
  • Reusable across CQRS handlers, repositories, export jobs, and filters.
  • Fully composable: active.And(vip).Or(premium).Not().

=============================================================
  LIBRARY ARCHITECTURE
=============================================================

  ┌─────────────────────────────────────────────────┐
  │  EricksonLopez.Specification.Abstractions        │
  │  • ISpecification<T>                            │
  │  • IExpressionSpecification<T>                  │
  │  • IReadRepository<T>                           │
  │  • QuerySpec<T>  / QuerySpec<T, TResult>        │
  └──────────────────────────┬──────────────────────┘
                             │
  ┌──────────────────────────▼──────────────────────┐
  │  EricksonLopez.Specification (Core)              │
  │  • Specification<T> (base class)                │
  │  • Spec (factory static)                        │
  │  • ExpressionComposer  / ExpressionHasher       │
  │  • ExpressionSimplifier / ExpressionInterpreter │
  │  • ExpressionCompilationCache                   │
  │  • SpecificationDiagnostics (OTel)              │
  └────────┬───────────────────────────┬────────────┘
           │                           │
  ┌────────▼───────────┐    ┌──────────▼──────────┐
  │  .Linq             │    │  .Sql                │
  │  IQueryable<T>     │    │  QuerySpecTranslator │
  │  Apply / Any /     │    │  ISqlDialect         │
  │  Count             │    │  QueryModel / AST    │
  └────────────────────┘    └──────────┬───────────┘
                                       │
                         ┌─────────────┼─────────────┐
                   ┌─────▼──┐    ┌─────▼──┐    ┌─────▼──┐
                   │PostgeSQL│    │SqlServer│    │SQLite  │
                   └────────┘    └────────┘    └────────┘
                                       │
                            ┌──────────▼──────────┐
                            │  .Dapper             │
                            │  QuerySpecDapper     │
                            │  Extensions          │
                            └─────────────────────┘

=============================================================
  ADVANTAGES
=============================================================
  ✓ Reusability (DRY): single specification, multiple consumers.
  ✓ Testability: pure unit tests without infrastructure dependencies.
  ✓ Safe boolean composition (And / Or / Not).
  ✓ Direct parameterized SQL generation (ORM-independent) — optimal for Dapper.
  ✓ Native AOT safe: ExpressionInterpreter eliminates runtime JIT compilation.
  ✓ Thread-safe: specifications and QuerySpec instances are immutable.
  ✓ Built-in observability: native OpenTelemetry metrics and activity tracing.

=============================================================
  TRADE-OFFS & LIMITATIONS
=============================================================
  ✗ Initial learning curve for developers new to DDD Specifications.
  ✗ Class overhead for strictly trivial one-line queries.
  ✗ Complex nested subqueries require dedicated LINQ expressions.
  ✗ Direct SQL path requires column resolution (IColumnNameResolver).

=============================================================
  COMPARISON WITH ALTERNATIVES
=============================================================

  Ardalis.Specification:
    • Tight coupling to EF Core; cannot generate standalone SQL.
    • Passes complete specification object to repository layer.
    • Not fully Native AOT compatible.

  EricksonLopez.Specification:
    • Provider agnostic (works with EF Core, Dapper, MongoDB, Memory).
    • AST-based SQL generation (QueryModel + ISqlDialect).
    • Native AOT first with zero DynamicCode execution by default.
    • Compile-time Roslyn analyzers enforcing architecture purity.

=============================================================
""");

        return Task.CompletedTask;
    }
}




