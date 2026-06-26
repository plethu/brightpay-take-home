set dotenv-load
set default-list

solution := "BrightPay.TakeHome.slnx"
web_project := "src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj"
unit_tests := "tests/BrightPay.TakeHome.Tests.Unit/BrightPay.TakeHome.Tests.Unit.csproj"
component_tests := "tests/BrightPay.TakeHome.Tests.Components/BrightPay.TakeHome.Tests.Components.csproj"
tooling_project := "tools/BrightPay.TakeHome.Tooling/BrightPay.TakeHome.Tooling.csproj"

container_runtime := env_var_or_default("CONTAINER_RUNTIME", "docker")
compose := if container_runtime == "docker" { "docker compose" } else { "podman-compose" }
tofu := env_var_or_default("TOFU", "mise exec -- tofu")
sql_host_port := env_var_or_default("SQL_HOST_PORT", "14333")
sql_server_password := env_var_or_default("SQL_SERVER_PASSWORD", "BrightPay_takehome_Passw0rd!")
host_checkout_connection := env_var_or_default(
    "ConnectionStrings__CheckoutDatabase",
    "Server=127.0.0.1," + sql_host_port + ";Database=BrightPayTakeHome;User Id=sa;Password=" + sql_server_password + ";TrustServerCertificate=True",
)

host_uid := `id -u`
host_gid := `id -g`

export HOST_UID := host_uid
export HOST_GID := host_gid
export SQL_HOST_PORT := sql_host_port
export SQL_SERVER_PASSWORD := sql_server_password
export ConnectionStrings__CheckoutDatabase := host_checkout_connection
export DOTNET_CLI_TELEMETRY_OPTOUT := "1"
export DOTNET_NOLOGO := "1"

dotnet := compose + " run --rm --no-deps sdk dotnet"
dotnet_with_db := compose + " run --rm sdk dotnet"

# List available recipes.
default:
    @just --list

# ---------------------------------------------------------------------------
# dev — local runtime lifecycle
# ---------------------------------------------------------------------------

# Open lazysql pointed at the local Docker SQL Server.
[group('dev')]
db: db-up
    lazysql -config .lazysql.toml

# Start the database + web watcher and print the dev dashboard.
[group('dev')]
dev: up
    {{ dotnet_with_db }} run --project {{ tooling_project }} -- dev-dashboard

# Start the database + web watcher in the background and return.
[group('dev')]
up: db-up
    {{ compose }} up --detach web

# Follow the web app logs.
[group('dev')]
logs:
    {{ compose }} logs --follow web

# Restart local containers.
[group('dev')]
restart: down up

# Stop and remove local containers.
[group('dev')]
down:
    {{ compose }} down --remove-orphans

# Show local container status.
[group('dev')]
ps:
    {{ compose }} ps

# Open a shell in a container: `just shell` (disposable sdk), `web`, or `db`.
[group('dev')]
shell where="sdk": cache-dirs
    #!/usr/bin/env bash
    set -euo pipefail
    case '{{ where }}' in
        sdk) {{ compose }} run --rm --no-deps sdk sh ;;
        web) {{ compose }} exec web sh ;;
        db)  {{ compose }} exec db bash ;;
        *)   echo "unknown shell target '{{ where }}' (use: sdk | web | db)" >&2; exit 2 ;;
    esac

# ---------------------------------------------------------------------------
# test — build and tests
# ---------------------------------------------------------------------------

# Build the solution (Release) in the SDK container.
[group('test')]
build: restore
    {{ dotnet }} build {{ solution }} --configuration Release --no-restore

# Run unit + component tests. Optional FQN filter: `just test CheckoutTotals`.
[group('test')]
test filter="": build
    #!/usr/bin/env bash
    set -euo pipefail
    args=(--configuration Release --no-build)
    if [ -n '{{ filter }}' ]; then
        args+=(--filter "FullyQualifiedName~{{ filter }}")
    fi
    {{ dotnet }} test {{ unit_tests }} "${args[@]}"
    {{ dotnet }} test {{ component_tests }} "${args[@]}"

# Run the containerized Playwright + Lighthouse suite against the production app image.
[group('test')]
e2e: cache-dirs db-up
    {{ compose }} up --detach --build app
    {{ compose }} run --rm e2e

# ---------------------------------------------------------------------------
# check — quality gates
# ---------------------------------------------------------------------------

# Fast quality gate: build, tests, format check, infra (no E2E).
[group('check')]
check: test fmt-check infra

# Full quality gate including E2E. Required for task sign-off.
[group('check')]
check-all: check e2e

# Auto-format C# and OpenTofu.
[group('check')]
fmt: restore
    {{ dotnet }} format {{ solution }}
    {{ tofu }} -chdir=infra/opentofu fmt -recursive

# Verify repo toolchain version pins agree.
[group('check')]
toolchain-check: cache-dirs
    {{ dotnet }} run --project {{ tooling_project }} -- check-toolchain

# Check OpenTofu formatting and validate the configuration.
[group('check')]
infra: infra-init
    {{ tofu }} -chdir=infra/opentofu fmt -check -recursive
    {{ tofu }} -chdir=infra/opentofu validate

# ---------------------------------------------------------------------------
# database — SQL Server + EF Core migrations
# ---------------------------------------------------------------------------

# Start SQL Server in the background.
[group('database')]
db-up: cache-dirs
    {{ compose }} up --detach db

# Apply EF Core migrations to the local checkout database.
[group('database')]
db-update: db-up tools-restore
    {{ dotnet_with_db }} tool run dotnet-ef database update --project {{ web_project }} --startup-project {{ web_project }}

# Add an EF Core migration: `just db-migrate AddThing`.
[group('database')]
db-migrate name: db-up tools-restore
    {{ dotnet_with_db }} tool run dotnet-ef migrations add {{ name }} --project {{ web_project }} --startup-project {{ web_project }} --output-dir Data/Migrations

# Generate an idempotent SQL migration script.
[group('database')]
db-script: db-up tools-restore
    {{ dotnet_with_db }} tool run dotnet-ef migrations script --idempotent --project {{ web_project }} --startup-project {{ web_project }}

# ---------------------------------------------------------------------------
# housekeeping
# ---------------------------------------------------------------------------

# Remove build/test artifacts. `just clean all` also runs `git clean -fdX`.
[group('housekeeping')]
clean scope="":
    #!/usr/bin/env bash
    set -euo pipefail
    find ./src ./tests ./tools -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
    rm -rf TestResults coverage playwright-report artifacts
    if [ '{{ scope }}' = all ]; then
        git clean -fdX -e .env -e .docker-cache/ -e .vscode/ -e .idea/ -e .cursor/ -e .zed/ -e .claude/
    fi

# Check NuGet package freshness in the SDK container.
[group('housekeeping')]
outdated: tools-restore
    {{ dotnet }} tool run dotnet-outdated

# ---------------------------------------------------------------------------
# private plumbing (runnable, hidden from `just --list`)
# ---------------------------------------------------------------------------

[private]
cache-dirs:
    mkdir -p .docker-cache/dotnet-home .docker-cache/e2e-home .docker-cache/nuget .docker-cache/pnpm-home .docker-cache/pnpm-store

[private]
restore: toolchain-check
    {{ dotnet }} restore {{ solution }}

[private]
fmt-check: restore
    {{ dotnet }} format {{ solution }} --verify-no-changes

[private]
tools-restore: cache-dirs
    {{ dotnet }} tool restore

[private]
infra-init:
    {{ tofu }} -chdir=infra/opentofu init -backend=false
