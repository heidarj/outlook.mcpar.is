using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
using OutlookMcp.Server.Configuration;
using OutlookMcp.Server.Services;
using OutlookMcp.Server.Tools;

var builder = WebApplication.CreateBuilder(args);
const string OAuthDiscoveryCorsPolicy = "OAuthDiscovery";
const string OAuthMetadataHttpClient = "OAuthMetadataHttpClient";

builder.Services.AddOptions<AzureAdOptions>()
    .Bind(builder.Configuration.GetSection("AzureAd"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<GraphOptions>()
    .Bind(builder.Configuration.GetSection("MicrosoftGraph"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<McpServerOptions>()
    .Bind(builder.Configuration.GetSection("McpServer"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Auth: JWT bearer + OBO + Graph
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddDistributedTokenCaches();

builder.Services.AddCors(options =>
{
    options.AddPolicy(OAuthDiscoveryCorsPolicy, policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpClient(OAuthMetadataHttpClient);
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();

builder.Services.AddScoped<IGraphService, GraphService>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<OutlookMcpTools>();

builder.Services.AddHealthChecks();

builder.Logging.AddConsole();

var app = builder.Build();

var azureAdOptions = app.Services.GetRequiredService<IOptions<AzureAdOptions>>().Value;
var mcpServerOptions = app.Services.GetRequiredService<IOptions<McpServerOptions>>().Value;
var entraHostBaseUrl = $"{azureAdOptions.Instance.TrimEnd('/')}/{azureAdOptions.TenantId}";
var authorizationServerUrl = $"{entraHostBaseUrl}/v2.0";
var openIdConfigurationUrl = $"{authorizationServerUrl}/.well-known/openid-configuration";
var authorizeUrl = $"{entraHostBaseUrl}/oauth2/v2.0/authorize";
var redirectUris = new[]
{
    "http://127.0.0.1:33418",
    "https://vscode.dev/redirect",
    "https://claude.ai/api/mcp/auth_callback"
};

var discoveryEndpoints = app.MapGroup("/.well-known")
    .RequireCors(OAuthDiscoveryCorsPolicy);

static string GetResourceBaseUrl(HttpContext context, McpServerOptions options)
{
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        return options.BaseUrl.TrimEnd('/');
    }

    return $"{context.Request.Scheme}://{context.Request.Host.Value}".TrimEnd('/');
}

app.UseForwardedHeaders();

discoveryEndpoints.MapGet("/oauth-protected-resource", (HttpContext context) => Results.Json(new
{
    resource = GetResourceBaseUrl(context, mcpServerOptions),
    authorization_servers = new[]
    {
        authorizationServerUrl
    }
}));

discoveryEndpoints.MapGet("/oauth-authorization-server", async (HttpContext context, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken) =>
{
    try
    {
        using var client = httpClientFactory.CreateClient(OAuthMetadataHttpClient);
        using var response = await client.GetAsync(openIdConfigurationUrl, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Results.Text(
                responseBody,
                response.Content.Headers.ContentType?.ToString() ?? "application/json",
                statusCode: (int)response.StatusCode);
        }

        if (JsonNode.Parse(responseBody) is not JsonObject metadata)
        {
            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }

        metadata["issuer"] = GetResourceBaseUrl(context, mcpServerOptions);

        return Results.Text(metadata.ToJsonString(), "application/json");
    }
    catch (HttpRequestException)
    {
        return Results.StatusCode(StatusCodes.Status502BadGateway);
    }
});

app.MapPost("/register", () => Results.Json(new
{
    client_id = azureAdOptions.ClientId,
    client_secret_expires_at = 0,
    redirect_uris = redirectUris,
    grant_types = new[] { "authorization_code", "refresh_token" },
    response_types = new[] { "code" },
    token_endpoint_auth_method = "none"
}, statusCode: StatusCodes.Status201Created))
    .RequireCors(OAuthDiscoveryCorsPolicy);

app.MapGet("/authorize", (HttpContext context) =>
{
    var queryString = context.Request.QueryString.Value ?? string.Empty;
    return Results.Redirect($"{authorizeUrl}{queryString}");
});

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.MapMcp("/mcp").RequireAuthorization();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
