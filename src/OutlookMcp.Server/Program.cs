using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
using Microsoft.Kiota.Abstractions.Authentication;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;
using OutlookMcp.Server.Configuration;
using OutlookMcp.Server.Services;
using OutlookMcp.Server.Tools;

var builder = WebApplication.CreateBuilder(args);
var azureAd = builder.Configuration.GetSection("AzureAd");

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
builder.Services.AddAuthentication(options =>
    {
        options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    // Resource metadata is configured below through named options so test and environment overrides
    // are applied after configuration providers have finished loading.
    .AddMcp(_ => { })
    .AddMicrosoftIdentityWebApi(azureAd)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDistributedTokenCaches();

builder.Services.AddOptions<McpAuthenticationOptions>(McpAuthenticationDefaults.AuthenticationScheme)
    .Configure<IOptions<AzureAdOptions>, IOptions<McpServerOptions>>((options, azureAdOptions, mcpServerOptions) =>
    {
        var azureAdValues = azureAdOptions.Value;
        var mcpServerValues = mcpServerOptions.Value;

        options.ResourceMetadata = new ProtectedResourceMetadata
        {
            AuthorizationServers = [$"{azureAdValues.Instance.TrimEnd('/')}/{azureAdValues.TenantId}/v2.0"],
            ScopesSupported = [
                $"api://{azureAdValues.ClientId}/.default"
            ]
        };

        if (!string.IsNullOrWhiteSpace(mcpServerValues.BaseUrl))
        {
            options.ResourceMetadata.Resource = mcpServerValues.BaseUrl.TrimEnd('/');
        }
    });

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<AzureAdOptions>>((options, azureAdOptions) =>
    {
        var clientId = azureAdOptions.Value.ClientId;

        options.TokenValidationParameters.ValidAudiences =
        [
            clientId,
            $"api://{clientId}"
        ];
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddAuthorization();

builder.Services.AddScoped<GraphUserAccessTokenProvider>();
builder.Services.AddScoped<GraphServiceClient>(serviceProvider =>
{
    var graphOptions = serviceProvider.GetRequiredService<IOptions<GraphOptions>>().Value;
    var tokenProvider = serviceProvider.GetRequiredService<GraphUserAccessTokenProvider>();
    var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);

    return new GraphServiceClient(authProvider, graphOptions.BaseUrl);
});
builder.Services.AddScoped<IGraphService, GraphService>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<OutlookMcpTools>();

builder.Services.AddHealthChecks();

builder.Logging.AddConsole();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.MapMcp("/mcp").RequireAuthorization();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
