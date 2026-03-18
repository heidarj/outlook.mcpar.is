# Copilot Instructions for outlook.mcpar.is

## Project Overview

This repository contains **Outlook MCP** — a Model Context Protocol (MCP) server that integrates with Microsoft Outlook. It is a .NET project targeting modern .NET runtimes.

The project is in early stages; the codebase currently contains only foundational files (README, .gitignore, LICENSE). New source code will follow standard .NET conventions.

## Repository Layout

```
/
├── .github/
│   └── copilot-instructions.md   # This file
├── .gitignore                    # .NET-specific gitignore
├── LICENSE
└── README.md
```

As the project grows, expect the following structure:
- **`src/`** — Main source code (C# projects)
- **`tests/`** — Test projects (xUnit or NUnit)
- **`*.sln`** — Visual Studio solution file at the repo root

## Technology Stack

- **Language:** C# (.NET)
- **Runtime:** .NET 8 or later
- **Protocol:** Model Context Protocol (MCP) — see [modelcontextprotocol.io](https://modelcontextprotocol.io)
- **Integration:** Microsoft Outlook / Microsoft Graph API

## Build & Development

### Prerequisites

- .NET SDK 8.0 or later (`dotnet --version` to verify)

### Bootstrap

```bash
dotnet restore
```

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

### Run

```bash
dotnet run --project src/<ProjectName>
```

> Always run `dotnet restore` before building if dependencies have changed.

## Coding Conventions

- Follow standard C# and .NET conventions (PascalCase for types and public members, camelCase for locals/parameters).
- Use file-scoped namespaces.
- Prefer `record` types for immutable data models.
- Use `async`/`await` throughout — all I/O must be asynchronous.
- Keep MCP tool handlers thin; delegate business logic to service classes.

## MCP-Specific Notes

- MCP tools are defined using the MCP .NET SDK attributes or builder pattern.
- Each Outlook operation (read mail, send mail, calendar access, etc.) should be a separate MCP tool.
- Authentication to Microsoft Graph uses OAuth 2.0 / MSAL; credentials must never be hard-coded.

## Key Guidelines for the Coding Agent

- Trust these instructions; only search the codebase if the information here seems incomplete or incorrect.
- When adding a new MCP tool, add a corresponding unit test in the `tests/` directory.
- The `.gitignore` already excludes `bin/`, `obj/`, `*.nupkg`, and other build artifacts — do not commit these.
- If a solution file (`.sln`) does not yet exist, create one with `dotnet new sln` before adding projects.
