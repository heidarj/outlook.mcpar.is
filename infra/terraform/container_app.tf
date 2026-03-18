# ── Container Apps environment ────────────────────────────────────────────────
# Log Analytics workspace is intentionally omitted to keep costs minimal.
# Basic live/stream logs are still accessible via the Azure portal and az CLI.
resource "azurerm_container_app_environment" "main" {
  name                = local.cae_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  tags                = local.tags
}

# ── Container App ─────────────────────────────────────────────────────────────
resource "azurerm_container_app" "api" {
  name                         = local.ca_name
  resource_group_name          = azurerm_resource_group.main.name
  container_app_environment_id = azurerm_container_app_environment.main.id
  revision_mode                = "Single"
  tags                         = local.tags

  # Attach the managed identity so secrets can be fetched from Key Vault.
  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app.id]
  }

  # GHCR registry credentials pulled from Key Vault via the managed identity.
  registry {
    server               = "ghcr.io"
    username             = var.ghcr_username
    password_secret_name = "ghcr-pat"
  }

  # ── Secrets (sourced from Key Vault via managed identity) ────────────────
  secret {
    name                = "ghcr-pat"
    key_vault_secret_id = azurerm_key_vault_secret.ghcr_pat.versionless_id
    identity            = azurerm_user_assigned_identity.app.id
  }

  secret {
    name                = "azure-ad-client-secret"
    key_vault_secret_id = azurerm_key_vault_secret.app_client_secret.versionless_id
    identity            = azurerm_user_assigned_identity.app.id
  }

  # ── Template ─────────────────────────────────────────────────────────────
  template {
    # Scale to zero outside working hours; scale out under load up to 2 replicas.
    min_replicas = 0
    max_replicas = 2

    # Cron rule: keep 1 replica warm on weekday working hours (08:00–18:00 UTC).
    custom_scale_rule {
      name             = "weekday-working-hours"
      custom_rule_type = "cron"
      metadata = {
        timezone        = "UTC"
        start           = "0 8 * * 1-5"
        end             = "0 18 * * 1-5"
        desiredReplicas = "1"
      }
    }

    # HTTP rule: scale out when concurrent requests exceed the threshold.
    http_scale_rule {
      name                = "http-scaling"
      concurrent_requests = local.http_scale_concurrent_requests
    }

    container {
      name = "api"
      # Initial image; the deploy workflow updates this on every push to main.
      # Terraform ignores changes to the image tag after first apply (see lifecycle below).
      image  = "ghcr.io/${var.ghcr_username}/outlook-mcpar-is:latest"
      cpu    = 0.25
      memory = "0.5Gi"

      # ── Environment variables ───────────────────────────────────────────
      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
      env {
        name  = "ASPNETCORE_HTTP_PORTS"
        value = "8080"
      }
      env {
        name  = "AzureAd__TenantId"
        value = var.tenant_id
      }
      env {
        name  = "AzureAd__ClientId"
        value = azuread_application.api.client_id
      }
      env {
        name        = "AzureAd__ClientSecret"
        secret_name = "azure-ad-client-secret"
      }
      env {
        name  = "AzureAd__Audience"
        value = "api://${azuread_application.api.client_id}"
      }
      env {
        name  = "MicrosoftGraph__BaseUrl"
        value = "https://graph.microsoft.com/v1.0"
      }
      env {
        name  = "MicrosoftGraph__Scopes__0"
        value = "User.Read"
      }
      env {
        name  = "MicrosoftGraph__Scopes__1"
        value = "Mail.Read"
      }
      env {
        name  = "MicrosoftGraph__Scopes__2"
        value = "Calendars.Read"
      }
      env {
        name  = "MicrosoftGraph__Scopes__3"
        value = "Contacts.Read"
      }
      env {
        name  = "MicrosoftGraph__Scopes__4"
        value = "MailboxSettings.Read"
      }

      # ── Health probes ───────────────────────────────────────────────────
      liveness_probe {
        path      = "/health"
        port      = 8080
        transport = "HTTP"
      }

      readiness_probe {
        path      = "/health/ready"
        port      = 8080
        transport = "HTTP"
      }
    }
  }

  # ── Ingress ───────────────────────────────────────────────────────────────
  ingress {
    # Public HTTPS ingress. A custom domain can be added here later without
    # changing any other part of the configuration.
    allow_insecure_connections = false
    external_enabled           = true
    target_port                = 8080

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  # The deploy workflow owns the image tag; Terraform must not revert it on
  # every apply.
  lifecycle {
    ignore_changes = [
      template[0].container[0].image,
    ]
  }

  depends_on = [
    azurerm_role_assignment.kv_secrets_user_app,
  ]
}
