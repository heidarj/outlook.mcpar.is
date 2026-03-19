provider "azurerm" {
  subscription_id = var.subscription_id

  features {
    key_vault {
      # Keep soft-deleted Key Vaults (name still occupies the namespace for
      # the retention period) so an accidental destroy does not lose secrets.
      purge_soft_delete_on_destroy    = false
      recover_soft_deleted_key_vaults = true
    }
  }
}

provider "azuread" {
  tenant_id = var.tenant_id
}
