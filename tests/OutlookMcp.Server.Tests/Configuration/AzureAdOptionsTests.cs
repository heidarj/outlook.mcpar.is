using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OutlookMcp.Server.Configuration;
using Xunit;

namespace OutlookMcp.Server.Tests.Configuration;

public class AzureAdOptionsTests
{
    [Fact]
    public void Options_Bind_From_Configuration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "common",
                ["AzureAd:ClientId"] = "test-client-id"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<AzureAdOptions>()
            .Bind(config.GetSection("AzureAd"));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AzureAdOptions>>().Value;

        Assert.Equal("https://login.microsoftonline.com/", options.Instance);
        Assert.Equal("common", options.TenantId);
        Assert.Equal("test-client-id", options.ClientId);
    }

    [Fact]
    public void GraphOptions_Bind_From_Configuration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftGraph:BaseUrl"] = "https://graph.microsoft.com/v1.0"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<GraphOptions>()
            .Bind(config.GetSection("MicrosoftGraph"));

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GraphOptions>>().Value;

        Assert.Equal("https://graph.microsoft.com/v1.0", options.BaseUrl);
    }
}
