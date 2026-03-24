using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using OutlookMcp.Server.Services;

namespace OutlookMcp.Server.Tests.Endpoints;

public class GraphClientRegistrationTests
{
    [Fact]
    public void GraphServices_CanBeResolved_FromServiceProvider()
    {
        using var factory = OAuthDiscoveryEndpointTestsFactory.Create();
        using var scope = factory.Services.CreateScope();

        var graphClient = scope.ServiceProvider.GetRequiredService<GraphServiceClient>();
        var graphService = scope.ServiceProvider.GetRequiredService<IGraphService>();

        Assert.NotNull(graphClient);
        Assert.NotNull(graphService);
    }
}
