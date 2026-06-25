# OpenTofu Azure Deployment

Optional Azure target for the take-home: a published container image on Azure
Container Apps, backed by Azure SQL. Not required to run or review the app
locally.

It provisions a resource group, a Container Apps environment and app, and a
serverless Azure SQL database. The database connection string is generated and
injected into the container as a secret.

## Commands

```bash
just infra   # check formatting + tofu init -backend=false + validate (no Azure credentials needed)
just fmt     # auto-format C# and OpenTofu
```

`tofu plan` and `tofu apply` need Azure authentication and a container image.
The `release` workflow publishes the image to GHCR on a `v*` tag; point
`container_image` at that tag.

```bash
cp terraform.tfvars.example terraform.tfvars   # set container_image
tofu -chdir=infra/opentofu plan
```

Do not commit `terraform.tfvars`, state files, plans, or credentials. State
holds the generated SQL password.

## Cost

Built to cost near nothing while idle:

- Container app scales to zero (`min_replicas = 0`, `max_replicas = 1`, 0.25
  vCPU / 0.5 GiB).
- SQL uses the serverless tier and auto-pauses after 60 minutes, so an unused
  demo pays for storage only. The first request after a pause waits for the
  database to resume.
- Log Analytics is off by default. Turn it on only if deployment diagnostics
  matter for the submitted demo.

## Security note

The SQL firewall uses Azure's "allow Azure services" rule so the container can
reach the database without fixed egress IPs. That is fine for a demo. Anything
real wants a VNet with a private endpoint.
