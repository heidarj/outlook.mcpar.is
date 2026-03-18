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
  "MicrosoftGraph": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": ["User.Read", "Mail.Read", "Calendars.Read", "Contacts.Read", "MailboxSettings.Read"]
  }
}
```

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
```
