# BrightPay Take-Home

.NET 10 Blazor checkout app for the BrightPay take-home exercise.

`scratch/` is a series of stream-of-consciousness brainstorming files that I usually wouldn't document or stage, but thought might be useful to help explain thought processes behind choices.

## Prerequisites

- [`mise`](https://mise.jdx.dev/getting-started.html) for devtool versioning;
- [Podman](https://podman.io/docs/installation) with `podman-compose`, or
  [Docker](https://docs.docker.com/get-started/get-docker/) with Docker
  Compose.

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

Run `just` to list the available commands.
