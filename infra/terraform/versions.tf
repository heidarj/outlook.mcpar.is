terraform {
  required_version = ">= 1.9"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 3.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # HCP Terraform — used for remote state only.
  # Set execution mode to "Local" in the HCP Terraform workspace so that
  # plan/apply run in GitHub Actions, not on HCP Terraform runners.
  # Backend config (hostname, organization, workspace) is supplied at
  # init time via -backend-config flags in the GitHub Actions workflows.
  # The HCP Terraform token is supplied by hashicorp/setup-terraform via
  # cli_config_credentials_token and must NOT be passed via -backend-config.
  backend "remote" {}
}
