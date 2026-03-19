locals {
  # Canonical short prefix used in every resource name.
  prefix = "${var.app_name}-${var.environment}"

  # Resource-type prefixes follow the Microsoft CAF abbreviations.
  rg_name  = "rg-${local.prefix}"
  kv_name  = "kv-${substr(local.prefix, 0, 15)}-${random_string.suffix.result}"
  id_name  = "id-${local.prefix}"
  cae_name = "cae-${local.prefix}"
  ca_name  = "ca-${local.prefix}-api"

  # Entra client secret TTL expressed as a duration string accepted by timeadd().
  # One year is the practical maximum for a client secret in Microsoft Entra.
  client_secret_ttl = "8760h"

  # Number of concurrent HTTP requests at which the Container App scales out.
  # 10 is a conservative starting value for a low-traffic API; adjust upward
  # if requests are consistently short-lived and latency is acceptable.
  http_scale_concurrent_requests = "10"

  # Common tags applied to every resource.
  tags = {
    application = var.app_name
    environment = var.environment
    managed_by  = "terraform"
  }
}

resource "random_string" "suffix" {
  length  = 5
  upper   = false
  special = false
  # Key Vault names must be globally unique; a short random suffix makes
  # repeated destroys and re-creates safe without a naming collision.
}
