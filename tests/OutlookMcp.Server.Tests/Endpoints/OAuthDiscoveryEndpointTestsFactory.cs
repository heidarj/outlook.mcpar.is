using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace OutlookMcp.Server.Tests.Endpoints;

internal static class OAuthDiscoveryEndpointTestsFactory
{
    public const string ClientId = "test-client-id";
    public const string TenantId = "test-tenant-id";
    public const string McpBaseUrl = "https://mcp.example.com";
    public const string ScopeName = "Outlook.Access";

    public static WebApplicationFactory<Program> Create(bool includeMcpBaseUrl = true)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                        ["AzureAd:TenantId"] = TenantId,
                        ["AzureAd:ClientId"] = ClientId,
                        ["AzureAd:Audience"] = $"api://{ClientId}",
                        ["McpServer:ScopeName"] = ScopeName,
                        ["MicrosoftGraph:BaseUrl"] = "https://graph.microsoft.com/v1.0"
                    };

                    if (includeMcpBaseUrl)
                    {
                        settings["McpServer:BaseUrl"] = McpBaseUrl;
                    }

                    config.AddInMemoryCollection(settings);
                });
            });
    }
}
