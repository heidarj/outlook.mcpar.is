# ── Microsoft Graph data source ──────────────────────────────────────────────
# Look up the well-known Graph service principal so we can reference delegated
# permission scope IDs by name instead of hardcoding GUIDs.
data "azuread_service_principal" "msgraph" {
  client_id = "00000003-0000-0000-c000-000000000000"
}

locals {
  # Resolve delegated scope IDs from the live Graph service principal.
  graph_scopes = { for s in data.azuread_service_principal.msgraph.oauth2_permission_scopes : s.value => s.id }
}

# ── Random UUID for the exposed API scope ────────────────────────────────────
resource "random_uuid" "api_scope" {}

# ── Application registration ─────────────────────────────────────────────────
resource "azuread_application" "api" {
  display_name                   = var.entra_app_display_name
  sign_in_audience               = "AzureADandPersonalMicrosoftAccount"
  fallback_public_client_enabled = true

  web {
    redirect_uris = [
      "https://vscode.dev/redirect",
      "https://claude.ai/api/mcp/auth_callback",
    ]
  }

  public_client {
    redirect_uris = [
      "http://127.0.0.1:33418",
    ]
  }

  # Expose a single delegated scope that client apps request.
  api {
    requested_access_token_version = 2

    oauth2_permission_scope {
      admin_consent_description  = "Access the outlook.mcpar.is MCP API on behalf of the signed-in user"
      admin_consent_display_name = "Access outlook.mcpar.is"
      enabled                    = true
      id                         = random_uuid.api_scope.result
      type                       = "User"
      user_consent_description   = "Access the outlook.mcpar.is MCP API on your behalf"
      user_consent_display_name  = "Access outlook.mcpar.is"
      value                      = "access_as_user"
    }
  }

  # Delegated Microsoft Graph permissions required for the OBO flow.
  required_resource_access {
    resource_app_id = "00000003-0000-0000-c000-000000000000" # Microsoft Graph

    dynamic "resource_access" {
      for_each = [
        "User.Read",
        "Mail.Read",
        "Calendars.Read",
        "Contacts.Read",
        "MailboxSettings.Read",
      ]
      content {
        id   = local.graph_scopes[resource_access.value]
        type = "Scope"
      }
    }
  }
}

# Set the Application ID URI after the app exists so we can safely use the
# generated client ID without creating a self-reference in the application
# resource itself.
resource "azuread_application_identifier_uri" "api" {
  application_id = azuread_application.api.id
  identifier_uri = "api://${azuread_application.api.client_id}"
}

# ── Service principal ─────────────────────────────────────────────────────────
resource "azuread_service_principal" "api" {
  client_id = azuread_application.api.client_id
}

# ── Client secret (stored in Key Vault; see keyvault.tf) ─────────────────────
resource "azuread_application_password" "api" {
  application_id = azuread_application.api.id
  display_name   = "terraform-managed"
  end_date       = timeadd(timestamp(), local.client_secret_ttl) # re-apply to rotate

  lifecycle {
    # Prevent Terraform from rotating the secret on every apply due to
    # timestamp() being re-evaluated each run.
    ignore_changes = [end_date]
  }
}
