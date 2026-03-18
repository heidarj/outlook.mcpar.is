# Bootstrap Guide

This guide walks through the one-time steps required to provision the
`outlook.mcpar.is` infrastructure from a blank Azure subscription.

After bootstrap, all infrastructure changes are applied automatically from
GitHub Actions.

---

## Prerequisites

| Tool | Minimum version |
|------|-----------------|
| [Terraform CLI](https://developer.hashicorp.com/terraform/install) | 1.9 |
| [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) | 2.60 |
| [GitHub CLI](https://cli.github.com/) | 2.x (optional, for setting secrets) |

You also need:

- An **Azure subscription** with Owner or Contributor + User Access Administrator rights
- An **HCP Terraform account** at <https://app.terraform.io> with an organisation
- A **GitHub repository** (this one) with Actions enabled

---

## Step 1 — Create the HCP Terraform workspace

1. Sign in to <https://app.terraform.io>.
2. Create a new workspace in your organisation:
   - **Name:** `outlook-mcpar-is-prod` (or a name of your choice)
   - **Execution mode:** **Local** — this is critical. Plans and applies run in
     GitHub Actions; HCP Terraform is used for remote state only.
3. Generate an **API token** for the workspace:
   - Go to **User Settings → Tokens → Create an API token**.
   - Store this token as a GitHub Actions secret named `TF_API_TOKEN`.
4. Note your **organisation name**, **workspace name**, and the backend hostname
   (`app.terraform.io`). You will set these as GitHub Actions variables in Step 4.

---

## Step 2 — Create the GitHub Actions service principal (OIDC)

GitHub Actions uses Azure OIDC federated identity, so **no long-lived Azure
secrets are stored in GitHub**.

Run the following commands once from your local machine or Azure Cloud Shell:

```bash
# Variables — adjust to your environment
SUBSCRIPTION_ID="<your-azure-subscription-id>"
TENANT_ID="<your-azure-tenant-id>"
GITHUB_OWNER="heidarj"          # GitHub user or org
GITHUB_REPO="outlook.mcpar.is"  # Repository name (without owner)
SP_NAME="sp-outlook-mcp-github-actions"
RESOURCE_GROUP="rg-outlook-mcp-prod"  # Must match var.app_name + var.environment

# Log in
az login
az account set --subscription "$SUBSCRIPTION_ID"

# Create the service principal (no secret — we use federated credentials)
SP_APP_ID=$(az ad app create --display-name "$SP_NAME" --query appId -o tsv)
az ad sp create --id "$SP_APP_ID"
SP_OBJECT_ID=$(az ad sp show --id "$SP_APP_ID" --query id -o tsv)

# Assign the roles Terraform needs on the subscription
# (Contributor for resources, User Access Administrator for RBAC assignments)
az role assignment create \
  --assignee "$SP_OBJECT_ID" \
  --role "Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID"

az role assignment create \
  --assignee "$SP_OBJECT_ID" \
  --role "User Access Administrator" \
  --scope "/subscriptions/$SUBSCRIPTION_ID"

# Grant permission to manage Entra app registrations
az ad app permission add \
  --id "$SP_APP_ID" \
  --api "00000003-0000-0000-c000-000000000000" \
  --api-permissions "1bfefb4e-e0b5-418b-a88f-73c46d2cc8e9=Role"  # Application.ReadWrite.All

az ad app permission admin-consent --id "$SP_APP_ID"

# Add federated identity credentials — one per workflow/branch combination
# Plan workflow (pull requests from any branch)
az ad app federated-credential create \
  --id "$SP_APP_ID" \
  --parameters "{
    \"name\": \"github-pr\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_OWNER}/${GITHUB_REPO}:pull_request\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"

# Plan workflow (push to main)
az ad app federated-credential create \
  --id "$SP_APP_ID" \
  --parameters "{
    \"name\": \"github-main-push\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_OWNER}/${GITHUB_REPO}:ref:refs/heads/main\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"

# Apply + Deploy workflows (production environment)
az ad app federated-credential create \
  --id "$SP_APP_ID" \
  --parameters "{
    \"name\": \"github-env-production\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"repo:${GITHUB_OWNER}/${GITHUB_REPO}:environment:production\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }"

echo "Client ID: $SP_APP_ID"
echo "Tenant ID: $TENANT_ID"
echo "Subscription ID: $SUBSCRIPTION_ID"
```

---

## Step 3 — Create a GitHub PAT for GHCR pulls

The Container App pulls images from GitHub Container Registry (GHCR). Because
GHCR does not support Azure managed identity for authentication, a GitHub
Personal Access Token is required.

1. Go to **GitHub → Settings → Developer settings → Personal access tokens →
   Fine-grained tokens**.
2. Create a token with **read:packages** permission (classic PAT is also
   acceptable).
3. Store the token value as described in Step 4.

---

## Step 4 — Configure GitHub Actions variables and secrets

### Repository variables (`Settings → Secrets and variables → Actions → Variables`)

| Variable | Value |
|----------|-------|
| `AZURE_CLIENT_ID` | Client ID from Step 2 (`$SP_APP_ID`) |
| `AZURE_TENANT_ID` | Azure tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `GHCR_USERNAME` | GitHub username / org that owns the GHCR package (lower-case) |
| `CONTAINER_APP_NAME` | `ca-outlook-mcp-prod-api` (matches `locals.ca_name` in Terraform) |
| `RESOURCE_GROUP_NAME` | `rg-outlook-mcp-prod` (matches `locals.rg_name` in Terraform) |
| `TF_BACKEND_HOSTNAME` | `app.terraform.io` |
| `TF_BACKEND_ORGANIZATION` | Your HCP Terraform organisation name |
| `TF_BACKEND_WORKSPACE` | Your HCP Terraform workspace name (e.g. `outlook-mcpar-is-prod`) |

> **Note:** `CONTAINER_APP_NAME` and `RESOURCE_GROUP_NAME` must match the
> resource names that Terraform will create. By default these are derived from
> `var.app_name` (`outlook-mcp`) and `var.environment` (`prod`).

### Repository secrets (`Settings → Secrets and variables → Actions → Secrets`)

| Secret | Value |
|--------|-------|
| `TF_API_TOKEN` | HCP Terraform API token from Step 1 |
| `GHCR_PAT` | GitHub PAT from Step 3 (read:packages) |

---

## Step 5 — Configure the GitHub "production" environment

Both the Terraform Apply and Deploy workflows target the `production`
environment, which adds a mandatory approval gate.

1. Go to **Settings → Environments → New environment → production**.
2. Add yourself (or your team) as a required reviewer.
3. Optionally restrict the environment to the `main` branch.

---

## Step 6 — Run the first `terraform apply`

```bash
cd infra/terraform

# Authenticate locally
az login
az account set --subscription "<your-subscription-id>"

# Initialise — this connects to HCP Terraform for remote state.
# Supply backend config via -backend-config (hostname / org / workspace are
# not hardcoded in the repo; the HCP token is read from ~/.terraform.d/credentials.tfrc.json).
terraform init \
  -backend-config="hostname=app.terraform.io" \
  -backend-config="organization=<your-hcp-org>" \
  -backend-config='workspaces=[{name="<your-workspace-name>"}]'

# Review what will be created
terraform plan \
  -var="subscription_id=<your-subscription-id>" \
  -var="tenant_id=<your-tenant-id>" \
  -var="ghcr_username=<your-github-username>" \
  -var="ghcr_pat=<your-ghcr-pat>"

# Apply
terraform apply \
  -var="subscription_id=<your-subscription-id>" \
  -var="tenant_id=<your-tenant-id>" \
  -var="ghcr_username=<your-github-username>" \
  -var="ghcr_pat=<your-ghcr-pat>"
```

Note the outputs:

```
container_app_fqdn     = "ca-outlook-mcp-prod-api.<hash>.westeurope.azurecontainerapps.io"
entra_client_id        = "<generated-client-id>"
key_vault_uri          = "https://kv-outlook-mcp-prod-xxxxx.vault.azure.net/"
```

---

## Step 7 — Push the first container image

After Terraform apply, trigger the **Build and Deploy** workflow:

```bash
git commit --allow-empty -m "chore: trigger first build"
git push origin main
```

Or run the workflow manually from **Actions → Build and Deploy →
Run workflow**.

---

## Ongoing operations

| Task | How |
|------|-----|
| Rotate the Entra client secret | Run `terraform apply` — `azuread_application_password.api` has a 1-year TTL |
| Change container size / scaling | Edit `container_app.tf`, commit, push to `main` |
| Add a custom domain | Add `custom_domain` block to the `ingress {}` in `container_app.tf` |
| Add a new environment | Create a new workspace in HCP Terraform, copy the Terraform directory, set the new backend variables in a new GitHub environment |

---

## Cost notes

- **Container Apps Consumption** charges per vCPU-second and GiB-second of
  active use. With `minReplicas = 0` the app scales to zero outside working
  hours, resulting in zero compute cost overnight and on weekends.
- No Log Analytics workspace is provisioned, keeping monitoring costs at zero.
  Basic streaming logs are available via the Azure portal and `az containerapp logs show`.
- The Key Vault Standard tier costs a few pence per 10,000 operations.
- Total expected monthly cost at low traffic is **< $5 USD**.
