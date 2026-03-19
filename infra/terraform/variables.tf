variable "subscription_id" {
  description = "Azure subscription ID."
  type        = string
}

variable "tenant_id" {
  description = "Azure Active Directory tenant ID."
  type        = string
}

variable "location" {
  description = "Azure region for all resources."
  type        = string
  default     = "northeurope"
}

variable "environment" {
  description = "Short environment name used in resource names (e.g. prod, staging)."
  type        = string
  default     = "prod"
}

variable "app_name" {
  description = "Short application name used in resource names."
  type        = string
  default     = "outlook-mcp"
}

# ── Entra ────────────────────────────────────────────────────────────────────

variable "entra_app_display_name" {
  description = "Display name for the Entra application registration."
  type        = string
  default     = "outlook.mcpar.is API"
}

# ── GitHub / GHCR ────────────────────────────────────────────────────────────

variable "ghcr_username" {
  description = "GitHub username or organisation that owns the GHCR package (lower-case)."
  type        = string
}

variable "ghcr_pat" {
  description = "GitHub Personal Access Token with read:packages scope. Used by the Container App to pull images from GHCR."
  type        = string
  sensitive   = true
}

# ── GitHub OIDC (for bootstrap documentation) ────────────────────────────────

variable "github_owner" {
  description = "GitHub owner (user or org) that hosts the repository. Used when documenting OIDC setup."
  type        = string
  default     = "heidarj"
}

variable "github_repo" {
  description = "GitHub repository name (without the owner prefix). Used when documenting OIDC setup."
  type        = string
  default     = "outlook.mcpar.is"
}
