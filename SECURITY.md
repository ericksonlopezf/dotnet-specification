# Security Policy

## Supported Versions

Only the current major release line is actively supported with security updates.

| Version | Supported | Notes |
|---------|-----------|-------|
| 1.0.x   | ✅        | Current active development (pre-release; not yet published to NuGet.org) |
| < 1.0   | ❌        | Pre-release iterations are not supported |

> **Note**: No NuGet packages have been published yet. `v1.0.0` is the planned initial release
> (`VersionPrefix=1.0.0` in `Directory.Build.props`, tracked by `.release-please-manifest.json`).

---

## Reporting a Vulnerability

If you discover a security vulnerability in this project, please report it **privately**:

- **Email**: ericksonlopezf@gmail.com
- **GitHub Private Advisory**: Use [GitHub's private vulnerability reporting](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing/privately-reporting-a-security-vulnerability) if available for this repository

Please **do not** disclose security-related issues publicly until a fix has been announced.

**Response SLA**: All security reports are reviewed within 72 hours. A fix timeline will be communicated within that window.

---

## Supply Chain Security

The following supply chain security mechanisms are configured and ready for the first NuGet publish event:

| Mechanism | Status | Source |
|-----------|--------|--------|
| Strong Name Signing | ✅ Configured | `publish.yml` — `SNK_KEY` secret, base64 key decoded at publish time |
| NuGet Trusted Publishing (OIDC) | ✅ Configured | `publish.yml` — `NuGet/login@v1` action, no static API key required |
| Sigstore Provenance Attestation | ✅ Configured | `publish.yml` — `actions/attest-build-provenance@v2` on all `.nupkg` files |
| Central Package Management (CPM) | ✅ Active | All versions pinned in `Directory.Packages.props` |
| Dependabot | ✅ Active | `.github/dependabot.yml` — weekly NuGet + GitHub Actions scanning |
| Mutation Quality Gate | ✅ Active | `publish.yml` calls `verify-mutation-gate.js`; break threshold ≥ 95% |

### Strong Name Key Recovery

The `.snk` assembly signing key is stored exclusively as a GitHub Actions secret (`SNK_KEY`) encoded in base64. It is decoded at publish time only and never committed to the repository. The file `EricksonLopez.Specification.snk` is regenerated ephemerally during the publish job.

### NuGet Trusted Publishing (OIDC)

Packages are published to NuGet.org using OpenID Connect (OIDC) federated identity (`NuGet/login@v1`). This eliminates the need for long-lived static API keys. The workflow requires `id-token: write` permissions.

### Sigstore Provenance Attestation

Every `.nupkg` file produced by the publish workflow receives a Sigstore-signed SLSA provenance attestation via `actions/attest-build-provenance@v2`. This attestation is published to GitHub's attestation store and can be verified by consumers.

### Mutation Testing Quality Gate at Publish

The `publish.yml` workflow runs `scripts/verify-mutation-gate.js` before packing. If any package falls below the configured break threshold (≥ 95% mutation score), the publish is aborted.

---

## Dependency Management

All external dependencies are centrally managed in [`Directory.Packages.props`](Directory.Packages.props) using .NET's [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management). This prevents version drift between projects.

Key runtime dependencies:

| Package | Pinned Version |
|---------|---------------|
| `Dapper` | 2.1.66 |
| `Dapper.AOT` | 1.0.52 |
| `Npgsql` | 9.0.3 |
| `Microsoft.EntityFrameworkCore` | 9.0.2 |
| `MongoDB.Driver` | 3.10.0 |
| `OpenTelemetry.Api` | 1.10.0 |

---

## Known Security Boundaries

### SQL Transpiler (`EricksonLopez.Specification.Sql`)

The translation engine generates parameterized SQL **only**. It does not concatenate raw user-provided values into SQL strings. All dynamic values are emitted as named parameters (e.g., `@p0`, `$1`, `:p0`) and bound via Dapper's `DynamicParameters` or equivalent.

**This is a critical security boundary.** If you encounter an edge case where a value is concatenated directly into a SQL string by any dialect class, that is a **critical security vulnerability** and must be reported immediately via the private disclosure process above.

### Expression Engine

`Specification<T>` constructs C# `Expression<Func<T,bool>>` trees. These trees are:

- **Immutable** after construction
- **Not evaluated as code** — interpreted by `ExpressionInterpreter` via tree traversal
- **Not serialized** — they remain as in-memory CLR objects

The engine does not evaluate arbitrary strings as code. There is no `eval()` equivalent.

### Reflection Annotations

Components that use reflection are annotated:

- `ExpressionInterpreter`: `[DynamicallyAccessedMembers]` on `PropertyInfo.GetValue`
- `QuerySpecTranslator<T>`: `[RequiresUnreferencedCode]` on closure extraction via `FieldInfo`
- `ExpressionCompilationCache`: `[RequiresDynamicCode]` (JIT-only path; cannot be used in Native AOT)

These annotations ensure the .NET linker/trimmer warns consumers when these paths are used in NativeAOT scenarios.
