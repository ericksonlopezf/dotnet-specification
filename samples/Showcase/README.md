# EricksonLopez.Specification — Official Showcase

The **Showcase** is the official executable reference implementation of the `EricksonLopez.Specification` library. It serves as executable documentation, an integration cookbook, and a progressive learning path through the library's entire public API surface.

---

## 🎯 Architecture & Educational Roadmap

The Showcase is organized into 11 strictly graduated levels (`Levels/` folder), demonstrating the library from foundational domain concepts to enterprise Clean Architecture with CQRS:

| Level | Class | Core Responsibility | Public APIs Demonstrated |
|:---:|---|---|---|
| **0** | `Level0_Conceptual` | Architectural foundations & problem space | Theoretical DDD Specification, comparisons with alternatives, AOT guarantees |
| **1** | `Level1_QuickStart` | Minimal setup, basic composition & operators | `Specification<T>`, `Spec.For`, `Spec.True/False`, `Spec.All`, `Spec.Any`, `Spec.Between` (nullable & struct), `operator &`, `operator \|`, `operator !`, `operator true/false`, `BitwiseAnd`, `BitwiseOr`, `LogicalNot`, `ExpressionDebugFormatterRegistry` |
| **2** | `Level2_Configuration` | Comprehensive QuerySpec capabilities | `QuerySpec<T>`, `QuerySpec<T,TResult>`, `Where`, `TagWith`, `Search`, `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `Page`, `Take`, `Skip`, `Distinct`, `SeekAfter`, `SeekBefore`, `WithCursor`, `ExpressionSimplifier` |
| **3** | `Level3_RealUseCases` | LINQ & in-memory evaluation | `QuerySpecLinqExtensions.Apply`, `Any`, `Count`, `Where`, `All`, `FirstOrDefault` across `IQueryable<T>` and `IEnumerable<T>` |
| **4** | `Level4_AdvancedIntegration` | Provider-agnostic SQL translation | `QuerySpecTranslator<T>`, `ISqlDialect`, `PostgreSqlDialect`, `MsSqlDialect`, `SqliteDialect`, `MySqlDialect`, `MariaDbDialect`, `OracleDialect`, `SnakeCaseColumnNameResolver`, `VerbatimColumnNameResolver` |
| **5** | `Level5_Processing` | Batching, pagination & cancellation | Batch processing via `Page()`, `CancellationToken` flow, conditional filtering with `Spec.True<T>()`, thread-safety verification |
| **6** | `Level6_ErrorHandling` | Error prevention & Result pattern | `NotSupportedException` avoidance, argument fast-fail validation, defensive `Spec.Between` checks, `ReadRepositoryResultExtensions` (`ListResultAsync`, `FirstOrDefaultResultAsync`, `SingleOrDefaultResultAsync`, `GetByIdResultAsync`) |
| **7** | `Level7_Scalability` | Low-level performance & AST engine | `ExpressionHasher`, `ExpressionEqualityComparer`, `ExpressionCompilationCache`, `ExpressionInterpreter`, `ExpressionComposer` (`AndAll`, `OrAny`), `SpecificationDiagnostics` (OpenTelemetry metrics & activity) |
| **8** | `Level8_Customization` | Custom interfaces & extensibility | Custom `IColumnNameResolver` (Legacy & Explicit mappings), custom `ISqlDialect` implementation |
| **9** | `Level9_Extensions` | Ecosystem integrations | `QuerySpecDapperExtensions` (Dapper), `EfSpecificationEvaluator` & `QuerySpecEfCoreExtensions` (EF Core), `MongoSpecificationEvaluator` & `MongoReadRepository` (MongoDB), `SpecificationDapperExtensions` (UnitOfWork) |
| **10** | `Level10_EnterpriseArchitecture` | Clean Architecture + DDD + CQRS | `IReadRepository<T>`, `EfReadRepository<TDbContext, TEntity>`, CQRS Query handlers, ServiceCollection DI extensions |

---

## 🚀 How to Run

### Interactive Mode (Step-by-Step)

```bash
dotnet run --project samples/Showcase/EricksonLopez.Specification.Showcase.csproj
```
*Prompts to press `<Enter>` after each level, allowing step-by-step console inspection.*

### Automated / CI Mode (Unattended)

```bash
# In PowerShell:
"" | dotnet run --project samples/Showcase/EricksonLopez.Specification.Showcase.csproj

# In Bash / Linux:
dotnet run --project samples/Showcase/EricksonLopez.Specification.Showcase.csproj < /dev/null
```

---

## 🛠️ Dependency Injection & Host Setup

The Showcase uses the standard Microsoft Generic Host (`Microsoft.Extensions.Hosting`):

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureServices(services =>
    {
        // 1. Register domain specifications (Singleton: immutable and thread-safe)
        services.AddSingleton<ActiveCustomerSpecification>();
        services.AddSingleton<VipCustomerSpecification>();

        // 2. Register Showcase levels
        services.AddTransient<ILevel, Level0_Conceptual>();
        services.AddTransient<ILevel, Level1_QuickStart>();
        // ... Levels 2 to 10
    })
    .Build();

await host.RunAsync();
```

---

## 📐 Clean Architecture Integration Pattern

```
┌─────────────────────────────────────────────────────────────┐
│                       Presentation                          │
│         API Controllers / Minimal APIs / gRPC               │
└──────────────────────────────┬──────────────────────────────┘
                               │ Dispatches Queries / Commands
┌──────────────────────────────▼──────────────────────────────┐
│                    Application (CQRS)                       │
│   • GetActiveCustomersQuery                                 │
│   • GetActiveCustomersHandler(IReadRepository<Customer>)   │
│   → Strictly depends on IReadRepository<T> & QuerySpec<T>   │
└──────────────────────────────┬──────────────────────────────┘
                               │ Uses Domain Rules
┌──────────────────────────────▼──────────────────────────────┐
│                       Domain (Core)                         │
│   • Customer, Order (Entities)                              │
│   • ActiveCustomerSpecification : Specification<Customer>   │
│   • VipCustomerSpecification : Specification<Customer>      │
│   → ZERO external dependencies. Pure C# + Expression Trees. │
└──────────────────────────────▲──────────────────────────────┘
                               │ Implements Repositories
┌──────────────────────────────┴──────────────────────────────┐
│                       Infrastructure                        │
│   • EfReadRepository<AppDbContext, Customer>                │
│   • MongoReadRepository<Customer>                           │
│   • QuerySpecTranslator & Dapper extensions                 │
└─────────────────────────────────────────────────────────────┘
```

---

## 🔒 Source Code Guarantee

1. **Strictly Verified Public APIs**: Every method, property, overload, operator, and interface used in this Showcase exists in `src/`.
2. **Native AOT Compatible**: Uses `ExpressionInterpreter` for in-memory evaluation and static SQL AST generation without IL emit.
3. **Always Compilable**: Continuously verified via solution build and unit tests.
