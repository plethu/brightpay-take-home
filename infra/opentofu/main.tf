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

# --- Checkout database -------------------------------------------------------

resource "random_password" "sql_admin" {
  length = 24
  # SQL Server rejects a few specials in the admin password; keep a safe set.
  override_special = "!#%*-_"
  min_upper        = 1
  min_lower        = 1
  min_numeric      = 1
  min_special      = 1
}

resource "azurerm_mssql_server" "this" {
  name                          = "sql-${local.resource_prefix}-${var.environment}"
  resource_group_name           = azurerm_resource_group.this.name
  location                      = azurerm_resource_group.this.location
  version                       = "12.0"
  administrator_login           = var.sql_admin_login
  administrator_login_password  = random_password.sql_admin.result
  minimum_tls_version           = "1.2"
  public_network_access_enabled = true
  tags                          = local.common_tags
}

# Container Apps egress IPs are not fixed, so allow Azure-internal traffic. The
# 0.0.0.0 start/end is Azure's "allow Azure services" rule, not the public
# internet. Use a VNet and private endpoint for anything beyond a demo.
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.this.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Serverless tier with auto-pause: an idle demo costs storage only, and resumes
# on the next connection.
# TODO(D1): the database is created empty. Once EF migrations exist, the schema
# needs applying on deploy (startup MigrateAsync in the app, or a one-off init
# step) or the container will start against a database with no tables.
resource "azurerm_mssql_database" "checkout" {
  name                        = "BrightPayTakeHome"
  server_id                   = azurerm_mssql_server.this.id
  sku_name                    = "GP_S_Gen5_1"
  min_capacity                = 0.5
  auto_pause_delay_in_minutes = 60
  max_size_gb                 = 2
  collation                   = "SQL_Latin1_General_CP1_CI_AS"
  zone_redundant              = false
  tags                        = local.common_tags
}

locals {
  checkout_connection_string = join("", [
    "Server=tcp:${azurerm_mssql_server.this.fully_qualified_domain_name},1433;",
    "Database=${azurerm_mssql_database.checkout.name};",
    "User ID=${var.sql_admin_login};",
    "Password=${random_password.sql_admin.result};",
    "Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
  ])
}

# --- Web app -----------------------------------------------------------------

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

  secret {
    name  = "checkout-db-connection"
    value = local.checkout_connection_string
  }

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

      env {
        name        = "ConnectionStrings__CheckoutDatabase"
        secret_name = "checkout-db-connection"
      }
    }
  }
}
