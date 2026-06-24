set dotenv-load
set default-list

solution := "BrightPay.TakeHome.slnx"
web_project := "src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj"
unit_tests := "tests/BrightPay.TakeHome.Tests.Unit/BrightPay.TakeHome.Tests.Unit.csproj"
component_tests := "tests/BrightPay.TakeHome.Tests.Components/BrightPay.TakeHome.Tests.Components.csproj"
e2e_project := "tests/BrightPay.TakeHome.Tests.E2E/BrightPay.TakeHome.Tests.E2E.csproj"
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

export DOTNET_CLI_HOME := ".dotnet"
export DOTNET_CLI_TELEMETRY_OPTOUT := "1"
export DOTNET_NOLOGO := "1"
export ConnectionStrings__CheckoutDatabase := host_checkout_connection
export SQL_SERVER_PASSWORD := sql_server_password

# Verify that repo toolchain pins agree.
toolchain-check:
    dotnet run --project {{ tooling_project }} -- check-toolchain

# Restore NuGet packages.
restore: toolchain-check
    dotnet restore {{ solution }}

# Compile collocated checkout TypeScript assets.
frontend-build:
    pnpm --package=typescript@6.0.3 dlx tsc --project tsconfig.json

# Build the solution in Release mode.
build: restore frontend-build
    dotnet build {{ solution }} --configuration Release --no-restore

# Run unit tests.
test-unit: build
    dotnet test {{ unit_tests }} --configuration Release --no-build

# Run unit tests matching a class, method, or namespace substring.
test-unit-filter filter: build
    dotnet test {{ unit_tests }} --configuration Release --no-build --filter "FullyQualifiedName~{{ filter }}"

# Run bUnit component tests.
test-components: build
    dotnet test {{ component_tests }} --configuration Release --no-build

# Run bUnit component tests matching a class, method, or namespace substring.
test-components-filter filter: build
    dotnet test {{ component_tests }} --configuration Release --no-build --filter "FullyQualifiedName~{{ filter }}"

# Run containerized Playwright E2E tests.
test-e2e: db-up
    {{ compose }} up --detach --build app
    {{ compose }} run --rm e2e

# Run E2E tests against E2E_BASE_URL.
test-e2e-host:
    dotnet test {{ e2e_project }} --configuration Release --filter "Category=E2E"

# Run E2E tests against E2E_BASE_URL matching a class, method, or namespace substring.
test-e2e-host-filter filter:
    dotnet test {{ e2e_project }} --configuration Release --filter "Category=E2E&FullyQualifiedName~{{ filter }}"

# Run all tests.
test: test-unit test-components test-e2e

# Restore local .NET tools inside the running web container.
tools-restore-web:
    {{ compose }} exec web dotnet tool restore

# Apply EF Core migrations to the local checkout database.
db-update:
    dotnet tool run dotnet-ef database update --project {{ web_project }} --startup-project {{ web_project }}

# Add an EF Core migration on the host.
db-migration name:
    dotnet tool run dotnet-ef migrations add {{ name }} --project {{ web_project }} --startup-project {{ web_project }} --output-dir Data/Migrations

# Generate an idempotent SQL migration script.
db-script:
    dotnet tool run dotnet-ef migrations script --idempotent --project {{ web_project }} --startup-project {{ web_project }}

# Apply EF Core migrations inside the running web container.
db-update-web: tools-restore-web
    {{ compose }} exec web dotnet tool run dotnet-ef database update --project {{ web_project }} --startup-project {{ web_project }}

# Add an EF Core migration inside the running web container.
db-migration-web name: tools-restore-web
    {{ compose }} exec web dotnet tool run dotnet-ef migrations add {{ name }} --project {{ web_project }} --startup-project {{ web_project }} --output-dir Data/Migrations

# Generate an idempotent SQL migration script inside the running web container.
db-script-web: tools-restore-web
    {{ compose }} exec web dotnet tool run dotnet-ef migrations script --idempotent --project {{ web_project }} --startup-project {{ web_project }}

# Run unit tests inside the running web container.
test-unit-web:
    {{ compose }} exec web dotnet test {{ unit_tests }} --configuration Release

# Run unit tests inside the running web container, filtered by substring.
test-unit-filter-web filter:
    {{ compose }} exec web dotnet test {{ unit_tests }} --configuration Release --filter "FullyQualifiedName~{{ filter }}"

# Run bUnit component tests inside the running web container.
test-components-web:
    {{ compose }} exec web dotnet test {{ component_tests }} --configuration Release

# Run bUnit component tests inside the running web container, filtered by substring.
test-components-filter-web filter:
    {{ compose }} exec web dotnet test {{ component_tests }} --configuration Release --filter "FullyQualifiedName~{{ filter }}"

# Verify .NET formatting.
fmt: restore
    dotnet format {{ solution }} --verify-no-changes

# Apply .NET formatting.
fmt-fix:
    dotnet format {{ solution }}

# Run the full quality gate.
check: test fmt infra-check

# Initialize OpenTofu without a backend.
infra-init:
    {{ tofu }} -chdir=infra/opentofu init -backend=false

# Verify OpenTofu formatting.
infra-fmt:
    {{ tofu }} -chdir=infra/opentofu fmt -check -recursive

# Apply OpenTofu formatting.
infra-fmt-fix:
    {{ tofu }} -chdir=infra/opentofu fmt -recursive

# Validate OpenTofu configuration.
infra-validate: infra-init
    {{ tofu }} -chdir=infra/opentofu validate

# Run OpenTofu formatting and validation checks.
infra-check: infra-fmt infra-validate

# Run host-side checks without containerized E2E.
check-host: test-unit test-components fmt infra-check

# Start the local development runtime.
run: up

# Start SQL Server.
db-up:
    {{ compose }} up --detach db

# Start SQL Server and the web app, then print local development links.
up: down db-up
    {{ compose }} up --detach web
    dotnet run --project {{ tooling_project }} -- dev-dashboard

# Start the development runtime and follow the web watcher logs.
watch: up
    {{ compose }} logs --follow web

# Start SQL Server and the web app in the background.
up-detached: down db-up
    {{ compose }} up --detach web
    dotnet run --project {{ tooling_project }} -- dev-dashboard

# Stop local containers.
down:
    {{ compose }} down --remove-orphans

# Restart the local development runtime.
restart: up

# Follow web app logs.
logs:
    {{ compose }} logs --follow web

# Open a shell in the web container.
shell: shell-web

# Open a shell in the web container.
shell-web:
    {{ compose }} exec web sh

# Open a shell in the SQL Server container.
shell-db:
    {{ compose }} exec db bash

# Show local container status.
ps:
    {{ compose }} ps

# Remove generated build and test artifacts.
clean:
    dotnet clean {{ solution }} --configuration Debug
    dotnet clean {{ solution }} --configuration Release
    git clean -fdX -e .env -e .dotnet/ -e .nuget/ -e .vscode/ -e .idea/ -e .cursor/ -e .zed/ -e .claude/

# Check NuGet package freshness.
outdated:
    dotnet tool restore
    dotnet outdated
