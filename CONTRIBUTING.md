# Contributing to EricksonLopez.Specification

Thank you for considering contributing to `EricksonLopez.Specification`. Contributions of all kinds are welcome — bug reports, fixes, documentation improvements, and new features.

---

## Development Environment

### Prerequisites

- [.NET 10.0.302 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (pinned in `global.json`, `rollForward: latestFeature`)
- Docker — required for running containerized integration tests (`tests/EricksonLopez.Specification.PostgreSql.IntegrationTests` and `tests/EricksonLopez.Specification.MongoDB.IntegrationTests`)
- .NET local tools (installed via `dotnet tool restore`): **Stryker.NET** for mutation testing

### Clone and Restore

```bash
git clone https://github.com/ericksonlopezf/dotnet-specification.git
cd dotnet-specification
dotnet tool restore          # installs stryker
dotnet restore EricksonLopez.Specifications.slnx
```

---

## Build

```bash
dotnet build EricksonLopez.Specifications.slnx
```

> **Note**: `TreatWarningsAsErrors=true` is set globally in `Directory.Build.props`. The build will fail on any compiler warning, including missing XML documentation (`CS1591`) on public members.

---

## Running Tests

### Unit and Integration Tests

```bash
dotnet test EricksonLopez.Specifications.slnx --verbosity normal
```

Integration tests for PostgreSQL and MongoDB use **Testcontainers** and spin up containers automatically when Docker is active.

Alternatively, start the services manually via Docker Compose:

```bash
docker compose -f tests/docker-compose.yml up -d
dotnet test tests/EricksonLopez.Specification.PostgreSql.IntegrationTests --verbosity normal
dotnet test tests/EricksonLopez.Specification.MongoDB.IntegrationTests --verbosity normal
```

### Mutation Testing

We use [Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction) to ensure test quality.

**Thresholds** (from `stryker-config.json`):
- High: 100% — target
- Low: 98% — warning
- Break: 95% — pipeline failure

```bash
dotnet tool restore
dotnet stryker
```

> Stryker runs can take 10–30 minutes depending on the machine. Running with `--project` limits to a specific package.

---

## Running Benchmarks

If modifying performance-critical code (`ExpressionHasher`, `ExpressionSimplifier`, `ExpressionComposer`, SQL translation), run benchmarks before and after your change:

```bash
cd benchmarks/EricksonLopez.Specification.Benchmarks
dotnet run -c Release
```

See [docs/benchmarks.md](docs/benchmarks.md) for scenario descriptions and interpretation guidance.

---

## Branching Strategy

| Branch type | Pattern | Example |
|---|---|---|
| Feature | `feature/*` | `feature/sqlite-dialect` |
| Bug fix | `bugfix/*` | `bugfix/like-translation` |
| Documentation | `docs/*` | `docs/update-aot-guide` |

| Long-lived branch | CI Trigger | Purpose |
|---|---|---|
| `main` | Push + PR (CI + Release Please) | Production-ready code; releases are cut from here |
| `develop` | Push + PR (CI) | Integration branch for feature work |

All features and bugfixes are merged to `develop` first; `develop` is promoted to `main` when ready for release.

---

## Commit Convention

Please follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat(sql): add MsSqlDialect for SQL Server
fix(core): correct StartsWith LIKE pattern escaping
docs(aot): update .NET 10 AOT sample configuration
test(dapper): add integration tests for QueryAsync
```

Types: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `chore`

---

## Pull Request Checklist

Before opening a PR, ensure:

- [ ] `dotnet build` passes with zero warnings (warnings are errors in CI)
- [ ] `dotnet test` — all tests pass
- [ ] If modifying query/expression logic — run `dotnet stryker` and check mutation score ≥ 98%
- [ ] If modifying performance-critical code — run benchmarks and confirm no regression
- [ ] New public members have XML documentation (`///` comments)
- [ ] Corresponding documentation in `/docs/` is updated if applicable
- [ ] The PR description is filled out using the provided template

---

## Code Style

- The project uses `.editorconfig` for code style enforcement.
- `EnforceCodeStyleInBuild=true` and `AnalysisMode=All` are active.
- `LangVersion=preview` is configured globally.
- `Nullable=enable` is required everywhere.

---

## Code of Conduct

This project follows the [Contributor Covenant v2.1](CODE_OF_CONDUCT.md). By participating, you agree to abide by its terms.

Report violations to `ericksonlopezf@gmail.com`.
