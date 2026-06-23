output "resource_group_name" {
  description = "Azure resource group containing the take-home deployment."
  value       = azurerm_resource_group.this.name
}

output "container_app_name" {
  description = "Azure Container Apps app name."
  value       = azurerm_container_app.web.name
}

output "container_app_latest_revision_url" {
  description = "HTTPS URL for the latest active container app revision."
  value       = "https://${azurerm_container_app.web.latest_revision_fqdn}"
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of the Azure SQL server."
  value       = azurerm_mssql_server.this.fully_qualified_domain_name
}

output "checkout_connection_string" {
  description = "Checkout database connection string, already injected into the container app as a secret."
  value       = local.checkout_connection_string
  sensitive   = true
}
