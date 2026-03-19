using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OutlookMcp.Server.Tests.Endpoints;

public class OAuthDiscoveryEndpointTests
{
    private const string ClientId = "test-client-id";
    private const string TenantId = "test-tenant-id";
    private const string McpBaseUrl = "https://mcp.example.com";
    private const string ProxyHttpClientName = "OAuthMetadataProxy";

    [Fact]
    public async Task ProtectedResource_ReturnsConfiguredMetadata_AndCorsHeader()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/.well-known/oauth-protected-resource");
        request.Headers.Add("Origin", "https://client.example.com");

        using var response = await client.SendAsync(request);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal(McpBaseUrl, json.RootElement.GetProperty("resource").GetString());

        var authorizationServers = json.RootElement.GetProperty("authorization_servers").EnumerateArray().Select(element => element.GetString()).ToArray();
        Assert.Equal(new[] { $"https://login.microsoftonline.com/{TenantId}/v2.0" }, authorizationServers);
    }

    [Fact]
    public async Task AuthorizationServerMetadata_ProxiesIssuerRewrite_AndCorsHeader()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "issuer": "https://login.microsoftonline.com/original/v2.0",
                  "authorization_endpoint": "https://login.microsoftonline.com/test-tenant-id/oauth2/v2.0/authorize",
                  "token_endpoint": "https://login.microsoftonline.com/test-tenant-id/oauth2/v2.0/token"
                }
                """,
                Encoding.UTF8,
                "application/json")
        });

        using var factory = CreateFactory(handler);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/.well-known/oauth-authorization-server");
        request.Headers.Add("Origin", "https://client.example.com");

        using var response = await client.SendAsync(request);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal(McpBaseUrl, json.RootElement.GetProperty("issuer").GetString());
        Assert.Equal(
            $"https://login.microsoftonline.com/{TenantId}/v2.0/.well-known/openid-configuration",
            handler.RequestUri?.ToString());
    }

    [Fact]
    public async Task AuthorizationServerMetadata_ReturnsBadGateway_WhenUpstreamRequestFails()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("boom"));

        using var factory = CreateFactory(handler);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/.well-known/oauth-authorization-server");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsStaticClientRegistration_WithCorsHeader()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/register");
        request.Headers.Add("Origin", "https://client.example.com");

        using var response = await client.SendAsync(request);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("*", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal(ClientId, json.RootElement.GetProperty("client_id").GetString());
        Assert.Equal("none", json.RootElement.GetProperty("token_endpoint_auth_method").GetString());

        var redirectUris = json.RootElement.GetProperty("redirect_uris").EnumerateArray().Select(element => element.GetString()).ToArray();
        Assert.Equal(
            new[]
            {
                "http://127.0.0.1:33418",
                "https://vscode.dev/redirect",
                "https://claude.ai/api/mcp/auth_callback"
            },
            redirectUris);
    }

    [Fact]
    public async Task Authorize_RedirectsToEntra_WithOriginalQueryString_AndNoCorsHeader()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/authorize?client_id=abc&code_challenge=abc%2B123%2F456%3D&state=xyz");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(
            $"https://login.microsoftonline.com/{TenantId}/oauth2/v2.0/authorize?client_id=abc&code_challenge=abc%2B123%2F456%3D&state=xyz",
            response.Headers.Location?.ToString());
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task McpEndpoint_StillRequiresAuthorization()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var response = await client.PostAsync(
            "/mcp",
            new StringContent("""{"jsonrpc":"2.0","id":"1","method":"initialize","params":{}}""", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static WebApplicationFactory<Program> CreateFactory(HttpMessageHandler? metadataHandler = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                        ["AzureAd:TenantId"] = TenantId,
                        ["AzureAd:ClientId"] = ClientId,
                        ["AzureAd:Audience"] = $"api://{ClientId}",
                        ["MicrosoftGraph:BaseUrl"] = "https://graph.microsoft.com/v1.0",
                        ["McpServer:BaseUrl"] = McpBaseUrl
                    });
                });

                if (metadataHandler is not null)
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.AddHttpClient(ProxyHttpClientName)
                            .ConfigurePrimaryHttpMessageHandler(() => metadataHandler);
                    });
                }
            });
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            return Task.FromResult(handler(request));
        }
    }
}
