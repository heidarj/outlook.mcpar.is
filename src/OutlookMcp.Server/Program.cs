using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
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
    .AddMcp(_ => { })
    .AddMicrosoftIdentityWebApi(azureAd)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddDistributedTokenCaches();

builder.Services.AddOptions<McpAuthenticationOptions>(McpAuthenticationDefaults.AuthenticationScheme)
    .Configure<IOptions<AzureAdOptions>, IOptions<McpServerOptions>>((options, azureAdOptions, mcpServerOptions) =>
    {
        var azureAdValues = azureAdOptions.Value;
        var mcpServerValues = mcpServerOptions.Value;

        options.ResourceMetadata = new ProtectedResourceMetadata
        {
            AuthorizationServers = [$"{azureAdValues.Instance.TrimEnd('/')}/{azureAdValues.TenantId}/v2.0"],
            ScopesSupported = [$"api://{azureAdValues.ClientId}/{mcpServerValues.ScopeName}"]
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
