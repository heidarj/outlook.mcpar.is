using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions.Authentication;
using OutlookMcp.Server.Configuration;

namespace OutlookMcp.Server.Services;

internal sealed class GraphUserAccessTokenProvider : IAccessTokenProvider
{
    private readonly ITokenAcquisition _tokenAcquisition;
    private readonly string[] _scopes;

    public GraphUserAccessTokenProvider(ITokenAcquisition tokenAcquisition, IOptions<GraphOptions> graphOptions)
    {
        _tokenAcquisition = tokenAcquisition;

        var options = graphOptions.Value;
        _scopes = options.Scopes;

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var graphBaseUri))
        {
            throw new InvalidOperationException($"MicrosoftGraph:BaseUrl must be an absolute URI. Current value: '{options.BaseUrl}'.");
        }

        AllowedHostsValidator = new AllowedHostsValidator([graphBaseUri.Host]);
    }

    public AllowedHostsValidator AllowedHostsValidator { get; }

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        return _tokenAcquisition.GetAccessTokenForUserAsync(
            _scopes,
            authenticationScheme: JwtBearerDefaults.AuthenticationScheme);
    }
}
