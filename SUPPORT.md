# Support Policy

## Channels

If you need help using `EricksonLopez.Specification`, the primary support channels are:

1. **GitHub Issues**: Use the [Issue Tracker](https://github.com/ericksonlopezf/dotnet-specification/issues) for bug reports and feature requests. Please use the appropriate template when filing an issue.
2. **GitHub Discussions**: Use [GitHub Discussions](https://github.com/ericksonlopezf/dotnet-specification/discussions) for questions, architectural advice, or sharing how you use the library in your projects.
3. **Direct Maintainer Contact**: For support questions not suitable for public channels or direct maintainer inquiries, contact [`ericksonlopezf@gmail.com`](mailto:ericksonlopezf@gmail.com).

---

## Documentation Reference

Before opening an issue, please check the existing documentation:

| Document | Description |
|---|---|
| [README.md](README.md) | Quick start and package overview |
| [Cookbook](docs/cookbook.md) | Copy-paste recipes for common scenarios |
| [System Overview](docs/system-overview.md) | Architecture and design principles |
| [Public API Surface](docs/public-api-surface.md) | Complete API reference |
| [AOT Guide](docs/aot.md) | NativeAOT compatibility and guidance |
| [Performance Guide](docs/performance-guide.md) | Performance tiers and optimization |
| [Migration from Ardalis](docs/migration-from-ardalis.md) | Step-by-step migration guide |
| [Best Practices](docs/best-practices.md) | Patterns, anti-patterns, and when to use Specifications |
| [Architecture Decision Records](docs/adr/README.md) | ADR index and design choices |

---

## When Filing a Bug Report

Please use the [Bug Report template](.github/ISSUE_TEMPLATE/bug-report.md) and include:

- **Library version** (NuGet package version or commit SHA)
- **Target framework** (e.g., `net10.0`)
- **Infrastructure** (e.g., EF Core 8, Dapper 2.1, PostgreSQL 15)
- **Minimal reproduction** — a small code snippet demonstrating the issue
- **Expected vs actual behavior**
- **Generated SQL** (if relevant)

---

## Enterprise Support

There is no SLA-backed enterprise support. All support is community-driven and provided on a best-effort basis by the maintainers.

For security vulnerabilities, follow the private disclosure process in [SECURITY.md](SECURITY.md).
