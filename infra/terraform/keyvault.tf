# ── Current caller identity (needed for initial Key Vault RBAC) ──────────────
data "azurerm_client_config" "current" {}

# ── Key Vault ─────────────────────────────────────────────────────────────────
resource "azurerm_key_vault" "main" {
  name                       = local.kv_name
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  tenant_id                  = var.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  purge_protection_enabled   = false # allow destroy in non-prod bootstrap scenarios
  soft_delete_retention_days = 7

  tags = local.tags
}

# ── Key Vault RBAC: Terraform CI/CD principal (bootstrap) ────────────────────
# The service principal running Terraform needs to write secrets during apply.
resource "azurerm_role_assignment" "kv_admin_cicd" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets Officer"
  principal_id         = data.azurerm_client_config.current.object_id
}

# ── Key Vault RBAC: Container App managed identity (runtime reads) ────────────
resource "azurerm_role_assignment" "kv_secrets_user_app" {
  scope                = azurerm_key_vault.main.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_user_assigned_identity.app.principal_id
}

# ── Secrets ───────────────────────────────────────────────────────────────────

# Entra client secret — written by Terraform, read by the container app at startup.
resource "azurerm_key_vault_secret" "app_client_secret" {
  name         = "AzureAd--ClientSecret"
  value        = azuread_application_password.api.value
  key_vault_id = azurerm_key_vault.main.id
  content_type = "text/plain"

  depends_on = [azurerm_role_assignment.kv_admin_cicd]
}

# GitHub PAT (read:packages) — used by the Container App to pull images from GHCR.
resource "azurerm_key_vault_secret" "ghcr_pat" {
  name         = "ghcr-pat"
  value        = var.ghcr_pat
  key_vault_id = azurerm_key_vault.main.id
  content_type = "text/plain"

  depends_on = [azurerm_role_assignment.kv_admin_cicd]
}
