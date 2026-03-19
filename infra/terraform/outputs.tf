output "resource_group_name" {
  description = "Name of the Azure resource group."
  value       = azurerm_resource_group.main.name
}

output "container_app_name" {
  description = "Name of the Azure Container App."
  value       = azurerm_container_app.api.name
}

output "container_app_fqdn" {
  description = "Public FQDN of the Container App ingress."
  value       = azurerm_container_app.api.ingress[0].fqdn
}

output "key_vault_uri" {
  description = "URI of the Azure Key Vault."
  value       = azurerm_key_vault.main.vault_uri
}

output "entra_client_id" {
  description = "Entra application (client) ID — used in AzureAd__ClientId app config."
  value       = azuread_application.api.client_id
}

output "entra_app_id_uri" {
  description = "Application ID URI — used as the Audience value (api://<client_id>)."
  value       = "api://${azuread_application.api.client_id}"
}

output "managed_identity_client_id" {
  description = "Client ID of the user-assigned managed identity attached to the Container App."
  value       = azurerm_user_assigned_identity.app.client_id
}
