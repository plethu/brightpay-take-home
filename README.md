# BrightPay Take-Home

.NET 10 Blazor checkout app for the BrightPay take-home exercise.

`scratch/` is a series of stream-of-consciousness brainstorming files that I usually wouldn't document or stage, but thought they might help show the reasoning behind some choices.

## Prerequisites

- [`mise`](https://mise.jdx.dev/getting-started.html) for devtool versioning
  (`dotnet`, `just`, OpenTofu, and `pnpm`);
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

Docker is the default local container runtime. To use Podman instead, set
`CONTAINER_RUNTIME=podman` in `.env` after confirming `podman-compose` works on
your machine.

Run `just` to list the available commands.

## Backend note

The backend keeps the suggested checkout shape, but does not implement the
sample `ICheckout` literally. The domain uses a typed `Sku`, NodaMoney `Money`
values, explicit operation results for expected validation failures, and a
checkout transaction constructed from the active pricing rules.
