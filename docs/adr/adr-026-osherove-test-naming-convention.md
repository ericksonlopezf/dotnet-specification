# adr-026: Institutional Adoption of the Osherove Naming Convention for Tests and Local Suppression of IDE1006 / CA1707

## Status

Accepted

## Date

2026-08-18

## Context

Standard .NET coding style analyzers (specifically Microsoft CA1707: *"Identifiers should not contain underscores"* and Roslyn IDE1006: *"Naming rule violation: These words must begin with upper case characters / cannot contain underscores"*) enforce strict `PascalCase` across all methods and public identifiers in C# codebases.

However, unit tests and integration tests serve a fundamentally different purpose than production library APIs:
- Production APIs are consumed by developers writing code and must adhere to standard .NET framework design guidelines (`PascalCase`).
- Test methods are **living, executable specifications** of system behavior. When a test fails in continuous integration (GitHub Actions, Azure DevOps, terminal runners), the test method name is the primary diagnostic message.

Without words separated by underscores, test names become dense strings of concatenated words (e.g. `IsSatisfiedByActiveCustomerWhenActiveReturnsTrue` or `TranslateLikeStartsWithReturnsMatchingRows`), increasing cognitive load and slowing down incident triage.

## Problem

Under Roy Osherove's testing standard, test method names follow the canonical pattern:
```
[UnitOfWork]_[StateUnderTest]_[ExpectedBehavior]
```
or its equivalent:
```
[Method]_[Scenario]_[Result]
```

Examples:
- `IsSatisfiedBy_ActiveCustomer_WhenIsActive_ReturnsTrue`
- `Translate_LikeStartsWith_ReturnsMatchingRows`
- `GetOrCompile_UnderConcurrentAccess_IsThreadSafeAndConsistent`
- `Apply_WithSeekBefore_FiltersAndLimitsCorrectly`

When analyzers enforce CA1707 or IDE1006 on test assemblies without distinction:
1. Developers are forced to choose between unreadable test names or compiler warnings.
2. If `TreatWarningsAsErrors` is enabled globally, builds fail unless test names lose their semantic structure.
3. CI/CD test reports lose clarity when identifying regression roots.

## Options Considered

### Option A: Enforce PascalCase without underscores on test methods — Rejected
- Eliminates CA1707/IDE1006 warnings without configuration.
- **Drawback:** Degrades test readability in CI logs, IDE test explorers, and failure stack traces. Violates the principle that tests are living documentation.

### Option B: Use DisplayName / description attributes in every test — Rejected
- Uses `[Fact(DisplayName = "Is satisfied by active customer when active returns true")]`.
- **Drawback:** Massive boilerplate duplication; runners and CI logs often show the method symbol name rather than the display name; refactoring tools do not rename the display string.

### Option C: Institutionalize the Osherove Pattern and Suppress CA1707 / IDE1006 Locally in Tests — Accepted
- Mandate `Method_Scenario_Result` / `UnitOfWork_StateUnderTest_ExpectedBehavior` across all test projects.
- Suppress CA1707 and IDE1006 exclusively in test and benchmark assemblies via `Directory.Build.props` and the root `.editorconfig`.
- Maintain strict PascalCase and zero-warning enforcement (`TreatWarningsAsErrors = true`) on all production assemblies in `src/`.

## Decision

Adopt **Option C**:
1. **Institutional Standard:** All test methods across the `dotnet-specification` test suite must follow the Osherove naming pattern:
   ```
   [MethodName/UnitOfWork]_[Scenario/StateUnderTest]_[ExpectedResult/Behavior]
   ```
2. **EditorConfig Rule:** In `.editorconfig`, configure test and benchmark paths to disable CA1707, CA1515, and IDE1006:
   ```ini
   [tests/**/*.cs]
   dotnet_diagnostic.IDE1006.severity = none
   dotnet_diagnostic.CA1707.severity = none
   dotnet_diagnostic.CA1515.severity = none

   [benchmarks/**/*.cs]
   dotnet_diagnostic.IDE1006.severity = none
   dotnet_diagnostic.CA1707.severity = none
   ```
3. **Directory.Build.props Configuration:** Ensure test project property groups include `IDE1006;CA1707;CA1515` in `<NoWarn>`.

## Decision Drivers

- **Living Documentation:** Test names are specifications that must be immediately legible to engineers and stakeholders during CI failures.
- **CI/CD Observability:** Test runners (xUnit console, GitHub Actions summary) format test names as titles. Underscore-separated segments allow instant identification of the failing scenario and expectation.
- **FIRST Principles:** Improves the *Self-Validating* and *Timely* properties by making failure diagnosis instantaneous.
- **Pragmatism > Purism:** Production code retains 100% architectural and analyzer purity; test code optimizes for developer ergonomics and clarity.

## Consequences

### Positive
- Test failure logs in CI/CD clearly differentiate the Unit of Work, the Scenario, and the Expected Result.
- 0 compiler warnings across the entire solution.
- Consistent, predictable naming conventions for any new contributor.

### Negative
- Test method naming deviates from Microsoft framework design guidelines for public symbols (justified because test methods are never called as public APIs by external consumers).

## Reconsideration Criteria

None. This is an established industry standard for .NET testing frameworks (xUnit, NUnit, MSTest).
