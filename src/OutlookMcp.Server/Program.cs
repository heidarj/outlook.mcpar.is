using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using OutlookMcp.Server.Configuration;
using OutlookMcp.Server.Services;
using OutlookMcp.Server.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AzureAdOptions>()
    .Bind(builder.Configuration.GetSection("AzureAd"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<GraphOptions>()
    .Bind(builder.Configuration.GetSection("MicrosoftGraph"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Auth: JWT bearer + OBO + Graph
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddDistributedTokenCaches();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();

builder.Services.AddScoped<IGraphService, GraphService>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<OutlookMcpTools>();

builder.Services.AddHealthChecks();

builder.Logging.AddConsole();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.MapMcp("/mcp").RequireAuthorization();

app.Run();

// Make Program accessible for integration tests
public partial class Program { }
