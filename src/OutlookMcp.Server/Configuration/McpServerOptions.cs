using System.ComponentModel.DataAnnotations;

namespace OutlookMcp.Server.Configuration;

public class McpServerOptions
{
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
}
