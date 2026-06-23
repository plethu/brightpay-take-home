# OpenTofu Azure Deployment

This directory contains an optional Azure Container Apps target for a published
container image. Container Apps is a better fit for the current Blazor Web App
shape than Static Web Apps because the app is server-hosted.

Static Web Apps remains the cheaper fallback if the spec can be delivered as a
static Blazor WebAssembly app plus serverless APIs.

## Commands

```bash
just infra-fmt
just infra-validate
```

`just infra-validate` runs `tofu init -backend=false` and does not require Azure
credentials. `tofu plan` and `tofu apply` require Azure authentication and a
container image value:

```bash
cp terraform.tfvars.example terraform.tfvars
tofu -chdir=infra/opentofu plan
```

Do not commit `terraform.tfvars`, state files, plans, or cloud credentials.

## Cost Notes

The default shape is intentionally small: one resource group, one Container Apps
environment, and one container app with `min_replicas = 0` and `max_replicas =
1`. Log Analytics is disabled by default to keep the scaffold cost-conscious;
turn it on only if deployment diagnostics matter for the submitted demo.
