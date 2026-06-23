# BrightPay Take-Home

.NET 10 Blazor Web App scaffold for an expected BrightPay take-home exercise.
The official product spec is not in the repo yet, so the current app is a
readiness screen plus quality gates.

## Quick Start

```bash
mise install
cp .env.example .env
just check-host
```

Use `just up` for the containerized dev app, or `just run` for host `dotnet
watch`.

## Documentation

- [Setup](docs/setup.md)
- [Commands](docs/commands.md)
- [Dependency notes](docs/dependencies.md)
- [Azure OpenTofu deployment](infra/opentofu/README.md)

## Tooling

- .NET 10.0.301 via `mise` and `global.json`
- `just` task runner, with `make` as an optional Unix shim
- Podman-first container defaults, Docker-compatible overrides
- Central NuGet versions in `Directory.Packages.props`
- GitHub Actions in `.github/workflows/ci.yml`
- Containerized Playwright E2E checks
- Repo-local agent skills in `.agents/skills`

## Agent Skills

Codex discovers skills from `SKILL.md` YAML frontmatter. Each repo skill also
has `agents/openai.yaml` metadata for OpenAI UI surfaces.
