# Setup

This repo supports native Windows, macOS, and Linux development. WSL is useful
if you prefer a Linux environment on Windows, but it is not required for the
normal .NET workflow.

## Prerequisites

- [`mise`](https://mise.jdx.dev/installing-mise.html) for installing the
  pinned SDK and task runner.
- Podman or Docker for containerized app and E2E workflows:
  [Podman installation](https://podman.io/docs/installation) or
  [Get Docker](https://docs.docker.com/get-started/get-docker/).
- OpenTofu for optional infrastructure checks. It is pinned in `.mise.toml`;
  see the [OpenTofu install docs](https://opentofu.org/docs/intro/install/) if
  you prefer a direct install.

`mise install` reads `.mise.toml` and installs the pinned .NET SDK, `just`, and
OpenTofu versions for this repo. See the mise
[getting started guide](https://mise.jdx.dev/getting-started.html) for shell
activation and PATH setup.

Direct installs are also fine if preferred:
[.NET install docs](https://learn.microsoft.com/en-us/dotnet/core/install/)
and [`just` package docs](https://just.systems/man/en/packages.html).

`make` is only a Unix convenience shim over `just`. Windows users should call
`just` directly from PowerShell, cmd, or Windows Terminal.

## First Run

```bash
mise install
cp .env.example .env
just check-host
```

On Windows, create `.env` by copying `.env.example` in Explorer, PowerShell, or
your editor if `cp` is unavailable.

## Container Runtime

Podman is the repo default:

```dotenv
CONTAINER_RUNTIME=podman
DEV_CONTAINER_NAME=brightpay-takehome-web
APP_HOST_PORT=8080
```

Docker users can switch the runtime in `.env`:

```dotenv
CONTAINER_RUNTIME=docker
```

`just` loads `.env` through `set dotenv-load := true`, so these values do not
need to be repeated before every command.

OpenTofu commands default to `TOFU="mise exec -- tofu"` so they work even when
the shell has not activated mise shims. If you installed OpenTofu directly, set
`TOFU=tofu` in `.env`.

## Platform Notes

- The host path uses `dotnet` and `just`, not Bash-only scripts.
- `just up` runs `dotnet watch` inside the .NET SDK container using the selected
  container runtime directly.
- `just test-e2e` runs Playwright in the dedicated Linux container image.
- `just infra-validate` checks OpenTofu syntax and provider configuration
  without provisioning Azure resources.
- `scripts/run-e2e.sh` is intentionally Linux-only because it runs inside that
  E2E container, not on the developer host.
- WSL is optional on Windows. Docker Desktop and Podman Desktop may use WSL2 or
  a VM internally, but the repo does not require a WSL shell.

## SDK Pins

`global.json` cannot dynamically read `.mise.toml`, so the repo keeps the SDK
version explicit in both files. `just toolchain-check` verifies that the pins
match using a small .NET helper project.
