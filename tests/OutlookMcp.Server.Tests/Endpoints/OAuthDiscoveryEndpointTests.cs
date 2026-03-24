using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace OutlookMcp.Server.Tests.Endpoints;

public class OAuthDiscoveryEndpointTests
{
    [Fact]
    public async Task ProtectedResource_ReturnsConfiguredMetadata()
    {
        using var factory = OAuthDiscoveryEndpointTestsFactory.Create();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/.well-known/oauth-protected-resource");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OAuthDiscoveryEndpointTestsFactory.McpBaseUrl, json.RootElement.GetProperty("resource").GetString());

        var authorizationServers = json.RootElement.GetProperty("authorization_servers").EnumerateArray().Select(element => element.GetString()).ToArray();
        Assert.Equal(new[] { $"https://login.microsoftonline.com/{OAuthDiscoveryEndpointTestsFactory.TenantId}/v2.0" }, authorizationServers);

        var scopes = json.RootElement.GetProperty("scopes_supported").EnumerateArray().Select(element => element.GetString()).ToArray();
        Assert.Equal(new[] { $"api://{OAuthDiscoveryEndpointTestsFactory.ClientId}/{OAuthDiscoveryEndpointTestsFactory.ScopeName}" }, scopes);
    }

    [Fact]
    public async Task ProtectedResource_UsesRequestUrl_WhenBaseUrlIsNotConfigured()
    {
        using var factory = OAuthDiscoveryEndpointTestsFactory.Create(includeMcpBaseUrl: false);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://connector.example.com")
        });

        using var response = await client.GetAsync("/.well-known/oauth-protected-resource");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https://connector.example.com", json.RootElement.GetProperty("resource").GetString());
    }

    [Fact]
    public async Task McpEndpoint_ReturnsResourceMetadataChallenge_WhenUnauthorized()
    {
        using var factory = OAuthDiscoveryEndpointTestsFactory.Create();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://connector.example.com")
        });

        using var response = await client.PostAsync(
            "/mcp",
            JsonContent.Create(new
            {
                jsonrpc = "2.0",
                id = "1",
                method = "initialize",
                @params = new { }
            }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var challenge = Assert.Single(response.Headers.WwwAuthenticate).ToString();
        Assert.Contains("Bearer", challenge);
        Assert.Contains(
            "resource_metadata=\"https://connector.example.com/.well-known/oauth-protected-resource/mcp\"",
            challenge);
    }

    [Fact]
    public async Task Legacy_OAuthProxyRoutes_AreNotMapped()
    {
        using var factory = OAuthDiscoveryEndpointTestsFactory.Create();
        using var client = factory.CreateClient();

        using var authorizeResponse = await client.GetAsync("/authorize");
        using var registerResponse = await client.PostAsync("/register", JsonContent.Create(new { }));
        using var authorizationServerResponse = await client.GetAsync("/.well-known/oauth-authorization-server");

        Assert.Equal(HttpStatusCode.NotFound, authorizeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, registerResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, authorizationServerResponse.StatusCode);
    }

    [Fact]
    public void JwtBearerOptions_Allow_ClientId_And_ApiAudience()
    {
        using var factory = OAuthDiscoveryEndpointTestsFactory.Create();
        var options = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal(
            new[]
            {
                OAuthDiscoveryEndpointTestsFactory.ClientId,
                $"api://{OAuthDiscoveryEndpointTestsFactory.ClientId}"
            },
            options.TokenValidationParameters.ValidAudiences);
    }
}
