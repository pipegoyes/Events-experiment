output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "container_registry_login_server" {
  description = "Login server for the Azure Container Registry"
  value       = data.azurerm_container_registry.acr.login_server
}

output "container_registry_username" {
  description = "Admin username for ACR"
  value       = data.azurerm_container_registry.acr.admin_username
  sensitive   = true
}

output "container_registry_password" {
  description = "Admin password for ACR"
  value       = data.azurerm_container_registry.acr.admin_password
  sensitive   = true
}

output "api_url" {
  description = "URL of the Box Tracking API"
  value       = "https://${azurerm_container_app.api.ingress[0].fqdn}"
}

output "dashboard_url" {
  description = "URL of the Dashboard"
  value       = "https://${azurerm_container_app.dashboard.ingress[0].fqdn}"
}

output "simulator_url" {
  description = "URL of the Event Simulator"
  value       = "https://${azurerm_container_app.simulator.ingress[0].fqdn}"
}

output "swagger_url" {
  description = "URL of the API Swagger documentation"
  value       = "https://${azurerm_container_app.api.ingress[0].fqdn}/swagger"
}

output "container_apps_environment_id" {
  description = "ID of the Container Apps Environment"
  value       = azurerm_container_app_environment.main.id
}
