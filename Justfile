set dotenv-load := true

solution := "BrightPay.TakeHome.slnx"
web_project := "src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj"
unit_tests := "tests/BrightPay.TakeHome.Tests.Unit/BrightPay.TakeHome.Tests.Unit.csproj"
component_tests := "tests/BrightPay.TakeHome.Tests.Components/BrightPay.TakeHome.Tests.Components.csproj"
e2e_project := "tests/BrightPay.TakeHome.Tests.E2E/BrightPay.TakeHome.Tests.E2E.csproj"
tooling_project := "tools/BrightPay.TakeHome.Tooling/BrightPay.TakeHome.Tooling.csproj"
container_runtime := env_var_or_default("CONTAINER_RUNTIME", "podman")
tofu := env_var_or_default("TOFU", "mise exec -- tofu")
sdk_image := env_var_or_default("DOTNET_SDK_IMAGE", "mcr.microsoft.com/dotnet/sdk:10.0")
dev_container := env_var_or_default("DEV_CONTAINER_NAME", "brightpay-takehome-web")
app_host_port := env_var_or_default("APP_HOST_PORT", "8080")
aspnetcore_environment := env_var_or_default("ASPNETCORE_ENVIRONMENT", "Development")
repo_dir := justfile_directory()

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

test-e2e:
    {{container_runtime}} build -f Dockerfile.e2e -t brightpay-takehome-e2e:local .
    {{container_runtime}} run --rm brightpay-takehome-e2e:local

test-e2e-host:
    dotnet test {{e2e_project}} --configuration Release --filter "Category=E2E"

test: test-unit test-components test-e2e

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

run:
    dotnet watch --project {{web_project}} run

up: down
    {{container_runtime}} run --rm --name {{dev_container}} --publish {{app_host_port}}:8080 --env ASPNETCORE_ENVIRONMENT={{aspnetcore_environment}} --env DOTNET_CLI_HOME=/tmp/dotnet-home --env DOTNET_CLI_TELEMETRY_OPTOUT=1 --env DOTNET_NOLOGO=1 --env DOTNET_USE_POLLING_FILE_WATCHER=1 --env DisableHttpsRedirection=true --volume "{{repo_dir}}:/workspace" --volume brightpay-takehome-nuget:/workspace/.nuget/packages --volume brightpay-takehome-dotnet-home:/tmp/dotnet-home --workdir /workspace {{sdk_image}} dotnet watch --project {{web_project}} run --no-launch-profile --urls http://0.0.0.0:8080

up-detached: down
    {{container_runtime}} run --detach --name {{dev_container}} --publish {{app_host_port}}:8080 --env ASPNETCORE_ENVIRONMENT={{aspnetcore_environment}} --env DOTNET_CLI_HOME=/tmp/dotnet-home --env DOTNET_CLI_TELEMETRY_OPTOUT=1 --env DOTNET_NOLOGO=1 --env DOTNET_USE_POLLING_FILE_WATCHER=1 --env DisableHttpsRedirection=true --volume "{{repo_dir}}:/workspace" --volume brightpay-takehome-nuget:/workspace/.nuget/packages --volume brightpay-takehome-dotnet-home:/tmp/dotnet-home --workdir /workspace {{sdk_image}} dotnet watch --project {{web_project}} run --no-launch-profile --urls http://0.0.0.0:8080

down:
    -{{container_runtime}} rm --force {{dev_container}}

restart: down
    {{container_runtime}} run --rm --name {{dev_container}} --publish {{app_host_port}}:8080 --env ASPNETCORE_ENVIRONMENT={{aspnetcore_environment}} --env DOTNET_CLI_HOME=/tmp/dotnet-home --env DOTNET_CLI_TELEMETRY_OPTOUT=1 --env DOTNET_NOLOGO=1 --env DOTNET_USE_POLLING_FILE_WATCHER=1 --env DisableHttpsRedirection=true --volume "{{repo_dir}}:/workspace" --volume brightpay-takehome-nuget:/workspace/.nuget/packages --volume brightpay-takehome-dotnet-home:/tmp/dotnet-home --workdir /workspace {{sdk_image}} dotnet watch --project {{web_project}} run --no-launch-profile --urls http://0.0.0.0:8080

logs:
    {{container_runtime}} logs --follow {{dev_container}}

shell:
    {{container_runtime}} exec --interactive --tty {{dev_container}} sh

ps:
    {{container_runtime}} ps --filter name={{dev_container}}

clean:
    dotnet clean {{solution}} --configuration Debug
    dotnet clean {{solution}} --configuration Release
    git clean -fdX -e .env -e .dotnet/ -e .nuget/ -e .vscode/ -e .idea/ -e .cursor/ -e .zed/ -e .claude/

image-build:
    {{container_runtime}} build -t brightpay-takehome:local .

outdated:
    dotnet tool restore
    dotnet outdated
