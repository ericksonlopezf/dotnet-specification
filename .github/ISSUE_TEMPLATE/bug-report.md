---
name: Bug report
about: Create a report to help us improve
title: '[BUG] '
labels: bug
assignees: ''

---

**Describe the bug**
A clear and concise description of what the bug is.

**To Reproduce**
Steps to reproduce the behavior:
1. Define specification '...'
2. Apply to `IQueryable` / `IDbConnection` / `IMongoCollection` '...'
3. See error or unexpected behavior

**Expected behavior**
A clear and concise description of what you expected to happen.

**Environment (please complete the following information):**

- OS: [e.g., Windows 11, Ubuntu 24.04]
- .NET SDK Version: [e.g., 10.0.302]
- Runtime mode: [JIT / Native AOT]
- Package(s) affected:
  - [ ] `EricksonLopez.Specification` 1.0.0
  - [ ] `EricksonLopez.Specification.Abstractions`
  - [ ] `EricksonLopez.Specification.Sql`
  - [ ] `EricksonLopez.Specification.PostgreSql`
  - [ ] `EricksonLopez.Specification.MsSql`
  - [ ] `EricksonLopez.Specification.MySql`
  - [ ] `EricksonLopez.Specification.MariaDb`
  - [ ] `EricksonLopez.Specification.Sqlite`
  - [ ] `EricksonLopez.Specification.Oracle`
  - [ ] `EricksonLopez.Specification.Dapper`
  - [ ] `EricksonLopez.Specification.EntityFrameworkCore`
  - [ ] `EricksonLopez.Specification.MongoDB`
  - [ ] `EricksonLopez.Specification.Analyzers`
  - [ ] `EricksonLopez.Specification.Generators`
  - [ ] Other: ___
- Storage/Infrastructure: [e.g., PostgreSQL 16 via Dapper 2.1.66 / EF Core 9.0.2 / MongoDB 7]

**Generated SQL (if applicable)**
If the bug involves SQL generation, paste the generated SQL string and bound parameters here.

**AST / Exception Stack Trace (if applicable)**
Paste the relevant exception and full stack trace here.

**Minimal reproduction**
A small, self-contained code snippet that reproduces the issue.

```csharp
// Your minimal reproduction here
```

**Additional context**
Add any other context about the problem here (analyzer rule violated, Roslyn version, etc.).
