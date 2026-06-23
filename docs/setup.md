# Setup

This repo supports native Windows, macOS, and Linux development. WSL is useful
if you prefer a Linux environment on Windows, but it is not required for the
normal .NET workflow.

## Prerequisites

- [`mise`](https://mise.jdx.dev/installing-mise.html) for installing the
  pinned SDK and task runner.
- Podman plus `podman-compose`, or Docker with Docker Compose, for
  containerized app and E2E workflows:
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

The checkout workflow is served at `/cart`. The hosted app requires SQL Server;
`just up` and `just run` both start the configured SQL Server service and run
the web app in the .NET SDK container through `compose.yaml`.

## SQL Server Catalogue

The app includes EF Core SQL Server models and migrations for Products and
Offers. The default local container settings are:

```dotenv
SQL_SERVER_IMAGE=mcr.microsoft.com/mssql/server:2022-latest
SQL_HOST_PORT=14333
ConnectionStrings__CheckoutDatabase="Server=127.0.0.1,14333;Database=BrightPayTakeHome;User Id=sa;Password=BrightPay_takehome_Passw0rd!;TrustServerCertificate=True"
```

`just up` starts the Compose services and the app applies migrations at startup.
`just db-update` is available when applying migrations manually; it reads
`ConnectionStrings__CheckoutDatabase` from `.env` or the shell. Cart contents are
session workflow state and are not stored in the database in D1.

## Container Runtime

Podman is the repo default. On Arch, install both packages:

```bash
sudo pacman -S podman podman-compose
```

The default runtime setting is:

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
- `just up` runs `dotnet watch` inside the .NET SDK container using Compose and
  the selected container runtime.
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
