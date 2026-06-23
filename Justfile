set dotenv-load := true

solution := "BrightPay.TakeHome.slnx"
web_project := "src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj"
unit_tests := "tests/BrightPay.TakeHome.Tests.Unit/BrightPay.TakeHome.Tests.Unit.csproj"
component_tests := "tests/BrightPay.TakeHome.Tests.Components/BrightPay.TakeHome.Tests.Components.csproj"
e2e_project := "tests/BrightPay.TakeHome.Tests.E2E/BrightPay.TakeHome.Tests.E2E.csproj"
tooling_project := "tools/BrightPay.TakeHome.Tooling/BrightPay.TakeHome.Tooling.csproj"
container_runtime := env_var_or_default("CONTAINER_RUNTIME", "podman")
compose := if container_runtime == "docker" { "docker compose" } else { "podman-compose" }
tofu := env_var_or_default("TOFU", "mise exec -- tofu")

export DOTNET_CLI_HOME := ".dotnet"
export DOTNET_CLI_TELEMETRY_OPTOUT := "1"
export DOTNET_NOLOGO := "1"

default: check

toolchain-check:
    dotnet run --project {{tooling_project}} -- check-toolchain

restore: toolchain-check
    dotnet restore {{solution}}

build: restore
    dotnet build {{solution}} --configuration Release --no-restore

test-unit: build
    dotnet test {{unit_tests}} --configuration Release --no-build

test-components: build
    dotnet test {{component_tests}} --configuration Release --no-build

test-e2e: db-up
    {{compose}} run --rm e2e

test-e2e-host:
    dotnet test {{e2e_project}} --configuration Release --filter "Category=E2E"

test: test-unit test-components test-e2e

db-update:
    dotnet tool run dotnet-ef database update --project {{web_project}} --startup-project {{web_project}}

db-script:
    dotnet tool run dotnet-ef migrations script --idempotent --project {{web_project}} --startup-project {{web_project}}

fmt: restore
    dotnet format {{solution}} --verify-no-changes

fmt-fix:
    dotnet format {{solution}}

check: test fmt infra-check

infra-init:
    {{tofu}} -chdir=infra/opentofu init -backend=false

infra-fmt:
    {{tofu}} -chdir=infra/opentofu fmt -check -recursive

infra-fmt-fix:
    {{tofu}} -chdir=infra/opentofu fmt -recursive

infra-validate: infra-init
    {{tofu}} -chdir=infra/opentofu validate

infra-check: infra-fmt infra-validate

check-host: test-unit test-components fmt infra-check

run: up

db-up:
    {{compose}} up --detach db

up: down db-up
    {{compose}} up web

up-detached: down db-up
    {{compose}} up --detach web

down:
    {{compose}} down --remove-orphans

restart: up

logs:
    {{compose}} logs --follow web

shell:
    {{compose}} exec web sh

ps:
    {{compose}} ps

clean:
    dotnet clean {{solution}} --configuration Debug
    dotnet clean {{solution}} --configuration Release
    git clean -fdX -e .env -e .dotnet/ -e .nuget/ -e .vscode/ -e .idea/ -e .cursor/ -e .zed/ -e .claude/

outdated:
    dotnet tool restore
    dotnet outdated
