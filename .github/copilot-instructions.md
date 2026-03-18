# Copilot Instructions for outlook.mcpar.is

## Project Overview
This repository is a **read-only Outlook MCP server** built with **.NET 10 / ASP.NET Core**.
It exposes Microsoft Outlook/Microsoft 365 data to MCP clients via a single `/mcp` endpoint.

## Architecture Decisions

### Authentication
- **Microsoft.Identity.Web** handles JWT bearer token validation
- The server is a **protected resource** — clients send bearer tokens issued **for this API**
- **On-Behalf-Of (OBO)** flow is used to acquire Graph tokens from the inbound bearer token
- The inbound token is **never** passed through to Microsoft Graph

### MCP Transport
- Uses `ModelContextProtocol.AspNetCore` 1.1.0 official SDK
- Single `/mcp` endpoint (SSE + Streamable HTTP)
- All tools use `[McpServerTool(ReadOnly = true)]`

### Graph Calls
- Use `GraphServiceClient` from `Microsoft.Identity.Web.MicrosoftGraph` DI registration
- Always use `$select` to request only needed fields
- Support pagination via `OdataNextLink` passthrough in `PagedResult<T>`
- For subsequent pages, use `WithUrl(nextLink)` on the request builder
- `get_message` defaults to text body: `Prefer: outlook.body-content-type="text"`

## Code Conventions

### Naming
- DTOs: `*Dto` suffix (e.g., `MailboxProfileDto`, `MessageDto`)
- Services: `I*Service` interface + `*Service` implementation
- Tools: Methods in `OutlookMcpTools` class

### Patterns
- All Graph calls go through `IGraphService` (testable interface)
- Map Graph SDK models to stable DTOs — **never return raw SDK models**
- Input validation is done in tool methods before calling `IGraphService`
- Use `CancellationToken` throughout

### No-Write Policy
- **No write operations of any kind**
- **No** `POST`, `PATCH`, `PUT`, `DELETE` to Graph
- **No** send mail, calendar writes, contact writes
- **No** attachment downloads (v1)

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

## Adding New Tools
1. Add DTO(s) to `Models/Dtos.cs`
2. Add method to `IGraphService` and implement in `GraphService`
3. Add `[McpServerTool(ReadOnly = true)]` method to `OutlookMcpTools`
4. Validate inputs before calling `IGraphService`
5. Add unit tests for validation and output mapping

## Pagination Pattern
The Graph SDK 5.x does not expose `$skiptoken` as a query parameter property.
Instead, use the `OdataNextLink` from the response as the `nextLink` in `PagedResult<T>`.
For subsequent pages, call `requestBuilder.WithUrl(nextLink).GetAsync()` — the full OData URL
already contains all the required query parameters from the original request.

## Adding New Dependencies
- Check GitHub Advisory Database for vulnerabilities before adding
- Prefer packages already in use (Microsoft.Identity.Web, Microsoft.Graph)
- Never add packages that enable write operations

## Testing Practices
- `IGraphService` is mocked with Moq in tool tests
- Configuration tests use `IConfiguration` with in-memory dictionaries
- Integration tests use `WebApplicationFactory<Program>` where needed
