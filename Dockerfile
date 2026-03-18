# ── Build stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Restore dependencies separately for better layer caching.
# Restore only the server project to avoid needing the test project files.
COPY src/OutlookMcp.Server/OutlookMcp.Server.csproj src/OutlookMcp.Server/
RUN dotnet restore src/OutlookMcp.Server/OutlookMcp.Server.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/OutlookMcp.Server \
    --configuration Release \
    --output /publish \
    --no-restore

# ── Runtime stage ────────────────────────────────────────────────────────────
# Chiseled image: minimal, non-root by default, reduced attack surface.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /app

COPY --from=build /publish .

# ASP.NET Core defaults to port 8080 in .NET 8 and later (including .NET 10)
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "OutlookMcp.Server.dll"]
