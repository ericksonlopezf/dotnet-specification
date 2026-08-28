# Testing Suite Architecture & Guidelines — EricksonLopez.Specification

This document details the architecture, conventions, execution instructions, and test double guidelines for the `dotnet-specification` test suite.

---

## 1. Test Projects Inventory

| Test Project | Scope & Focus | Parallelization |
| :--- | :--- | :--- |
| `EricksonLopez.Specification.Tests` | Core Domain, Expressions, Engine, Diagnostics, and LINQ Extensions. | ✅ Enabled (Isolated `ExpressionCompilationCacheCollection`) |
| `EricksonLopez.Specification.Sql.Tests` | SQL Expression Translation, Dialect Rendering (MSSQL, SQLite, MySQL), QueryPlanCache. | ✅ Enabled (Isolated `QueryPlanCacheCollection`) |
| `EricksonLopez.Specification.Sqlite.Tests` | SQLite specific dialect behavior and in-memory verification. | ✅ Enabled |
| `EricksonLopez.Specification.MySql.Tests` | MySQL specific dialect rendering, backtick quoting, and limit/offset. | ✅ Enabled |
| `EricksonLopez.Specification.MariaDb.Tests` | MariaDB specific dialect rendering and quoting. | ✅ Enabled |
| `EricksonLopez.Specification.MsSql.Tests` | Microsoft SQL Server specific dialect rendering and brackets quoting. | ✅ Enabled |
| `EricksonLopez.Specification.Oracle.Tests` | Oracle SQL specific dialect rendering, identifier quoting, and FETCH FIRST. | ✅ Enabled |
| `EricksonLopez.Specification.PostgreSql.Tests` | PostgreSQL specific dialect rendering, identifier quoting, and array operations in-memory. | ✅ Enabled |
| `EricksonLopez.Specification.Dapper.Tests` | 100% In-Memory Unit Tests for Dapper extensions using ADO.NET test doubles. | ✅ Enabled |
| `EricksonLopez.Specification.DapperExtensions.Tests` | Unit Tests for DapperExtensions UnitOfWork integration using linked test doubles. | ✅ Enabled |
| `EricksonLopez.Specification.MongoDB.Tests` | MongoDB filter and sort compilation, FindFluent evaluator, and repository unit tests. | ✅ Enabled |
| `EricksonLopez.Specification.EntityFrameworkCore.Tests` | EF Core `IReadRepository` integration with in-memory SQLite/InMemory provider. | ✅ Enabled |
| `EricksonLopez.Specification.PostgreSql.IntegrationTests` | PostgreSQL integration tests against live container via `PostgreSqlFixture` & Dapper integration. | 🔒 Serialized (`PostgreSqlDatabase`) |
| `EricksonLopez.Specification.Analyzers.Tests` | Roslyn analyzers testing (`SPEC001` - `SPEC011`) using Roslyn test harnesses. | ✅ Enabled |
| `EricksonLopez.Specification.Generators.Tests` | Incremental source generator diagnostic and output tests. | ✅ Enabled |

---

## 2. Test Conventions & Principles

### FIRST Principles
- **Fast**: Unit tests complete in milliseconds total across 1,050+ tests.
- **Independent**: Tests do not share mutable state. Shared static caches (e.g. `QueryPlanCache`, `ExpressionCompilationCache`) are managed via dedicated non-parallel xUnit collections (`[Collection("QueryPlanCacheCollection")]`, `[Collection("ExpressionCompilationCacheCollection")]`) and reset in `Dispose()`.
- **Repeatable**: Tests produce deterministic results in any order and environment.
- **Self-Validating**: All assertions use AwesomeAssertions / FluentAssertions with clear, expressive error feedback.
### Naming Conventions (Osherove Pattern)
Per [**adr-026**](../docs/adr/adr-026-osherove-test-naming-convention.md), all test methods across the solution strictly adhere to the Roy Osherove naming convention:
```
[UnitOfWork/Method]_[Scenario/StateUnderTest]_[ExpectedBehavior/Result]
```
- **Example**: `IsSatisfiedBy_ActiveCustomer_WhenIsActive_ReturnsTrue`
- **Example**: `Translate_LikeStartsWith_ReturnsMatchingRows`
- **Justification**: Test methods are living, executable specifications. Underscore separation maximizes readability in CI/CD terminal runners and failure stack traces. Roslyn analyzers `IDE1006` and `CA1707` are locally suppressed for test and benchmark projects in `.editorconfig` and `Directory.Build.props`.

### Test Doubles
- Shared ADO.NET test doubles (`FakeDbConnection`, `FakeDbCommand`, `FakeDbDataReader`, `FakeDbTransaction`, `FakeDbParameterCollection`) reside in `EricksonLopez.Specification.Dapper.Tests/FakeDbConnection.cs` and support cancellation token propagation (`Task.FromCanceled`).

---

## 3. Running Tests

### Running All Tests
```powershell
dotnet test EricksonLopez.Specifications.slnx
```

### Running Unit Tests Only (Fast In-Memory)
```powershell
dotnet test --filter "Category!=Integration"
```

### Running Integration Tests (PostgreSQL & MongoDB)
Integration tests run automatically via **Testcontainers** (or connect to pre-started local services). If Docker/containers are unavailable or skipped via `SPEC_SKIP_INTEGRATION_TESTS=true`, tests gracefully skip execution to avoid false-positive test failures:
```powershell
# Option A: Run directly with Testcontainers (Docker must be running)
dotnet test tests/EricksonLopez.Specification.PostgreSql.IntegrationTests/
dotnet test tests/EricksonLopez.Specification.MongoDB.IntegrationTests/

# Option B: Run against pre-started docker-compose service
docker-compose -f tests/docker-compose.yml up -d postgres
dotnet test tests/EricksonLopez.Specification.PostgreSql.IntegrationTests/
```

---

## 4. Mutation Testing with Stryker.NET

Mutation testing is configured via `stryker-config.json`:
```powershell
dotnet stryker
```
Defensive code branches that cannot be naturally triggered via safe expressions (due to .NET BCL expression builder constraints) are explicitly documented and excluded with `// Stryker disable once ...`.
