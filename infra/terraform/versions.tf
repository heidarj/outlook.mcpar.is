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
  # Replace the organization and workspace name with your own values.
  backend "remote" {
    hostname     = "app.terraform.io"
    organization = "YOUR_HCP_ORG" # TODO: replace with your HCP Terraform organisation
    workspaces {
      name = "outlook-mcpar-is-prod" # TODO: replace with your workspace name
    }
  }
}
