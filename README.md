# BrightPay Take-Home

.NET 10 Blazor checkout app for the BrightPay take-home exercise.

`scratch/` is a series of stream-of-consciousness brainstorming files that I usually wouldn't document or stage, but thought might be useful to help explain thought processes behind choices.

## Prerequisites

- [`mise`](https://mise.jdx.dev/getting-started.html) for devtool versioning;
- [Docker](https://docs.docker.com/get-started/get-docker/) with Docker
  Compose, or [Podman](https://podman.io/docs/installation) with
  `podman-compose`.

## Usage

```bash
git clone git@github.com:plethu/brightpay-take-home.git
cd take-home
mise trust
mise install
cp .env.example .env
just up
```

`just up` starts SQL Server and the Blazor app, clears the terminal, and prints some helpers for development.
It leaves `dotnet watch` running in the web container. Use `just watch` when
you want to keep the foreground terminal attached to the watcher logs.

Docker is the default local container runtime. To use Podman instead, set
`CONTAINER_RUNTIME=podman` in `.env` after confirming `podman-compose` works on
your machine.

The local SQL password is controlled by `SQL_SERVER_PASSWORD`; the Docker
connection strings and host EF commands derive from that value unless
`ConnectionStrings__CheckoutDatabase` is explicitly set.

Run `just` to list the available commands.
