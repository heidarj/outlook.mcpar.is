# outlook.mcpar.is — Outlook MCP Server

A production-minded read-only [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server for Microsoft Outlook / Microsoft 365, built with .NET 10 and ASP.NET Core.

## Overview

This server exposes Outlook mailbox data to MCP clients (AI assistants, automation tools) via a single `/mcp` endpoint. It authenticates callers using **Microsoft Entra ID bearer tokens** and calls **Microsoft Graph** on behalf of the signed-in user via the **On-Behalf-Of (OBO)** flow.

All operations are **strictly read-only**.

## Architecture

```
MCP Client
   │  Bearer token (for this API)
   ▼
OutlookMcp.Server  ─(OBO)→  Microsoft Graph
   │  JWT validation
   │  Microsoft.Identity.Web
   │  ModelContextProtocol.AspNetCore
   └─ /mcp  (SSE + Streamable HTTP)
```

## MCP Tools

| Tool | Description |
|------|-------------|
| `get_mailbox_profile` | User profile (name, email, job title) |
| `list_mail_folders` | Top-level mail folders with counts |
| `list_messages` | Messages from mailbox or a folder (paginated) |
| `get_message` | Single message with text body |
| `list_calendar_view` | Calendar events in a time window (paginated) |
| `list_contacts` | Contacts (paginated) |
| `get_mailbox_settings` | Timezone, language, auto-reply settings |

All tools are read-only and use `$select` to minimize Graph response size.

## Requirements

- .NET 10 SDK
- Azure AD app registration with the following configured:
  - Application ID URI (e.g. `api://<client-id>`)
  - Client secret (for OBO)
  - Delegated permissions: `User.Read`, `Mail.Read`, `Calendars.Read`, `Contacts.Read`, `MailboxSettings.Read`
  - Redirect URIs:
    - `http://127.0.0.1:33418`
    - `https://vscode.dev/redirect`
    - `https://claude.ai/api/mcp/auth_callback`
  - **Allow public client flows** enabled

## Configuration

Copy `appsettings.example.json` to `src/OutlookMcp.Server/appsettings.json` and fill in your values:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "common",
    "ClientId": "<your-api-client-id>",
    "ClientSecret": "<your-api-client-secret>",
    "Audience": "api://<your-api-client-id>"
  },
  "McpServer": {},
  "MicrosoftGraph": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": ["User.Read", "Mail.Read", "Calendars.Read", "Contacts.Read", "MailboxSettings.Read"]
  }
}
```

`McpServer:BaseUrl` is optional. If it is not configured, the OAuth discovery
endpoints infer the public base URL from the incoming request host and scheme.
When the server runs behind a proxy or load balancer, forwarded
`X-Forwarded-Host` and `X-Forwarded-Proto` headers are respected for this
inference.

## Running

```bash
dotnet run --project src/OutlookMcp.Server
```

The server starts on `http://localhost:5000`.  
MCP endpoint: `http://localhost:5000/mcp`  
Health check: `http://localhost:5000/health`

## Testing

```bash
dotnet test
```

## Pagination

Paginated tools (`list_messages`, `list_mail_folders`, `list_calendar_view`, `list_contacts`) return a `nextLink` field when more results are available. Pass this value back as the `nextLink` parameter to retrieve the next page.

## Security

- Bearer tokens issued **for this API** are validated with Microsoft.Identity.Web.
- The inbound token is **never** forwarded to Microsoft Graph directly.
- The OBO flow exchanges the inbound token for a Graph token scoped to the minimum required permissions.
- All tools are read-only; no write operations are possible.

## Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /.well-known/oauth-protected-resource` | OAuth protected resource metadata for MCP clients |
| `GET /.well-known/oauth-authorization-server` | Proxied Entra authorization server metadata with local issuer |
| `POST /register` | Static OAuth dynamic client registration response |
| `GET /authorize` | Redirect proxy to the Entra authorize endpoint |
| `POST /mcp` | MCP endpoint (requires `Authorization: Bearer <token>`) |
| `GET /health` | Liveness health check |
| `GET /health/ready` | Readiness health check |

## Project Structure

```
src/OutlookMcp.Server/
  Configuration/       # Options classes (AzureAdOptions, GraphOptions)
  Models/              # Stable DTOs (Dtos.cs)
  Services/            # IGraphService + GraphService
  Tools/               # OutlookMcpTools (MCP tool implementations)
  Program.cs           # App entry point + DI wiring
  appsettings.json     # Config (no secrets)

tests/OutlookMcp.Server.Tests/
  Configuration/       # Options binding tests
  Models/              # DTO tests
  Services/            # Graph service / pagination tests
  Tools/               # Tool validation tests

infra/terraform/       # Terraform infrastructure code
docs/bootstrap.md      # Bootstrap / first-time setup guide
Dockerfile             # Multi-stage container image build
```

