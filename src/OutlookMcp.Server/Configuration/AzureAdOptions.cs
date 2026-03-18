using System.ComponentModel.DataAnnotations;

namespace OutlookMcp.Server.Configuration;

public class AzureAdOptions
{
    [Required]
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    [Required]
    public string TenantId { get; set; } = "common";

    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;
}
