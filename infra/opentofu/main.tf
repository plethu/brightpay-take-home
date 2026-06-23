locals {
  resource_prefix = lower(replace(var.name_prefix, "_", "-"))
  common_tags = merge(
    {
      app         = "brightpay-takehome"
      environment = var.environment
      managed-by  = "opentofu"
    },
    var.tags,
  )
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${local.resource_prefix}-${var.environment}"
  location = var.location
  tags     = local.common_tags
}

resource "azurerm_log_analytics_workspace" "this" {
  count = var.enable_log_analytics ? 1 : 0

  name                = "log-${local.resource_prefix}-${var.environment}"
  location            = azurerm_resource_group.this.location
  resource_group_name = azurerm_resource_group.this.name
  retention_in_days   = 30
  sku                 = "PerGB2018"
  tags                = local.common_tags
}

resource "azurerm_container_app_environment" "this" {
  name                       = "cae-${local.resource_prefix}-${var.environment}"
  location                   = azurerm_resource_group.this.location
  resource_group_name        = azurerm_resource_group.this.name
  log_analytics_workspace_id = var.enable_log_analytics ? azurerm_log_analytics_workspace.this[0].id : null
  tags                       = local.common_tags
}

resource "azurerm_container_app" "web" {
  name                         = "ca-${local.resource_prefix}-${var.environment}"
  container_app_environment_id = azurerm_container_app_environment.this.id
  resource_group_name          = azurerm_resource_group.this.name
  revision_mode                = "Single"
  tags                         = local.common_tags

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 0
    max_replicas = 1

    container {
      name   = "web"
      image  = var.container_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "DisableHttpsRedirection"
        value = "true"
      }
    }
  }
}
