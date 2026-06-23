# BrightPay Take-Home

.NET 10 Blazor checkout app for the BrightPay take-home exercise.

## Prerequisites

- [`mise`](https://mise.jdx.dev/getting-started.html) for the pinned .NET SDK,
  `just`, and OpenTofu versions.
- [Podman](https://podman.io/docs/installation) with `podman-compose`, or
  [Docker](https://docs.docker.com/get-started/get-docker/) with Docker
  Compose.

## Usage

```bash
git clone <repo-url>
cd take-home
mise trust
mise install
cp .env.example .env
just up
```

`just up` starts SQL Server and the Blazor app, clears the terminal, and prints
the local links and runtime commands for the development session.

Run `just` to list the available commands.
