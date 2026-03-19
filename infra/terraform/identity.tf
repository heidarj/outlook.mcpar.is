resource "azurerm_user_assigned_identity" "app" {
  name                = local.id_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  tags                = local.tags
}