---

## Infrastructure

The app runs on **Azure Container Apps Consumption** and is deployed via
**GitHub Actions**. Terraform state is stored in **HCP Terraform** (remote
state only; plan/apply runs in GitHub Actions). Secrets are stored in **Azure
Key Vault**. Container images are hosted in **GitHub Container Registry
(GHCR)**.

### Architecture overview

```
GitHub Actions
  ├── terraform-plan.yml   → runs on PRs that touch infra/terraform/
  ├── terraform-apply.yml  → runs on merge to main (gated by "production" env)
  └── deploy.yml           → builds image, pushes to GHCR, deploys to ACA

Azure
  ├── Resource Group
  ├── User-Assigned Managed Identity  (for Key Vault access)
  ├── Key Vault Standard              (stores Entra client secret + GHCR PAT)
  └── Container Apps Environment
        └── Container App (API)
              ├── 0.25 vCPU / 0.5 GiB
              ├── minReplicas = 0  /  maxReplicas = 2
              ├── Cron scale rule: 1 replica Mon–Fri 08:00–18:00 UTC
              └── HTTP scale rule: scale out at 10 concurrent requests

Microsoft Entra
  └── App Registration + Service Principal + client secret (1-year rotation)
```

### Scaling behaviour

| Time window | Replicas |
|-------------|----------|
| Mon–Fri 08:00–18:00 UTC | 1 (warm) |
| Outside working hours | 0 (scale to zero) |
| Under HTTP load (any time) | up to 2 |

### Required GitHub Actions configuration

#### Repository variables (`Settings → Secrets and variables → Actions → Variables`)

| Variable | Description |
|----------|-------------|
| `AZURE_CLIENT_ID` | Client ID of the GitHub Actions OIDC service principal |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `GHCR_USERNAME` | GitHub username / org owning the GHCR package (lower-case) |
| `CONTAINER_APP_NAME` | Name of the Container App (Terraform output: `container_app_name`) |
| `RESOURCE_GROUP_NAME` | Name of the resource group (Terraform output: `resource_group_name`) |

#### Repository secrets

| Secret | Description |
|--------|-------------|
| `TF_API_TOKEN` | HCP Terraform API token (for remote state access) |
| `GHCR_PAT` | GitHub PAT with `read:packages` (passed to Terraform to configure GHCR pull credentials on the Container App) |

### HCP Terraform — state only

HCP Terraform is used **exclusively for remote state storage**. Plans and
applies run locally in GitHub Actions runners, not on HCP Terraform. The HCP
Terraform workspace must have its **Execution Mode set to Local**.

Update `infra/terraform/versions.tf` with your HCP Terraform organisation and
workspace name before the first run.

### GitHub OIDC → Azure

No long-lived Azure credentials are stored in GitHub. The GitHub Actions
workflows authenticate to Azure using **OIDC federated identity**. The
federated credentials must be created once on the Azure AD app that represents
the GitHub Actions identity. See [docs/bootstrap.md](docs/bootstrap.md) for
step-by-step instructions.

### Deployment flow

1. A developer pushes code to `main`.
2. The **Build and Deploy** workflow:
   a. Builds the .NET app and runs tests.
   b. Builds the Docker image and pushes it to GHCR with the commit SHA as the
      tag (e.g. `ghcr.io/heidarj/outlook-mcpar-is:sha-<sha>`).
   c. Runs `az containerapp update --image <new-image>` to create a new
      Container App revision.
3. Azure Container Apps performs a zero-downtime rolling update to the new
   revision.

Terraform manages infrastructure (Key Vault, identity, scaling rules, etc.)
but does **not** manage the running image tag — that is owned by the deploy
workflow.

### What is intentionally not deployed yet

| Feature | Reason |
|---------|--------|
| Custom domain / TLS certificate | Not required for v1; placeholder ingress block ready |
| Application Insights | Unnecessary cost for current scale |
| Log Analytics workspace (retained logs) | Unnecessary cost; streaming logs sufficient |
| Multiple environments (staging, etc.) | Not needed for v1; Terraform structure supports it |
| Azure Container Registry | GHCR is free for public/private repos at this scale |
